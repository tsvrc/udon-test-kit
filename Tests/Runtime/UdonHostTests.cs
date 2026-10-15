using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Tests.Helpers;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.Udon;
using Object = UnityEngine.Object;

namespace Tsvrc.UdonTestKit.Tests
{
    public class UdonHostTests
    {
        [Test]
        public void Create_ClassWithAProgramAsset_RunsItsProgram()
        {
            // Arrange
            using var udon = new UdonHost();
            UdonBehaviour probe = udon.Create<UdonProbe>("Probe");

            // Act
            probe.SendCustomEvent(nameof(UdonProbe._Run));

            // Assert
            Assert.That(probe.GetProgramVariable(nameof(UdonProbe.Runs)), Is.EqualTo(1));
        }

        [Test]
        public void Create_Name_NamesTheGameObject()
        {
            // Arrange
            using var udon = new UdonHost();

            // Act
            UdonBehaviour probe = udon.Create<UdonProbe>("Door");

            // Assert
            Assert.That(probe.gameObject.name, Is.EqualTo("Door"));
        }

        [UnityTest]
        public IEnumerator Create_Active_Starts()
        {
            // Arrange
            using var udon = new UdonHost();

            // Act
            UdonBehaviour probe = udon.Create<UdonProbe>("Probe");
            yield return WaitForStart(probe);

            // Assert
            Assert.That(probe.HasDoneStart, Is.True);
        }

        [Test]
        public void Create_Inactive_LeavesTheGameObjectInactive()
        {
            // Arrange
            using var udon = new UdonHost();

            // Act
            UdonBehaviour probe = udon.Create<UdonProbe>("Probe", active: false);

            // Assert
            Assert.That(probe.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator Create_InactiveThenActivated_Starts()
        {
            // Arrange
            using var udon = new UdonHost();
            UdonBehaviour probe = udon.Create<UdonProbe>("Probe", active: false);

            // Act
            probe.gameObject.SetActive(true);
            yield return WaitForStart(probe);

            // Assert
            Assert.That(probe.HasDoneStart, Is.True);
        }

        [Test]
        public void Create_ClassWithoutAProgramAsset_ThrowsAnErrorThatSaysSo()
        {
            // Arrange
            using var udon = new UdonHost();

            // Act and assert
            Assert.That(() => udon.Create<TargetBehaviour>("Target"),
                Throws.InvalidOperationException.With.Message.StartsWith("TargetBehaviour has no UdonSharp program asset"));
        }

        [Test]
        public void Dispose_AfterCreate_DestroysTheGameObjects()
        {
            // Arrange
            var udon = new UdonHost();
            GameObject first = udon.Create<UdonProbe>("First").gameObject;
            GameObject second = udon.Create<UdonProbe>("Second").gameObject;

            // Act
            udon.Dispose();

            // Assert
            Assert.That(first == null, Is.True, "First wasn't destroyed.");
            Assert.That(second == null, Is.True, "Second wasn't destroyed.");
        }

        [Test]
        public void Dispose_GameObjectTheTestDestroyed_SkipsIt()
        {
            // Arrange
            var udon = new UdonHost();
            Object.DestroyImmediate(udon.Create<UdonProbe>("Probe").gameObject);

            // Act and assert
            Assert.That(() => udon.Dispose(), Throws.Nothing);
        }

        [UnityTest]
        public IEnumerator ExpectUdonException_UdonSharpLogsTheCopy_EndsOnceItCame()
        {
            // Arrange
            VRChatExceptionWatching.DropQueuedErrors();
            using var watching = new VRChatExceptionWatching(on: false);
            using var udon = new UdonHost();
            UdonBehaviour probe = udon.Create<UdonProbe>("Probe");
            bool copied = false;
            Application.LogCallback onLog = (message, stackTrace, type) => copied |= message.Contains(UdonHost.UdonSharpCopy);
            Application.logMessageReceived += onLog;

            // Act: stepped here instead of yielded, so no frame passes after it ends.
            IEnumerator expect = UdonHost.ExpectUdonException(() => probe.SendCustomEvent(nameof(UdonProbe._Throw)));
            try
            {
                while (expect.MoveNext())
                {
                    yield return expect.Current;
                }
            }
            finally
            {
                Application.logMessageReceived -= onLog;
            }

            // Assert: an error it didn't expect would also fail this test.
            Assert.That(copied, Is.True);
        }

        // Each error is matched by the whole first line Udon or UdonSharp gives it.
        [UnityTest]
        public IEnumerator ExpectUdonException_ErrorsItExpects_EndTheFirstLinesUdonAndUdonSharpLog()
        {
            // Arrange
            VRChatExceptionWatching.DropQueuedErrors();
            using var watching = new VRChatExceptionWatching(on: false);
            using var udon = new UdonHost();
            UdonBehaviour probe = udon.Create<UdonProbe>("Probe");
            var firstLines = new List<string>();
            Application.LogCallback onLog = (message, stackTrace, type) => firstLines.Add(message.Split('\n')[0]);
            Application.logMessageReceived += onLog;

            // Act
            try
            {
                yield return UdonHost.ExpectUdonException(() => probe.SendCustomEvent(nameof(UdonProbe._Throw)));
            }
            finally
            {
                Application.logMessageReceived -= onLog;
            }

            // Assert
            Assert.That(firstLines, Has.Some.EndsWith(UdonHost.HaltError + "."));
            Assert.That(firstLines, Has.Some.EndsWith(UdonHost.UdonSharpCopy));
        }

        [UnityTest]
        public IEnumerator ExpectUdonException_UdonSharpLogsNoCopy_ExpectsOnlyTheHaltError()
        {
            // Arrange
            using var udon = new UdonHost();
            UdonBehaviour probe = udon.Create<UdonProbe>("Probe");

            // Act and assert: UdonSharp queues the error for its copy in the call that logs the error,
            // so dropping the queue right after the throw keeps the copy from coming, and an expected
            // error that never came would fail this test.
            yield return UdonHost.ExpectUdonException(() =>
            {
                probe.SendCustomEvent(nameof(UdonProbe._Throw));
                VRChatExceptionWatching.DropQueuedErrors();
            });
        }

        private static IEnumerator WaitForStart(UdonBehaviour behaviour)
        {
            for (int frame = 0; frame < 5 && !behaviour.HasDoneStart; frame++)
            {
                yield return null;
            }
        }
    }
}

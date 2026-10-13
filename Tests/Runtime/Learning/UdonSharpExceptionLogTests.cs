using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Tests.Helpers;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.Udon;

namespace Tsvrc.UdonTestKit.Tests.Learning
{
    // UdonSharp logs an exception thrown in Udon a second time, on a later frame, where it would fail
    // the next test. It doesn't when it watches the VRChat client's log, as it does by default, and
    // finds no VRChat installation: it queues the error instead, and logs the copy once watching
    // stops. So UdonHost.ExpectUdonException waits a few frames for the copy and expects it only once
    // it came, and the kit tests that stop watching drop the queue first.
    public class UdonSharpExceptionLogTests : UdonTestBase
    {
        private bool _copied;
        private bool _copiedDuringTheCall;

        [UnityTest]
        public IEnumerator Exception_NotWatchingVRChat_IsLoggedAgainOnALaterFrame()
        {
            // Arrange
            VRChatExceptionWatching.DropQueuedErrors();
            using var watching = new VRChatExceptionWatching(on: false);

            // Act
            yield return ThrowAndWaitForCopy();

            // Assert
            Assert.That(_copiedDuringTheCall, Is.False);
            Assert.That(_copied, Is.True, "No copy after 10 frames.");
        }

        [UnityTest]
        public IEnumerator Exception_WatchingVRChatWithoutVRChat_IsNotLoggedAgain()
        {
            // Arrange
            IgnoreWhereVRChatIsInstalled();
            using var watching = new VRChatExceptionWatching(on: true);

            // Act
            yield return ThrowAndWaitForCopy();

            // Assert
            Assert.That(_copied, Is.False);
        }

        [UnityTest]
        public IEnumerator Exception_WatchingVRChatWithoutVRChat_IsLoggedAgainOnceWatchingStops()
        {
            // Arrange
            IgnoreWhereVRChatIsInstalled();
            VRChatExceptionWatching.DropQueuedErrors();
            using var watchingOn = new VRChatExceptionWatching(on: true);
            yield return ThrowAndWaitForCopy();
            Assert.That(_copied, Is.False, "UdonSharp logged the copy while it watched for VRChat.");

            // Act
            using var watchingOff = new VRChatExceptionWatching(on: false);
            yield return WaitForCopy();

            // Assert
            Assert.That(_copied, Is.True, "No copy after 10 frames.");
        }

        private static void IgnoreWhereVRChatIsInstalled()
        {
            if (VRChatExceptionWatching.VRChatDataFolderExists())
            {
                Assert.Ignore("VRChat is installed here, so UdonSharp finds its log.");
            }
        }

        // Makes a probe throw, then waits for the copy of its error.
        private IEnumerator ThrowAndWaitForCopy()
        {
            UdonBehaviour probe = Udon.Create<UdonProbe>("Probe");
            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape(UdonHost.HaltError)));
            _copied = false;
            Application.logMessageReceived += RecordCopy;
            probe.SendCustomEvent(nameof(UdonProbe._Throw));
            Application.logMessageReceived -= RecordCopy;
            _copiedDuringTheCall = _copied;
            yield return WaitForCopy();
        }

        // Waits up to 10 frames for the copy, and expects it once it came.
        private IEnumerator WaitForCopy()
        {
            Application.logMessageReceived += RecordCopy;
            try
            {
                for (int frame = 0; frame < 10 && !_copied; frame++)
                {
                    yield return null;
                }
            }
            finally
            {
                Application.logMessageReceived -= RecordCopy;
            }
            if (_copied)
            {
                LogAssert.Expect(LogType.Error, new Regex(Regex.Escape(UdonHost.UdonSharpCopy)));
            }
        }

        private void RecordCopy(string message, string stackTrace, LogType type)
        {
            _copied |= message.Contains(UdonHost.UdonSharpCopy);
        }
    }
}

using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.Udon;

namespace Tsvrc.UdonTestKit.Tests.Learning
{
    // UdonSharp logs an exception thrown in Udon a second time, on a later frame, where it would fail
    // the next test. So UdonHost.ExpectUdonException waits for that copy.
    public class UdonSharpExceptionLogTests : UdonTestBase
    {
        [UnityTest]
        public IEnumerator Exception_ThrownInUdon_IsLoggedAgainByUdonSharpOnALaterFrame()
        {
            // Arrange
            UdonBehaviour probe = Udon.Create<UdonProbe>("Probe");
            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape(UdonHost.HaltError)));
            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape(UdonHost.UdonSharpCopy)));
            bool copied = false;
            Application.LogCallback onLog = (message, stackTrace, type) => copied |= message.Contains(UdonHost.UdonSharpCopy);
            Application.logMessageReceived += onLog;

            // Act
            bool copiedDuringTheCall;
            int frames = 0;
            try
            {
                probe.SendCustomEvent(nameof(UdonProbe._Throw));
                copiedDuringTheCall = copied;
                for (; frames < 10 && !copied; frames++)
                {
                    yield return null;
                }
            }
            finally
            {
                Application.logMessageReceived -= onLog;
            }

            // Assert
            Assert.That(copiedDuringTheCall, Is.False);
            Assert.That(copied, Is.True, $"No copy after {frames} frames.");
        }
    }
}

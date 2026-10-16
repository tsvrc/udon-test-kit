using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.Udon;

namespace Tsvrc.UdonTestKit.Tests.Learning
{
    // UdonManager runs the events a program delays with SendCustomEventDelayedFrames and
    // SendCustomEventDelayedSeconds for a behaviour UdonHost created: never during the call that
    // delays them, and not before the delay passed. So a test of code that delays an event, run
    // through UdonHost, yields until the event ran instead of calling the delayed method itself.
    public class UdonDelayedEventTests : UdonTestBase
    {
        // The delay of UdonProbe._RunAfterHalfASecond.
        private const float DelaySeconds = 0.5f;

        [UnityTest]
        public IEnumerator SendCustomEventDelayedFrames_FromTheProgram_RunsTheEventOnALaterFrame()
        {
            // Arrange
            UdonBehaviour probe = Udon.Create<UdonProbe>("Probe");

            // Act
            probe.SendCustomEvent(nameof(UdonProbe._RunAfterAFrame));
            int runsDuringTheCall = Runs(probe);
            for (int frame = 0; frame < 10 && Runs(probe) == 0; frame++)
            {
                yield return null;
            }

            // Assert
            Assert.That(runsDuringTheCall, Is.Zero);
            Assert.That(Runs(probe), Is.EqualTo(1), "The delayed event didn't run within 10 frames.");
        }

        [UnityTest]
        public IEnumerator SendCustomEventDelayedSeconds_FromTheProgram_RunsTheEventOnceTheDelayPassed()
        {
            // Arrange
            UdonBehaviour probe = Udon.Create<UdonProbe>("Probe");

            // Act
            probe.SendCustomEvent(nameof(UdonProbe._RunAfterHalfASecond));
            int runsDuringTheCall = Runs(probe);
            yield return new WaitForSeconds(DelaySeconds / 2);
            int runsBeforeTheDelay = Runs(probe);
            float waitStart = Time.time;
            while (Runs(probe) == 0 && Time.time - waitStart < 5f)
            {
                yield return null;
            }

            // Assert
            Assert.That(runsDuringTheCall, Is.Zero);
            Assert.That(runsBeforeTheDelay, Is.Zero);
            Assert.That(Runs(probe), Is.EqualTo(1), "The delayed event didn't run within 5 seconds of the delay.");
        }

        private static int Runs(UdonBehaviour probe) => (int)probe.GetProgramVariable(nameof(UdonProbe.Runs));
    }
}

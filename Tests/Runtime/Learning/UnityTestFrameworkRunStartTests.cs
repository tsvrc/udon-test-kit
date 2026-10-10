using NUnit.Framework;
using UnityEditor;

namespace Tsvrc.UdonTestKit.Tests.Learning
{
    // Unity Test Framework reports a Play Mode run's start only after the domain reload into Play
    // Mode, so TestRun.IsActive can't cover that reload. Editor tooling that reacts to it also checks
    // EditorApplication.isPlayingOrWillChangePlaymode, which is true there.
    public class UnityTestFrameworkRunStartTests
    {
        [Test]
        public void RunStarted_PlayModeRun_ComesAfterTheDomainLoadIntoPlayMode()
        {
            Assume.That(PlayModeDomainLoad.Recorded, Is.True, "No domain load into Play Mode was recorded in this run.");

            Assert.That(PlayModeDomainLoad.RunStartedBeforeIt, Is.False);
        }

        // Records, at the domain load into Play Mode, whether the run had reported its start.
        [InitializeOnLoad]
        private static class PlayModeDomainLoad
        {
            private const string RecordedKey = "Tsvrc.UdonTestKit.Tests.PlayModeDomainLoad.Recorded";
            private const string RunStartedKey = "Tsvrc.UdonTestKit.Tests.PlayModeDomainLoad.RunStarted";

            static PlayModeDomainLoad()
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }
                SessionState.SetBool(RecordedKey, true);
                SessionState.SetBool(RunStartedKey, TestRun.StartedInThisEditorSession);
            }

            public static bool Recorded => SessionState.GetBool(RecordedKey, false);
            public static bool RunStartedBeforeIt => SessionState.GetBool(RunStartedKey, false);
        }
    }
}

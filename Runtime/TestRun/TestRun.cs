using System;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Tsvrc.UdonTestKit
{
    /// <summary>
    /// Whether a Unity Test Framework run is in progress: from when the run reports its start until
    /// it reports its end, across domain reloads, and from the start of a batch-mode process started
    /// with -runTests. Editor tooling that rewrites assets when the domain reloads or a scene changes
    /// can check it to leave tests alone. A Play Mode run reports its start only after the domain
    /// reload into Play Mode, so tooling that reacts to that reload also checks
    /// EditorApplication.isPlayingOrWillChangePlaymode.
    /// </summary>
    [InitializeOnLoad]
    public static class TestRun
    {
        private const string StartedKey = "Tsvrc.UdonTestKit.TestRun.Started";

        static TestRun()
        {
            // Registered callbacks don't survive a domain reload, so every domain load registers them.
            ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(new Callbacks());
        }

        public static bool IsActive => IsActiveFor(StartedInThisEditorSession, Environment.GetCommandLineArgs());

        internal static bool IsActiveFor(bool startedInThisEditorSession, string[] commandLineArgs) =>
            startedInThisEditorSession || IsTestProcess(commandLineArgs);

        // SessionState survives domain reloads but not an editor restart, so a run that never
        // reported its end can't leave this set for good.
        internal static bool StartedInThisEditorSession
        {
            get => SessionState.GetBool(StartedKey, false);
            set => SessionState.SetBool(StartedKey, value);
        }

        internal static bool IsTestProcess(string[] commandLineArgs) => commandLineArgs.Contains("-runTests");

        internal sealed class Callbacks : IErrorCallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) => StartedInThisEditorSession = true;

            public void RunFinished(ITestResultAdaptor result) => StartedInThisEditorSession = false;

            public void OnError(string message) => StartedInThisEditorSession = false;

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }
    }
}

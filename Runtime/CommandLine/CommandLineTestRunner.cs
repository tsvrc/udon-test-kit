using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UdonTestKit
{
    /// <summary>
    /// Runs the project's tests in one test mode, writes a plain-text report, and exits Unity
    /// with 0 when no test failed, 1 otherwise. The scripts CommandLineScripts creates start it with:
    /// <c>-executeMethod UdonTestKit.CommandLineTestRunner.Run -udonTestKitScriptVersion &lt;n&gt;
    /// -testPlatform EditMode|PlayMode -testReport &lt;file&gt; [-assemblyNames "A;B"]</c>
    /// </summary>
    [InitializeOnLoad]
    public static class CommandLineTestRunner
    {
        private const string RunMethod = "UdonTestKit.CommandLineTestRunner.Run";

        private static readonly string NoTestsMessage =
            "No tests found. Create test assemblies with " + TestAssemblyCreator.MenuPath.Replace("/", " > ") + ".";

        static CommandLineTestRunner()
        {
            // Registered callbacks don't survive a domain reload, and entering Play Mode reloads
            // the domain, so every load of this run's Unity process registers them again.
            if (Argument("-executeMethod") == RunMethod)
                ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(new Reporter());
        }

        public static void Run()
        {
            if (Argument("-udonTestKitScriptVersion") != CommandLineScripts.ScriptVersion.ToString())
            {
                Finish("This script was created by another version of Udon Test Kit. Create it again with " +
                    CommandLineScripts.MenuPath.Replace("/", " > ") + ".", 1);
                return;
            }

            string[] assemblies = Argument("-assemblyNames")?.Split(';') ?? ProjectAssemblies().ToArray();

            // A filter without assembly names runs every test, packages' included.
            if (assemblies.Length == 0)
            {
                Finish(NoTestsMessage, 0);
                return;
            }

            var filter = new Filter
            {
                testMode = (TestMode)Enum.Parse(typeof(TestMode), Argument("-testPlatform")),
                assemblyNames = assemblies,
            };
            ScriptableObject.CreateInstance<TestRunnerApi>().Execute(new ExecutionSettings(filter));
        }

        // Assemblies with source files under Assets. Their tests are the project's own, not the
        // ones that come with packages, such as the VRChat SDK's.
        internal static IEnumerable<string> ProjectAssemblies()
        {
            return CompilationPipeline.GetAssemblies(AssembliesType.Editor)
                .Where(assembly => assembly.sourceFiles.Any(file => file.StartsWith("Assets/")))
                .Select(assembly => assembly.name);
        }

        private static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 ? args[index + 1] : null;
        }

        private static void Finish(string report, int exitCode)
        {
            File.WriteAllText(Argument("-testReport"), report);
            EditorApplication.Exit(exitCode);
        }

        private sealed class Reporter : IErrorCallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                List<ITestResultAdaptor> testCases = TestCases(result).ToList();
                if (testCases.Count == 0)
                {
                    Finish(NoTestsMessage, 0);
                    return;
                }

                var report = new StringBuilder();
                foreach (ITestResultAdaptor testCase in testCases.Where(testCase => testCase.TestStatus != TestStatus.Passed))
                {
                    report.AppendLine($"{testCase.TestStatus.ToString().ToUpperInvariant()}  {testCase.FullName}");
                    if (!string.IsNullOrEmpty(testCase.Message))
                        report.AppendLine("    " + testCase.Message.Trim().Replace("\n", "\n    "));
                }

                int passed = testCases.Count(testCase => testCase.TestStatus == TestStatus.Passed);
                int failed = testCases.Count(testCase => testCase.TestStatus == TestStatus.Failed);
                report.AppendLine($"{passed} passed, {failed} failed, {testCases.Count - passed - failed} inconclusive or skipped");
                Finish(report.ToString(), failed > 0 ? 1 : 0);
            }

            // Called instead of RunFinished when a run can't finish, such as when a prebuild setup
            // throws. Unity would keep running if this didn't exit.
            public void OnError(string message)
            {
                Finish(message, 1);
            }

            private static IEnumerable<ITestResultAdaptor> TestCases(ITestResultAdaptor result)
            {
                return result.Test.IsSuite ? result.Children.SelectMany(TestCases) : new[] { result };
            }
        }
    }
}

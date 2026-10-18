using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // TestRun and TestSceneSavedData register their Unity Test Framework callbacks on every domain
    // load, and TestRun keeps whether a run is in progress in SessionState. While a test runs, a
    // script it imports leaves the Editor compiling until reloads are unlocked, so the tests that
    // import scripts delete them with ProjectScripts.DeleteFolders, which lets that compile run and
    // waits for the domain reload it ends in, as it does after this fixture's import.
    public class UnityDomainReloadTests
    {
        private const string Key = "Tsvrc.UdonTestKit.Editor.Tests.Learning.UnityDomainReloadTests";
        private const string Folder = "Assets/UdonTestKitDomainReloadTests";
        private const string Script = Folder + "/UdonTestKitImportedScript.cs";

        [TearDown]
        public void TearDown()
        {
            SessionState.EraseBool(Key);
        }

        [UnityTearDown]
        public IEnumerator DeleteTheScript() => ProjectScripts.DeleteFolders(Folder);

        [Test]
        public void ImportAsset_ScriptWhileATestRuns_LeavesTheEditorCompiling()
        {
            // Act
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(Folder));
            File.WriteAllText(Script, "public class UdonTestKitImportedScript { }");
            AssetDatabase.ImportAsset(Script);

            // Assert
            Assert.That(EditorApplication.isCompiling, Is.True);
        }

        [UnityTest]
        public IEnumerator RegisterCallbacks_AfterADomainReload_AreGone()
        {
            // Arrange
            ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(new Callbacks());
            Assert.That(TestRunnerCallbacks.Registered().OfType<Callbacks>(), Is.Not.Empty);

            // Act
            EditorUtility.RequestScriptReload();
            yield return new WaitForDomainReload();

            // Assert
            Assert.That(TestRunnerCallbacks.Registered().OfType<Callbacks>(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator SessionState_AfterADomainReload_KeepsItsValues()
        {
            // Arrange
            SessionState.SetBool(Key, true);

            // Act
            EditorUtility.RequestScriptReload();
            yield return new WaitForDomainReload();

            // Assert
            Assert.That(SessionState.GetBool(Key, false), Is.True);
        }

        private sealed class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }
    }
}

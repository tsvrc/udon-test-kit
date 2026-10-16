using System.Collections;
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
    // load, and TestRun keeps whether a run is in progress in SessionState.
    public class UnityDomainReloadTests
    {
        private const string Key = "Tsvrc.UdonTestKit.Editor.Tests.Learning.UnityDomainReloadTests";

        [TearDown]
        public void TearDown()
        {
            SessionState.EraseBool(Key);
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

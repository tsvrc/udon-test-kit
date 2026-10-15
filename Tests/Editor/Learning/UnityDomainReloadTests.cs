using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // TestRun registers its Unity Test Framework callbacks on every domain load, and keeps whether a
    // run is in progress in SessionState.
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
            Assert.That(RegisteredCallbacks().OfType<Callbacks>(), Is.Not.Empty);

            // Act
            EditorUtility.RequestScriptReload();
            yield return new WaitForDomainReload();

            // Assert
            Assert.That(RegisteredCallbacks().OfType<Callbacks>(), Is.Empty);
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

        // TestRunnerApi keeps the callbacks in an internal singleton.
        private static ICallbacks[] RegisteredCallbacks()
        {
            Type holder = typeof(TestRunnerApi).Assembly.GetType("UnityEditor.TestTools.TestRunner.Api.CallbacksHolder", throwOnError: true);
            object instance = PrivateFieldAccess.InvokeStatic(holder, "get_instance");
            return (ICallbacks[])PrivateFieldAccess.InvokeInstance(instance, "GetAll");
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

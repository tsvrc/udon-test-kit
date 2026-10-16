using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Tsvrc.UdonTestKit
{
    // Deletes what ClientSim saved for the Unity Test Framework test scene when a run ends, so test
    // runs don't fill the ClientSimStorage folder of the project.
    [InitializeOnLoad]
    internal static class TestSceneSavedData
    {
        static TestSceneSavedData()
        {
            // Registered callbacks don't survive a domain reload, so every domain load registers them.
            ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(new Callbacks(ClientSimSession.StorageRoot));
        }

        internal sealed class Callbacks : ICallbacks
        {
            private readonly string _storageRoot;

            public Callbacks(string storageRoot)
            {
                _storageRoot = storageRoot;
            }

            public void RunFinished(ITestResultAdaptor result) => ClientSimSession.DeleteTestSceneSavedData(_storageRoot);

            public void RunStarted(ITestAdaptor testsToRun)
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

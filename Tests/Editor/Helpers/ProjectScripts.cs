using System.Collections;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine.TestTools;

namespace Tsvrc.UdonTestKit.Editor.Tests.Helpers
{
    // Compiles scripts a test wrote into the project. While a test runs, Unity Test Framework locks
    // assembly reloads, and Unity compiles nothing while they're locked.
    internal static class ProjectScripts
    {
        // Runs the compile an import asked for, which has to fail: a compile that succeeds reloads the
        // domain, and Unity Test Framework stops the run when a test didn't ask for the reload.
        internal static IEnumerator CompileThatFails()
        {
            EditorApplication.UnlockReloadAssemblies();
            try
            {
                while (EditorApplication.isCompiling)
                {
                    yield return null;
                }
            }
            finally
            {
                EditorApplication.LockReloadAssemblies();
            }
        }

        // Deletes a folder of scripts and, once they were imported, waits for the compile without
        // them and the domain reload that follows.
        internal static IEnumerator DeleteFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                FileUtil.DeleteFileOrDirectory(folder);
                yield break;
            }

            AssetDatabase.DeleteAsset(folder);
            yield return new WaitForDomainReload();
            // Before a reload, Unity Test Framework 1.1 saves the step the current test method reached,
            // and after it, starts the next test method at that step. In [UnityTearDown] the test
            // method has finished, so the next [UnityTest] would start finished and run nothing.
            PrivateFieldAccess.SetField(typeof(TestRunnerApi).Assembly.GetType("UnityEditor.TestTools.TestRunner.EnumeratorStepHelper", throwOnError: true), "m_PC", 0);
        }
    }
}

using System.Collections;
using UdonSharp.Compiler;
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

            DeleteCompiledPrograms(folder);
            AssetDatabase.DeleteAsset(folder);
            yield return new WaitForDomainReload();
            // Before a reload, Unity Test Framework 1.1 saves the step the current test method reached,
            // and after it, starts the next test method at that step. In [UnityTearDown] the test
            // method has finished, so the next [UnityTest] would start finished and run nothing.
            PrivateFieldAccess.SetField(typeof(TestRunnerApi).Assembly.GetType("UnityEditor.TestTools.TestRunner.EnumeratorStepHelper", throwOnError: true), "m_PC", 0);
        }

        // Deletes the programs UdonSharp compiled for the program assets in a folder, which it keeps in
        // Assets/SerializedUdonPrograms under the GUID of each program asset and leaves there when
        // the program asset is deleted. A compile under way writes them when it ends, so it waits for
        // that first.
        internal static void DeleteCompiledPrograms(string folder)
        {
            PrivateFieldAccess.InvokeStatic(typeof(UdonSharpCompilerV1), "WaitForCompile");
            foreach (string guid in AssetDatabase.FindAssets("t:UdonSharpProgramAsset", new[] { folder }))
            {
                AssetDatabase.DeleteAsset($"Assets/SerializedUdonPrograms/{guid}.asset");
            }
        }
    }
}

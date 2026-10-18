using System.Collections;
using System.Linq;
using UdonSharp.Compiler;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine.TestTools;

namespace Tsvrc.UdonTestKit.Editor.Tests.Helpers
{
    // Compiles scripts a test wrote into the project. While a test runs, Unity Test Framework locks
    // assembly reloads, and Unity compiles nothing while they're locked: from the import of a script
    // it reports a compile until reloads are unlocked, and refuses to add a behaviour meanwhile. So a
    // test that imports scripts deletes them with DeleteFolders, which lets that compile run.
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

        // Deletes folders the test wrote scripts into and, once any of them was imported, waits for
        // the compile without them and the domain reload that follows. Return it from [UnityTearDown]:
        // after the reload, Unity Test Framework resumes the enumerator that method returned.
        internal static IEnumerator DeleteFolders(params string[] folders)
        {
            string[] imported = folders.Where(AssetDatabase.IsValidFolder).ToArray();
            foreach (string folder in folders.Except(imported))
            {
                FileUtil.DeleteFileOrDirectory(folder);
            }
            if (imported.Length == 0)
            {
                yield break;
            }

            foreach (string folder in imported)
            {
                DeleteCompiledPrograms(folder);
                AssetDatabase.DeleteAsset(folder);
            }
            yield return new WaitForDomainReload();
            // Before a reload, Unity Test Framework saves the step the current test method reached,
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

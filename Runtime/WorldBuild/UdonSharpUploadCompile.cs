using System.Collections.Generic;
using UdonSharp.Compiler;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tsvrc.UdonTestKit
{
    /// <summary>
    /// Compiles every UdonSharp program as the VRChat SDK does before a world build, so a test can
    /// check that the programs of a world compile for upload as well as for the Editor.
    /// </summary>
    public static class UdonSharpUploadCompile
    {
        /// <summary>Compiles every UdonSharp program for upload and returns the errors that compile
        /// logged, which it marks expected, so the test decides what they mean. Then it compiles them
        /// for the Editor again, since the upload compile leaves out code inside <c>#if UNITY_EDITOR</c>.
        /// Call it from a test: outside one, Unity Test Framework has no log scope to expect errors in,
        /// and LogAssert throws InvalidOperationException.</summary>
        public static IReadOnlyList<string> Run()
        {
            var errors = new List<string>();
            void Record(string message, string stackTrace, LogType type)
            {
                if (type == LogType.Error)
                {
                    errors.Add(message);
                }
            }

            // With a compile under way and another queued, CompileSync returns before its own compile
            // runs. A compile for the Editor first leaves nothing queued.
            UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions());
            Application.logMessageReceived += Record;
            try
            {
                UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions
                {
                    IsEditorBuild = false,
                    CurrentBuildTarget = EditorUserBuildSettings.activeBuildTarget,
                });
            }
            finally
            {
                Application.logMessageReceived -= Record;
                UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions());
            }
            foreach (string error in errors)
            {
                LogAssert.Expect(LogType.Error, error);
            }
            return errors;
        }
    }
}

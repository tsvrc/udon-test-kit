using System.Collections.Generic;
using UdonSharp.Compiler;
using UnityEditor;
using UnityEngine;

namespace Tsvrc.UdonTestKit
{
    /// <summary>
    /// Compiles every UdonSharp program as the VRChat SDK does before a world build, so a test can
    /// check that the world's programs compile for upload, not only for the Editor.
    /// </summary>
    public static class UdonSharpUploadCompile
    {
        /// <summary>Compiles every UdonSharp program for upload and returns the errors that compile
        /// logged. Then it compiles them for the Editor again, since the upload compile leaves out code
        /// inside <c>#if UNITY_EDITOR</c>.</summary>
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
            return errors;
        }
    }
}

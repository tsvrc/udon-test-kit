using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using VRC.SDK3.Editor.Builder;

namespace Tsvrc.UdonTestKit
{
    /// <summary>
    /// A world the VRChat SDK built from the open scene, so a test can check what a world build holds,
    /// such as which UdonSharp programs reach it.
    /// </summary>
    public sealed class BuiltWorld
    {
        // Where the VRChat SDK's world exporter records the path of the world file it built.
        internal const string WorldFileKey = "currentBuildingAssetBundlePath";

        private BuiltWorld(string file, string text)
        {
            FilePath = file;
            Text = text;
        }

        // Where the world file was before FromOpenScene deleted it.
        internal string FilePath { get; }

        /// <summary>The objects in the world file, as text from the WebExtract and binary2text tools
        /// that ship with the Unity Editor.</summary>
        public string Text { get; }

        /// <summary>Builds the open scene into a world file with the world exporter of the VRChat SDK,
        /// without the callbacks the SDK runs before a build, reads the file and deletes it. The exporter
        /// records the path of the file in EditorPrefs, which every Unity project of the user shares, so
        /// the path recorded before is put back.</summary>
        public static BuiltWorld FromOpenScene()
        {
            string previousFile = EditorPrefs.GetString(WorldFileKey);
            string file;
            try
            {
                if (!new VRCWorldAssetExporter().ExportCurrentSceneResource(true, _ => { }, _ => { }))
                {
                    throw new InvalidOperationException("The world exporter of the VRChat SDK didn't build the open scene.");
                }
                file = EditorPrefs.GetString(WorldFileKey);
            }
            finally
            {
                EditorPrefs.SetString(WorldFileKey, previousFile);
            }
            try
            {
                return new BuiltWorld(file, Read(file));
            }
            finally
            {
                File.Delete(file);
            }
        }

        /// <summary>Whether the world holds the compiled program of an UdonSharp class.</summary>
        public bool HoldsProgramOf(Type udonSharpClass)
        {
            return Text.Contains($"m_Name \"{UdonHost.ProgramAssetOf(udonSharpClass).SerializedProgramAsset.name}\"");
        }

        private static string Read(string worldFile)
        {
            string tools = Path.Combine(EditorApplication.applicationContentsPath, "Tools");
            string folder = Path.Combine(Path.GetTempPath(), "Tsvrc.UdonTestKit.BuiltWorld");
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
            Directory.CreateDirectory(folder);
            try
            {
                string bundle = Path.Combine(folder, "world");
                File.Copy(worldFile, bundle);
                Run(Path.Combine(tools, "WebExtract.exe"), $"\"{bundle}\"");
                var text = new StringBuilder();
                foreach (string extracted in Directory.GetFiles(bundle + "_data"))
                {
                    Run(Path.Combine(tools, "binary2text.exe"), $"\"{extracted}\" \"{extracted}.txt\"");
                    text.Append(File.ReadAllText(extracted + ".txt"));
                }
                return text.ToString();
            }
            finally
            {
                Directory.Delete(folder, recursive: true);
            }
        }

        private static void Run(string tool, string arguments)
        {
            using Process process = Process.Start(new ProcessStartInfo(tool, arguments) { UseShellExecute = false, CreateNoWindow = true });
            process.WaitForExit();
        }
    }
}

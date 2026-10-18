using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Editor.Builder;
using VRC.SDKBase;
using Object = UnityEngine.Object;

namespace Tsvrc.UdonTestKit
{
    /// <summary>
    /// A world the VRChat SDK built from the world scene, the loaded scene that holds the scene
    /// descriptor, so a test can check what a world build holds, such as which UdonSharp programs
    /// reach it.
    /// </summary>
    public sealed class BuiltWorld
    {
        private BuiltWorld(string file, string text)
        {
            FilePath = file;
            Text = text;
        }

        // Where the world file was before FromWorldScene deleted it.
        internal string FilePath { get; }

        /// <summary>The objects in the world file, as text from the WebExtract and binary2text tools
        /// that ship with the Unity Editor.</summary>
        public string Text { get; }

        /// <summary>Builds the world scene, the loaded scene that holds the scene descriptor, into a
        /// world file with the world exporter of the VRChat SDK, without the callbacks the SDK runs
        /// before a build, reads the file and deletes it. Without those callbacks UdonSharp doesn't
        /// compile for upload first, so the world holds each program as last compiled for the Editor.
        /// The record the SDK keeps of your own builds stays as it was: the last build, which
        /// Test Last Build and Reload Last Build in the SDK control panel use, and the world files of
        /// earlier builds, which the exporter would otherwise delete. Throws InvalidOperationException
        /// when the loaded scenes don't hold exactly one scene descriptor, when the world scene was
        /// never saved, or when the exporter builds nothing.</summary>
        public static BuiltWorld FromWorldScene()
        {
            return FromWorldScene(() => new VRCWorldAssetExporter().ExportCurrentSceneResource(true, _ => { }, _ => { }));
        }

        internal static BuiltWorld FromWorldScene(Func<bool> export)
        {
            // The exporter builds neither. It asks about the descriptors with a dialog, and on Windows
            // about the world scene never saved too, and outside batch mode a dialog waits for a click.
            VRC_SceneDescriptor[] descriptors = Object.FindObjectsOfType<VRC_SceneDescriptor>(true);
            if (descriptors.Length != 1)
            {
                throw new InvalidOperationException($"The loaded scenes have {descriptors.Length} scene descriptors. The world exporter of the VRChat SDK builds a world with exactly one.");
            }
            if (string.IsNullOrEmpty(descriptors[0].gameObject.scene.path))
            {
                throw new InvalidOperationException("The world scene, which holds the scene descriptor, was never saved. The world exporter of the VRChat SDK builds only a saved scene.");
            }

            string file;
            using (new SdkBuildRecord())
            {
                if (!export())
                {
                    throw new InvalidOperationException("The world exporter of the VRChat SDK didn't build the world scene.");
                }
                file = EditorPrefs.GetString(SdkBuildRecord.WorldFileKey);
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

        internal static string Read(string worldFile)
        {
            string tools = Path.Combine(EditorApplication.applicationContentsPath, "Tools");
            // The Windows Editor ships these tools as .exe files, the Linux Editor without an extension.
            string extension = Application.platform == RuntimePlatform.WindowsEditor ? ".exe" : "";
            // A new folder in the project for each read, so a build in another project or test run at
            // the same time can't delete its files.
            string folder = Path.GetFullPath(FileUtil.GetUniqueTempPathInProject());
            Directory.CreateDirectory(folder);
            try
            {
                string bundle = Path.Combine(folder, "world");
                File.Copy(worldFile, bundle);
                Run(Path.Combine(tools, "WebExtract" + extension), $"\"{bundle}\"");
                var text = new StringBuilder();
                foreach (string extracted in Directory.GetFiles(bundle + "_data"))
                {
                    Run(Path.Combine(tools, "binary2text" + extension), $"\"{extracted}\" \"{extracted}.txt\"");
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

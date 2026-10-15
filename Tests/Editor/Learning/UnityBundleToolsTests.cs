using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Editor.Builder;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // The Unity Editor ships WebExtract and binary2text in its Tools folder, as .exe files only on
    // Windows. WebExtract unpacks a world file into a folder named after it with _data, and
    // binary2text turns each unpacked file into text, where the serialized program of each UdonSharp
    // behaviour the world holds shows as an m_Name. So BuiltWorld runs both, and HoldsProgramOf looks
    // for that m_Name.
    public class UnityBundleToolsTests
    {
        private string _previousFile;
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _previousFile = EditorPrefs.GetString(BuiltWorld.WorldFileKey);
            _folder = Path.GetFullPath(FileUtil.GetUniqueTempPathInProject());
            Directory.CreateDirectory(_folder);
        }

        [TearDown]
        public void TearDown()
        {
            string file = EditorPrefs.GetString(BuiltWorld.WorldFileKey);
            if (file != _previousFile && File.Exists(file))
            {
                File.Delete(file);
            }
            EditorPrefs.SetString(BuiltWorld.WorldFileKey, _previousFile);
            Directory.Delete(_folder, recursive: true);
        }

        [Test]
        public void Binary2Text_WorldUnpackedByWebExtract_NamesTheProgramOfThePlacedBehaviour()
        {
            // Arrange
            using var scene = new BuildableScene(typeof(UdonCallLog));
            new VRCWorldAssetExporter().ExportCurrentSceneResource(true, _ => { }, _ => { });
            string world = Path.Combine(_folder, "world");
            File.Copy(EditorPrefs.GetString(BuiltWorld.WorldFileKey), world);
            RunTool("WebExtract", $"\"{world}\"");

            // Act
            var text = new StringBuilder();
            foreach (string unpacked in Directory.GetFiles(world + "_data"))
            {
                RunTool("binary2text", $"\"{unpacked}\" \"{unpacked}.txt\"");
                text.Append(File.ReadAllText(unpacked + ".txt"));
            }

            // Assert
            string program = UdonSharpEditorUtility.GetUdonSharpProgramAsset(typeof(UdonCallLog)).SerializedProgramAsset.name;
            Assert.That(text.ToString(), Does.Contain($"m_Name \"{program}\""));
        }

        private static void RunTool(string name, string arguments)
        {
            string extension = Application.platform == RuntimePlatform.WindowsEditor ? ".exe" : "";
            string tool = Path.Combine(EditorApplication.applicationContentsPath, "Tools", name + extension);
            using Process process = Process.Start(new ProcessStartInfo(tool, arguments) { UseShellExecute = false, CreateNoWindow = true });
            process.WaitForExit();
        }
    }
}

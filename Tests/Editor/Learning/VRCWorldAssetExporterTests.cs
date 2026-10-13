using System.IO;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;
using UnityEditor;
using VRC.SDK3.Editor.Builder;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // The VRChat SDK's world exporter builds the open scene into a world file and records the path of
    // the file in EditorPrefs. So BuiltWorld reads the path there, and puts back the one recorded
    // before.
    public class VRCWorldAssetExporterTests
    {
        private string _previousFile;

        [SetUp]
        public void SetUp()
        {
            _previousFile = EditorPrefs.GetString(BuiltWorld.WorldFileKey);
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
        }

        [Test]
        public void ExportCurrentSceneResource_BuildableScene_RecordsTheWorldFileInEditorPrefs()
        {
            // Arrange
            using var scene = new BuildableScene();
            EditorPrefs.SetString(BuiltWorld.WorldFileKey, "");

            // Act
            bool built = new VRCWorldAssetExporter().ExportCurrentSceneResource(true, _ => { }, _ => { });

            // Assert
            string file = EditorPrefs.GetString(BuiltWorld.WorldFileKey);
            Assert.That(built, Is.True);
            Assert.That(File.Exists(file), Is.True, $"No world file at '{file}'.");
        }
    }
}

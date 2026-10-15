using System.IO;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;
using Tsvrc.UdonTestKit.Tests;
using UdonSharp.Compiler;
using UnityEditor;
using VRC.SDK3.Editor.Builder;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // The VRChat SDK's world exporter builds the open scene into a world file and records the path of
    // the file in EditorPrefs. It doesn't run the callbacks the SDK runs before a build, among them
    // the UdonSharp compile for upload, so the world holds each program as UdonSharp last compiled it
    // for the Editor. So BuiltWorld reads the path there, puts back the one recorded before, and says
    // which programs its world holds.
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

        [Test]
        public void ExportCurrentSceneResource_ProgramsCompiledForTheEditor_LeavesThemCompiledForTheEditor()
        {
            // Arrange
            using var scene = new BuildableScene();
            UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions());
            Assert.That(ProgramHeap.Strings(typeof(UdonProbe)), Has.Member(UdonProbe.EditorOnlyMarker),
                "The Editor compile left the marker out.");

            // Act
            new VRCWorldAssetExporter().ExportCurrentSceneResource(true, _ => { }, _ => { });

            // Assert: the compile for upload leaves the marker out.
            Assert.That(ProgramHeap.Strings(typeof(UdonProbe)), Has.Member(UdonProbe.EditorOnlyMarker));
        }
    }
}

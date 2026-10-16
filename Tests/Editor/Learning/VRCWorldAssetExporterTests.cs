using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;
using Tsvrc.UdonTestKit.Tests;
using UdonSharp.Compiler;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRC.Core;
using VRC.SDK3.Components;
using VRC.SDK3.Editor.Builder;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // The VRChat SDK's world exporter builds the open scene into a world file and records the path of
    // the file in EditorPrefs. It doesn't run the callbacks the SDK runs before a build, among them
    // the UdonSharp compile for upload, so the world holds each program as UdonSharp last compiled it
    // for the Editor. So BuiltWorld reads the path there, puts back the one recorded before, and says
    // which programs its world holds.
    //
    // It builds a scene with exactly one scene descriptor, active or not, and unsaved changes. A scene
    // with any other number of descriptors, or one never saved, it asks about with a dialog and
    // builds nothing; in batch mode Unity cancels the dialog and logs that it did. So
    // BuiltWorld.FromOpenScene checks both before it builds, since outside batch mode the dialog
    // waits for a click.
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
            bool built = Export();

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
            Export();

            // Assert: the compile for upload leaves the marker out.
            Assert.That(ProgramHeap.Strings(typeof(UdonProbe)), Has.Member(UdonProbe.EditorOnlyMarker));
        }

        [TestCase(0, 0)]
        [TestCase(2, 0)]
        [TestCase(1, 1)]
        public void ExportCurrentSceneResource_NotExactlyOneDescriptor_AsksWithADialogAndBuildsNothing(int active, int inactive)
        {
            // Arrange
            IgnoreOutsideBatchMode();
            using var scene = new BuildableScene();
            Object.DestroyImmediate(Object.FindObjectOfType<VRCSceneDescriptor>().gameObject);
            AddDescriptors(active, inactive);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            LogAssert.Expect(LogType.Assert, new Regex("^Cancelling DisplayDialog: Build Custom Scene Scene needs exactly one VRC_SceneDescriptor"));

            // Act
            bool built = Export();

            // Assert
            Assert.That(built, Is.False);
        }

        [Test]
        public void ExportCurrentSceneResource_SceneNeverSaved_AsksWhereToSaveItAndBuildsNothing()
        {
            // Arrange
            IgnoreOutsideBatchMode();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddDescriptors(1, 0);
            LogAssert.Expect(LogType.Assert, new Regex("^Cancelling FileDialog: Save Scene"));
            LogAssert.Expect(LogType.Error, "AssetBundle was not built");

            // Act
            bool built = Export();

            // Assert
            Assert.That(built, Is.False);
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void ExportCurrentSceneResource_OnlyDescriptorInactiveOrDisabled_Builds(bool gameObjectActive, bool componentEnabled)
        {
            // Arrange
            using var scene = new BuildableScene();
            VRCSceneDescriptor descriptor = Object.FindObjectOfType<VRCSceneDescriptor>();
            descriptor.enabled = componentEnabled;
            descriptor.gameObject.SetActive(gameObjectActive);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            // Act and assert
            Assert.That(Export(), Is.True);
        }

        // A dialog about the changes would fail the test with the log of its cancelling.
        [Test]
        public void ExportCurrentSceneResource_UnsavedChanges_Builds()
        {
            // Arrange
            using var scene = new BuildableScene();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            // Act and assert
            Assert.That(Export(), Is.True);
        }

        private static bool Export() => new VRCWorldAssetExporter().ExportCurrentSceneResource(true, _ => { }, _ => { });

        private static void AddDescriptors(int active, int inactive)
        {
            for (int i = 0; i < active + inactive; i++)
            {
                var descriptor = new GameObject("Descriptor");
                descriptor.AddComponent<VRCSceneDescriptor>().spawns = new[] { descriptor.transform };
                descriptor.AddComponent<PipelineManager>();
                descriptor.SetActive(i < active);
            }
        }

        private static void IgnoreOutsideBatchMode()
        {
            if (!Application.isBatchMode)
            {
                Assert.Ignore("Outside batch mode the dialog of the exporter waits for a click.");
            }
        }
    }
}

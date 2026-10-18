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
using UdonSharpEditor;
using VRC.SDK3.Components;
using VRC.SDK3.Editor.Builder;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // The VRChat SDK's world exporter builds the world scene, the loaded scene that holds the scene
    // descriptor, into a world file and records the path of the file in EditorPrefs. It doesn't run
    // the callbacks the SDK runs before a build, among them the UdonSharp compile for upload, so the
    // world holds each program as UdonSharp last compiled it for the Editor. So BuiltWorld reads the
    // path there, puts back the one recorded before, and says which programs its world holds.
    //
    // It counts the scene descriptors of every loaded scene, inactive ones too, and builds only when
    // there is exactly one, whichever scene is active, even with unsaved changes. It builds nothing
    // with any other number, which it asks about with a dialog, or when the world scene was never
    // saved, which on Windows it asks about with a dialog too. In batch mode Unity cancels a dialog
    // and logs that it did. So BuiltWorld.FromWorldScene checks both before it builds, since outside
    // batch mode a dialog waits for a click.
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
            Assert.That(ProgramHeap.Strings(typeof(UdonDouble)), Has.Member(UdonDouble.EditorOnlyMarker),
                "The Editor compile left the marker out.");

            // Act
            Export();

            // Assert: the compile for upload leaves the marker out.
            Assert.That(ProgramHeap.Strings(typeof(UdonDouble)), Has.Member(UdonDouble.EditorOnlyMarker));
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
            for (int i = 0; i < active + inactive; i++)
            {
                BuildableScene.AddDescriptor(active: i < active);
            }
            EditorSceneManager.SaveScene(scene.Scene);
            LogAssert.Expect(LogType.Assert, new Regex("^" + Regex.Escape(DescriptorDialog)));

            // Act
            bool built = Export();

            // Assert
            Assert.That(built, Is.False);
        }

        [Test]
        public void ExportCurrentSceneResource_DescriptorInEachOfTwoLoadedScenes_AsksWithADialogAndBuildsNothing()
        {
            // Arrange
            IgnoreOutsideBatchMode();
            using var scene = new BuildableScene();
            BuildableScene.AddUnsavedScene(withDescriptor: true);
            LogAssert.Expect(LogType.Assert, new Regex("^" + Regex.Escape(DescriptorDialog)));

            // Act
            bool built = Export();

            // Assert
            Assert.That(built, Is.False);
        }

        [Test]
        public void ExportCurrentSceneResource_DescriptorInALoadedSceneThatIsntActive_BuildsThatScene()
        {
            // Arrange: the active scene is another one, never saved.
            using var scene = new BuildableScene(typeof(UdonCallLog));
            BuildableScene.AddUnsavedScene(withDescriptor: false, typeof(UdonDouble));

            // Act
            bool built = Export();

            // Assert
            string text = BuiltWorld.Read(EditorPrefs.GetString(BuiltWorld.WorldFileKey));
            Assert.That(built, Is.True);
            Assert.That(text, Does.Contain(ProgramName(typeof(UdonCallLog))));
            Assert.That(text, Does.Not.Contain(ProgramName(typeof(UdonDouble))));
        }

        [Test, UnityPlatform(RuntimePlatform.WindowsEditor)]
        public void ExportCurrentSceneResource_WorldSceneNeverSavedOnWindows_AsksWhereToSaveItAndBuildsNothing()
        {
            // Arrange
            IgnoreOutsideBatchMode();
            using var scene = new BuildableScene();
            MakeAnotherSceneTheWorldSceneNeverSaved(scene);
            LogAssert.Expect(LogType.Assert, new Regex("^Cancelling FileDialog: Save Scene"));
            LogAssert.Expect(LogType.Error, "AssetBundle was not built");

            // Act
            bool built = Export();

            // Assert
            Assert.That(built, Is.False);
        }

        [Test, UnityPlatform(exclude = new[] { RuntimePlatform.WindowsEditor })]
        public void ExportCurrentSceneResource_WorldSceneNeverSaved_BuildsNothing()
        {
            // Arrange
            IgnoreOutsideBatchMode();
            using var scene = new BuildableScene();
            MakeAnotherSceneTheWorldSceneNeverSaved(scene);
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
            EditorSceneManager.SaveScene(scene.Scene);

            // Act and assert
            Assert.That(Export(), Is.True);
        }

        // A dialog about the changes would fail the test with the log of its cancelling.
        [Test]
        public void ExportCurrentSceneResource_UnsavedChanges_Builds()
        {
            // Arrange
            using var scene = new BuildableScene();
            EditorSceneManager.MarkSceneDirty(scene.Scene);

            // Act and assert
            Assert.That(Export(), Is.True);
        }

        private const string DescriptorDialog = "Cancelling DisplayDialog: Build Custom Scene Scene needs exactly one VRC_SceneDescriptor";

        private static bool Export() => new VRCWorldAssetExporter().ExportCurrentSceneResource(true, _ => { }, _ => { });

        // The saved scene keeps being the active scene, and the scene descriptor moves to a new scene
        // that is never saved.
        private static void MakeAnotherSceneTheWorldSceneNeverSaved(BuildableScene scene)
        {
            Object.DestroyImmediate(Object.FindObjectOfType<VRCSceneDescriptor>().gameObject);
            EditorSceneManager.SaveScene(scene.Scene);
            BuildableScene.AddUnsavedScene(withDescriptor: true);
            SceneManager.SetActiveScene(scene.Scene);
        }

        private static string ProgramName(System.Type udonSharpClass) =>
            $"m_Name \"{UdonSharpEditorUtility.GetUdonSharpProgramAsset(udonSharpClass).SerializedProgramAsset.name}\"";

        private static void IgnoreOutsideBatchMode()
        {
            if (!Application.isBatchMode)
            {
                Assert.Ignore("Outside batch mode the dialog of the exporter waits for a click.");
            }
        }
    }
}

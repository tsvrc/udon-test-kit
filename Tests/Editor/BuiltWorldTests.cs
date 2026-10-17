using System.IO;
using System.Linq;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;
using Tsvrc.UdonTestKit.Tests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Components;
using VRC.SDKBase.Editor;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    // The fixture builds its world over a made-up record of earlier builds, with stand-in world files,
    // and puts the record of the user back when it ends.
    public class BuiltWorldTests
    {
        private SdkBuildRecord _userRecord;
        private string _folder;
        private string _lastBuild;
        private string _buildBefore;
        private string[] _recordBefore;
        private BuildableScene _scene;
        private BuiltWorld _world;

        [OneTimeSetUp]
        public void BuildAWorld()
        {
            _userRecord = new SdkBuildRecord();
            _folder = Path.GetFullPath(FileUtil.GetUniqueTempPathInProject());
            Directory.CreateDirectory(_folder);
            _lastBuild = Path.Combine(_folder, "last build.vrcw");
            _buildBefore = Path.Combine(_folder, "build before.vrcw");
            File.WriteAllText(_lastBuild, "");
            File.WriteAllText(_buildBefore, "");
            foreach (string key in SdkEditorPrefsKeys)
            {
                EditorPrefs.SetString(key, "Recorded before: " + key);
            }
            SessionState.SetString(SdkBuildRecord.BuildBeforeKey, _buildBefore);
            VRC_SdkBuilder.VRCPath = _lastBuild;
            _recordBefore = SdkRecord();
            _scene = new BuildableScene(typeof(UdonCallLog));
            _world = BuiltWorld.FromWorldScene();
        }

        [OneTimeTearDown]
        public void RemoveTheScene()
        {
            _scene?.Dispose();
            _userRecord?.Dispose();
            Directory.Delete(_folder, recursive: true);
        }

        [Test]
        public void HoldsProgramOf_PlacedBehaviour_IsTrue()
        {
            Assert.That(_world.HoldsProgramOf(typeof(UdonCallLog)), Is.True);
        }

        [Test]
        public void HoldsProgramOf_BehaviourNothingPlaces_IsFalse()
        {
            Assert.That(_world.HoldsProgramOf(typeof(UdonDouble)), Is.False);
        }

        [Test]
        public void HoldsProgramOf_ClassWithoutAProgramAsset_Throws()
        {
            Assert.That(() => _world.HoldsProgramOf(typeof(TestsOnlyBehaviour)),
                Throws.InvalidOperationException.With.Message.StartsWith("TestsOnlyBehaviour has no UdonSharp program asset"));
        }

        // BuiltWorld reads a copy of the world file named world, and WebExtract writes the objects of
        // the world it reads to a folder next to it named after it, ending in _data.
        [Test]
        public void FromWorldScene_AfterReadingTheWorld_LeavesNeitherTheCopyNorWhatWasExtracted()
        {
            Assert.That(Directory.GetFileSystemEntries("Temp", "*world", SearchOption.AllDirectories), Is.Empty);
            Assert.That(Directory.GetFileSystemEntries("Temp", "*world_data", SearchOption.AllDirectories), Is.Empty);
        }

        [Test]
        public void FromWorldScene_AfterTheBuild_PutsBackWhatTheSdkRecordedBefore()
        {
            Assert.That(SdkRecord(), Is.EqualTo(_recordBefore));
        }

        [Test]
        public void FromWorldScene_AfterTheBuild_KeepsTheWorldFilesOfEarlierBuilds()
        {
            Assert.That(File.Exists(_lastBuild), Is.True, "The world file of the last build was deleted.");
            Assert.That(File.Exists(_buildBefore), Is.True, "The world file of the build before was deleted.");
        }

        [Test]
        public void FromWorldScene_AfterReadingTheWorld_DeletesTheWorldFile()
        {
            Assert.That(_world.FilePath, Is.Not.Null.And.Not.Empty);
            Assert.That(File.Exists(_world.FilePath), Is.False, $"'{_world.FilePath}' is still there.");
        }

        // Had the exporter run, the dialog it cancels in batch mode would fail the test with its log.
        [TestCase(0, 0)]
        [TestCase(2, 0)]
        [TestCase(1, 1)]
        public void FromWorldScene_NotExactlyOneDescriptor_ThrowsBeforeTheExporterAsks(int active, int inactive)
        {
            // Arrange
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            for (int i = 0; i < active + inactive; i++)
            {
                BuildableScene.AddDescriptor(active: i < active);
            }

            // Act and assert
            Assert.That(() => BuiltWorld.FromWorldScene(), Throws.InvalidOperationException.With.Message.EqualTo(
                $"The loaded scenes have {active + inactive} scene descriptors. The world exporter of the VRChat SDK builds a world with exactly one."));
        }

        [Test]
        public void FromWorldScene_DescriptorInEachOfTwoLoadedScenes_ThrowsBeforeTheExporterAsks()
        {
            // Arrange
            using var scene = new BuildableScene();
            BuildableScene.AddUnsavedScene(withDescriptor: true);

            // Act and assert
            Assert.That(() => BuiltWorld.FromWorldScene(), Throws.InvalidOperationException.With.Message.EqualTo(
                "The loaded scenes have 2 scene descriptors. The world exporter of the VRChat SDK builds a world with exactly one."));
        }

        [Test]
        public void FromWorldScene_WorldSceneNeverSaved_ThrowsBeforeTheExporterAsks()
        {
            // Arrange: the active scene is saved, and the world scene is another one, never saved.
            using var scene = new BuildableScene();
            Object.DestroyImmediate(Object.FindObjectOfType<VRCSceneDescriptor>().gameObject);
            EditorSceneManager.SaveScene(scene.Scene);
            BuildableScene.AddUnsavedScene(withDescriptor: true);
            SceneManager.SetActiveScene(scene.Scene);

            // Act and assert
            Assert.That(() => BuiltWorld.FromWorldScene(), Throws.InvalidOperationException.With.Message.EqualTo(
                "The world scene, which holds the scene descriptor, was never saved. The world exporter of the VRChat SDK builds only a saved scene."));
        }

        [Test]
        public void FromWorldScene_ActiveSceneIsAnotherNeverSaved_BuildsTheWorldScene()
        {
            // Arrange
            using var scene = new BuildableScene(typeof(UdonCallLog));
            BuildableScene.AddUnsavedScene(withDescriptor: false, typeof(UdonDouble));

            // Act
            BuiltWorld world = BuiltWorld.FromWorldScene();

            // Assert
            Assert.That(world.HoldsProgramOf(typeof(UdonCallLog)), Is.True);
            Assert.That(world.HoldsProgramOf(typeof(UdonDouble)), Is.False);
        }

        [Test]
        public void FromWorldScene_OnlyDescriptorInactive_BuildsTheWorld()
        {
            // Arrange
            using var scene = new BuildableScene(typeof(UdonCallLog));
            Object.FindObjectOfType<VRCSceneDescriptor>().gameObject.SetActive(false);
            EditorSceneManager.SaveScene(scene.Scene);

            // Act
            BuiltWorld world = BuiltWorld.FromWorldScene();

            // Assert
            Assert.That(world.HoldsProgramOf(typeof(UdonCallLog)), Is.True);
        }

        [Test]
        public void FromWorldScene_ExportBuildsNothing_ThrowsAndPutsBackWhatTheSdkRecordedBefore()
        {
            // Arrange
            using var scene = new BuildableScene();

            // Act
            var failure = Assert.Throws<System.InvalidOperationException>(() => BuiltWorld.FromWorldScene(() =>
            {
                EditorPrefs.SetString(SdkBuildRecord.WorldFileKey, "A path the failed export recorded");
                return false;
            }));

            // Assert
            Assert.That(failure.Message, Is.EqualTo("The world exporter of the VRChat SDK didn't build the world scene."));
            Assert.That(SdkRecord(), Is.EqualTo(_recordBefore));
        }

        private static readonly string[] SdkEditorPrefsKeys =
            { SdkBuildRecord.WorldFileKey, SdkBuildRecord.LastBuildKey, SdkBuildRecord.SignatureKey };

        // What the exporter records in EditorPrefs, the build before it keeps in SessionState, and the
        // last build since the scripts last reloaded.
        private static string[] SdkRecord() => SdkEditorPrefsKeys.Select(EditorPrefs.GetString)
            .Append(SessionState.GetString(SdkBuildRecord.BuildBeforeKey, ""))
            .Append(VRC_SdkBuilder.VRCPath)
            .ToArray();
    }
}

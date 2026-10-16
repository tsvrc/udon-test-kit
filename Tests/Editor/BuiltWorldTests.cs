using System.IO;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;
using Tsvrc.UdonTestKit.Tests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Components;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    public class BuiltWorldTests
    {
        private const string PathRecordedBefore = "Tsvrc.UdonTestKit.BuiltWorldTests.PathRecordedBefore";

        private string _realPathRecordedBefore;
        private BuildableScene _scene;
        private BuiltWorld _world;

        [OneTimeSetUp]
        public void BuildAWorld()
        {
            _realPathRecordedBefore = EditorPrefs.GetString(BuiltWorld.WorldFileKey);
            EditorPrefs.SetString(BuiltWorld.WorldFileKey, PathRecordedBefore);
            _scene = new BuildableScene(typeof(UdonCallLog));
            _world = BuiltWorld.FromWorldScene();
        }

        [OneTimeTearDown]
        public void RemoveTheScene()
        {
            _scene?.Dispose();
            EditorPrefs.SetString(BuiltWorld.WorldFileKey, _realPathRecordedBefore);
        }

        [Test]
        public void HoldsProgramOf_PlacedBehaviour_IsTrue()
        {
            Assert.That(_world.HoldsProgramOf(typeof(UdonCallLog)), Is.True);
        }

        [Test]
        public void HoldsProgramOf_BehaviourNothingPlaces_IsFalse()
        {
            Assert.That(_world.HoldsProgramOf(typeof(UdonProbe)), Is.False);
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
        public void FromWorldScene_AfterTheBuild_PutsBackThePathRecordedBefore()
        {
            Assert.That(EditorPrefs.GetString(BuiltWorld.WorldFileKey), Is.EqualTo(PathRecordedBefore));
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
            BuildableScene.AddUnsavedScene(withDescriptor: false, typeof(UdonProbe));

            // Act
            BuiltWorld world = BuiltWorld.FromWorldScene();

            // Assert
            Assert.That(world.HoldsProgramOf(typeof(UdonCallLog)), Is.True);
            Assert.That(world.HoldsProgramOf(typeof(UdonProbe)), Is.False);
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
        public void FromWorldScene_ExportBuildsNothing_ThrowsAndPutsBackThePathRecordedBefore()
        {
            // Arrange
            using var scene = new BuildableScene();
            string before = EditorPrefs.GetString(BuiltWorld.WorldFileKey);

            // Act
            var failure = Assert.Throws<System.InvalidOperationException>(() => BuiltWorld.FromWorldScene(() =>
            {
                EditorPrefs.SetString(BuiltWorld.WorldFileKey, "A path the failed export recorded");
                return false;
            }));

            // Assert
            Assert.That(failure.Message, Is.EqualTo("The world exporter of the VRChat SDK didn't build the world scene."));
            Assert.That(EditorPrefs.GetString(BuiltWorld.WorldFileKey), Is.EqualTo(before));
        }
    }
}

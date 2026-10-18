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
            _world = BuiltWorld.FromOpenScene();
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
        public void FromOpenScene_AfterReadingTheWorld_LeavesNeitherTheCopyNorWhatWasExtracted()
        {
            Assert.That(Directory.GetFileSystemEntries("Temp", "*world", SearchOption.AllDirectories), Is.Empty);
            Assert.That(Directory.GetFileSystemEntries("Temp", "*world_data", SearchOption.AllDirectories), Is.Empty);
        }

        [Test]
        public void FromOpenScene_AfterTheBuild_PutsBackThePathRecordedBefore()
        {
            Assert.That(EditorPrefs.GetString(BuiltWorld.WorldFileKey), Is.EqualTo(PathRecordedBefore));
        }

        [Test]
        public void FromOpenScene_AfterReadingTheWorld_DeletesTheWorldFile()
        {
            Assert.That(_world.FilePath, Is.Not.Null.And.Not.Empty);
            Assert.That(File.Exists(_world.FilePath), Is.False, $"'{_world.FilePath}' is still there.");
        }

        // Had the exporter run, the dialog it cancels in batch mode would fail the test with its log.
        [TestCase(0, 0)]
        [TestCase(2, 0)]
        [TestCase(1, 1)]
        public void FromOpenScene_NotExactlyOneDescriptor_ThrowsBeforeTheExporterAsks(int active, int inactive)
        {
            // Arrange
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            for (int i = 0; i < active + inactive; i++)
            {
                var descriptor = new GameObject("Descriptor");
                descriptor.AddComponent<VRCSceneDescriptor>();
                descriptor.SetActive(i < active);
            }

            // Act and assert
            Assert.That(() => BuiltWorld.FromOpenScene(), Throws.InvalidOperationException.With.Message.EqualTo(
                $"The open scene has {active + inactive} scene descriptors. The world exporter of the VRChat SDK builds a scene with exactly one."));
        }

        [Test]
        public void FromOpenScene_SceneNeverSaved_ThrowsBeforeTheExporterAsks()
        {
            // Arrange
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Descriptor").AddComponent<VRCSceneDescriptor>();

            // Act and assert
            Assert.That(() => BuiltWorld.FromOpenScene(), Throws.InvalidOperationException.With.Message.EqualTo(
                "The open scene was never saved. The world exporter of the VRChat SDK builds only a saved scene."));
        }

        [Test]
        public void FromOpenScene_OnlyDescriptorInactive_BuildsTheWorld()
        {
            // Arrange
            using var scene = new BuildableScene(typeof(UdonCallLog));
            Object.FindObjectOfType<VRCSceneDescriptor>().gameObject.SetActive(false);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            // Act
            BuiltWorld world = BuiltWorld.FromOpenScene();

            // Assert
            Assert.That(world.HoldsProgramOf(typeof(UdonCallLog)), Is.True);
        }
    }
}

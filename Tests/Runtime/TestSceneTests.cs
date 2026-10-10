using System.Collections;
using NUnit.Framework;
using Tsvrc.TestKit.Tests.Helpers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tsvrc.TestKit.Tests
{
    [PrebuildSetup(typeof(TestWorldScene)), PostBuildCleanup(typeof(TestWorldScene))]
    public class TestSceneTests
    {
        private const string SceneName = "Tsvrc.TestKit.TestSceneTests";

        private TestScene _scene;

        [TearDown]
        public void TearDown()
        {
            _scene?.Unload();
            _scene = null;
        }

        [UnityTest]
        public IEnumerator Load_Empty_BecomesTheActiveScene()
        {
            _scene = TestScene.Empty(SceneName);

            yield return _scene.Load();

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneName));
        }

        [UnityTest]
        public IEnumerator Load_FromAsset_BecomesTheActiveScene()
        {
            _scene = TestScene.FromAsset(TestWorldScene.Path);

            yield return _scene.Load();

            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(TestWorldScene.Path));
        }

        [UnityTest]
        public IEnumerator Load_AlreadyLoaded_KeepsTheSameScene()
        {
            // Arrange
            _scene = TestScene.Empty(SceneName);
            yield return _scene.Load();
            Scene loaded = SceneManager.GetActiveScene();

            // Act
            yield return _scene.Load();

            // Assert
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(loaded));
        }

        [UnityTest]
        public IEnumerator Unload_ObjectCreatedInTheScene_IsDestroyedBeforeItReturns()
        {
            // Arrange
            _scene = TestScene.Empty(SceneName);
            yield return _scene.Load();
            var created = new GameObject(nameof(TestSceneTests));

            // Act
            _scene.Unload();

            // Assert
            Assert.That(created == null, "The object the test created outlived its scene.");
        }

        [UnityTest]
        public IEnumerator Unload_AfterLoad_GivesBackThePreviousActiveScene()
        {
            // Arrange
            Scene before = SceneManager.GetActiveScene();
            _scene = TestScene.Empty(SceneName);
            yield return _scene.Load();

            // Act
            _scene.Unload();

            // Assert
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator Load_EmptyRightAfterUnload_CreatesItAgain()
        {
            // Arrange
            _scene = TestScene.Empty(SceneName);
            yield return _scene.Load();
            _scene.Unload();

            // Act
            yield return _scene.Load();

            // Assert
            Assert.That(_scene.IsLoaded, Is.True);
        }
    }
}

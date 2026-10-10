using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Tests.Helpers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace Tsvrc.UdonTestKit.Tests
{
    [PrebuildSetup(typeof(TestWorldScene)), PostBuildCleanup(typeof(TestWorldScene))]
    public class ClientSimTestBaseWorldSceneTests : ClientSimTestBase
    {
        public ClientSimTestBaseWorldSceneTests() : base(TestScene.FromAsset(TestWorldScene.Path))
        {
        }

        [UnityTest]
        public IEnumerator Start_InTheWorldScene_SpawnsAtItsSpawnPointWithItsOwnDescriptor()
        {
            yield return Session.Start();

            Assert.That(Vector3.Distance(Networking.LocalPlayer.GetPosition(), TestWorldScene.SpawnPosition), Is.LessThan(0.5f));
            Assert.That(Resources.FindObjectsOfTypeAll<VRCSceneDescriptor>().Count(descriptor => descriptor.gameObject.scene.IsValid()), Is.EqualTo(1));
        }
    }

    public class ClientSimTestBasePerTestSceneTests : ClientSimTestBase
    {
        private const string SceneName = "Tsvrc.UdonTestKit.PerTestScene";

        private readonly List<int> _sceneHandles = new List<int>();

        public ClientSimTestBasePerTestSceneTests() : base(TestScene.Empty(SceneName))
        {
        }

        // Runs after the base class's [UnitySetUp].
        [UnitySetUp]
        public IEnumerator CheckTheSceneLoadedBeforeThisSetUp()
        {
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneName));
            yield break;
        }

        // Runs after the base class's [TearDown] and before its [UnityTearDown].
        [UnityTearDown]
        public IEnumerator CheckTheSessionEndedBeforeTheSceneUnloads()
        {
            Assert.That(ClientSimMain.HasInstance(), Is.False, "ClientSim is still running.");
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneName), "The scene already unloaded.");
            yield break;
        }

        [UnityTest]
        public IEnumerator Start_EachTest_GetsItsOwnScene([Values(1, 2)] int test)
        {
            // Act
            yield return Session.Start();

            // Assert
            Scene scene = SceneManager.GetActiveScene();
            Assert.That(scene.name, Is.EqualTo(SceneName));
            Assert.That(_sceneHandles, Has.No.Member(scene.handle));
            _sceneHandles.Add(scene.handle);
        }
    }

    public class ClientSimTestBasePerFixtureSceneTests : ClientSimTestBase
    {
        private const string SceneName = "Tsvrc.UdonTestKit.PerFixtureScene";

        private int? _firstSceneHandle;

        public ClientSimTestBasePerFixtureSceneTests() : base(TestScene.Empty(SceneName), TestSceneLifetime.PerFixture)
        {
        }

        [UnityTest]
        public IEnumerator Start_EachTest_SharesTheFixturesScene([Values(1, 2)] int test)
        {
            // Act
            yield return Session.Start();

            // Assert
            Scene scene = SceneManager.GetActiveScene();
            _firstSceneHandle ??= scene.handle;
            Assert.That(scene.name, Is.EqualTo(SceneName));
            Assert.That(scene.handle, Is.EqualTo(_firstSceneHandle));
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tsvrc.UdonTestKit.Tests
{
    public class UdonTestBaseTests : UdonTestBase
    {
        private GameObject _created;

        // Runs after every [TearDown], so it sees what the base class left after the test.
        [UnityTearDown]
        public IEnumerator CheckWhatTheTestCreatedIsGone()
        {
            GameObject created = _created;
            _created = null;
            Assert.That(created == null, Is.True, "UdonTestBase didn't destroy what the test created.");
            yield break;
        }

        [Test]
        public void DisposeUdonHost_AfterATest_DestroysWhatTheTestCreated()
        {
            // Act
            _created = Udon.Create<UdonProbe>("Probe").gameObject;

            // Assert: in CheckWhatTheTestCreatedIsGone.
        }
    }

    public class UdonTestBasePerTestSceneTests : UdonTestBase
    {
        private const string SceneName = "Tsvrc.UdonTestKit.UdonPerTestScene";

        private readonly List<int> _sceneHandles = new List<int>();

        public UdonTestBasePerTestSceneTests() : base(TestScene.Empty(SceneName))
        {
        }

        [Test]
        public void Create_EachTest_GoesInTheTestsOwnScene([Values(1, 2)] int test)
        {
            // Act
            Scene scene = Udon.Create<UdonProbe>("Probe").gameObject.scene;

            // Assert
            Assert.That(scene.name, Is.EqualTo(SceneName));
            Assert.That(_sceneHandles, Has.No.Member(scene.handle));
            _sceneHandles.Add(scene.handle);
        }
    }

    public class UdonTestBasePerFixtureSceneTests : UdonTestBase
    {
        private const string SceneName = "Tsvrc.UdonTestKit.UdonPerFixtureScene";

        private int? _firstSceneHandle;

        public UdonTestBasePerFixtureSceneTests() : base(TestScene.Empty(SceneName), TestSceneLifetime.PerFixture)
        {
        }

        [Test]
        public void Create_EachTest_GoesInTheFixturesScene([Values(1, 2)] int test)
        {
            // Act
            Scene scene = Udon.Create<UdonProbe>("Probe").gameObject.scene;

            // Assert
            _firstSceneHandle ??= scene.handle;
            Assert.That(scene.name, Is.EqualTo(SceneName));
            Assert.That(scene.handle, Is.EqualTo(_firstSceneHandle));
        }
    }
}

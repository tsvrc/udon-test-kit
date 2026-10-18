using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tsvrc.UdonTestKit
{
    /// <summary>
    /// Base for Play Mode tests that use ClientSim. Start <see cref="Session"/> in a test; it ends
    /// after every test, after the teardown of the derived class. Tests run in the Unity Test Framework
    /// test scene, shared by the whole run, unless the fixture passes its own <see cref="TestScene"/>.
    /// </summary>
    public abstract class ClientSimTestBase
    {
        protected readonly ClientSimSession Session = new ClientSimSession();

        private readonly TestScene _scene;
        private readonly TestSceneLifetime _sceneLifetime;

        protected ClientSimTestBase()
        {
        }

        /// <summary>Runs every test in <paramref name="scene"/>, loaded before the setup of the
        /// derived class and unloaded once ClientSim has ended.</summary>
        protected ClientSimTestBase(TestScene scene, TestSceneLifetime lifetime = TestSceneLifetime.PerTest)
        {
            _scene = scene;
            _sceneLifetime = lifetime;
        }

        [UnitySetUp]
        public IEnumerator LoadTestScene()
        {
            if (_scene != null)
            {
                yield return _scene.Load();
            }
        }

        [TearDown]
        public void EndClientSimSession()
        {
            Session.End();
        }

        // [UnityTearDown] runs after every [TearDown], so the session has ended first.
        [UnityTearDown]
        public IEnumerator UnloadPerTestScene()
        {
            if (_sceneLifetime == TestSceneLifetime.PerTest)
            {
                _scene?.Unload();
            }
            yield break;
        }

        [OneTimeTearDown]
        public void UnloadPerFixtureScene()
        {
            _scene?.Unload();
        }
    }
}

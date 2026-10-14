using System;
using System.Collections;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Tsvrc.UdonTestKit
{
    /// <summary>
    /// A scene for Play Mode tests to run in: a new empty scene, or a scene asset such as the world's
    /// own scene. Loading it makes it the active scene, so ClientSim and the objects a test creates
    /// go into it, and unloading it destroys them.
    /// </summary>
    public sealed class TestScene
    {
        private readonly string _name;
        private readonly string _assetPath;
        private Scene _scene;
        private Scene _previousActiveScene;

        private TestScene(string name, string assetPath)
        {
            _name = name;
            _assetPath = assetPath;
        }

        /// <summary>A new empty scene with this name, created each time it's loaded.</summary>
        public static TestScene Empty(string name) => new TestScene(name, null);

        /// <summary>The scene asset at this path, such as "Assets/Scenes/MyWorld.unity". It doesn't
        /// need to be in the build settings.</summary>
        public static TestScene FromAsset(string assetPath) => new TestScene(null, assetPath);

        public bool IsLoaded => _scene.isLoaded;

        /// <summary>Loads the scene alongside the scenes already loaded and makes it the active scene.
        /// Does nothing if it's already loaded.</summary>
        public IEnumerator Load()
        {
            if (IsLoaded)
            {
                yield break;
            }

            _previousActiveScene = SceneManager.GetActiveScene();
            if (_assetPath == null)
            {
                _scene = SceneManager.CreateScene(_name);
            }
            else
            {
                _scene = EditorSceneManager.LoadSceneInPlayMode(_assetPath, new LoadSceneParameters(LoadSceneMode.Additive));
                while (!_scene.isLoaded)
                {
                    yield return null;
                }
                // LoadSceneInPlayMode drops the operation it loads with. Finalized on Unity's exit
                // instead, it crashed Unity; finalizing it now, while the scene is loaded, doesn't.
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            SceneManager.SetActiveScene(_scene);
        }

        /// <summary>Makes the scene that was active before it active again, and unloads it, destroying
        /// everything in it. Takes effect before it returns, so it also works where a test can't wait,
        /// such as [OneTimeTearDown]. Does nothing if the scene isn't loaded.</summary>
        public void Unload()
        {
            if (!IsLoaded)
            {
                return;
            }

            if (SceneManager.GetActiveScene() == _scene && _previousActiveScene.IsValid() && _previousActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(_previousActiveScene);
            }
            // The synchronous unload, obsolete because it's unsafe inside physics callbacks, which a
            // teardown isn't. It finishes before returning, unlike UnloadSceneAsync.
#pragma warning disable CS0618
            SceneManager.UnloadScene(_scene);
#pragma warning restore CS0618
            _scene = default;
        }
    }
}

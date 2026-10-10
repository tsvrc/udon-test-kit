using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tsvrc.TestKit.Tests.Helpers
{
    // A saved scene standing in for a world's own scene: a scene descriptor with one spawn point.
    // Play Mode can't save scenes, so it's written before the run and deleted after it.
    internal sealed class TestWorldScene : IPrebuildSetup, IPostBuildCleanup
    {
        public const string Path = "Assets/TestKitTestWorld.unity";
        public static readonly Vector3 SpawnPosition = new Vector3(3f, 0f, 4f);

        public void Setup()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.MoveGameObjectToScene(ClientSimSession.CreateSceneDescriptor(SpawnPosition), scene);
            EditorSceneManager.SaveScene(scene, Path);
            EditorSceneManager.CloseScene(scene, removeScene: true);
        }

        public void Cleanup()
        {
            AssetDatabase.DeleteAsset(Path);
        }
    }
}

using System;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Core;
using VRC.SDK3.Components;

namespace Tsvrc.UdonTestKit.Editor.Tests.Helpers
{
    // A saved scene the VRChat SDK's world exporter builds, open, with a GameObject for each UdonSharp
    // class it's given. Disposing it opens an empty scene, which closes every other, and deletes it.
    internal sealed class BuildableScene : IDisposable
    {
        private readonly string _folder;

        public BuildableScene(params Type[] placed)
        {
            _folder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder("Assets", "UdonTestKitScene"));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddDescriptor();
            Place(placed);
            EditorSceneManager.SaveScene(scene, _folder + "/World.unity");
        }

        public Scene Scene => SceneManager.GetSceneByPath(_folder + "/World.unity");

        // Adds a scene descriptor, as the world scene of a VRChat world has, to the active scene.
        public static GameObject AddDescriptor(bool active = true)
        {
            var descriptor = new GameObject("Descriptor");
            descriptor.AddComponent<VRCSceneDescriptor>().spawns = new[] { descriptor.transform };
            descriptor.AddComponent<PipelineManager>();
            descriptor.SetActive(active);
            return descriptor;
        }

        // Opens a new scene that is never saved next to the loaded ones and makes it the active scene.
        public static Scene AddUnsavedScene(bool withDescriptor, params Type[] placed)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            if (withDescriptor)
            {
                AddDescriptor();
            }
            Place(placed);
            return scene;
        }

        public void Dispose()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(_folder);
        }

        private static void Place(Type[] placed)
        {
            foreach (Type type in placed)
            {
                UdonSharpUndo.AddComponent(new GameObject(type.Name), type);
            }
        }
    }
}

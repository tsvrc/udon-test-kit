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
    // class it's given. Disposing it opens an empty scene and deletes it.
    internal sealed class BuildableScene : IDisposable
    {
        private readonly string _folder;

        public BuildableScene(params Type[] placed)
        {
            _folder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder("Assets", "UdonTestKitScene"));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var descriptor = new GameObject("Descriptor");
            descriptor.AddComponent<VRCSceneDescriptor>().spawns = new[] { descriptor.transform };
            descriptor.AddComponent<PipelineManager>();
            foreach (Type type in placed)
            {
                UdonSharpUndo.AddComponent(new GameObject(type.Name), type);
            }
            EditorSceneManager.SaveScene(scene, _folder + "/World.unity");
        }

        public void Dispose()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(_folder);
        }
    }
}

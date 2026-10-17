using System;
using System.Linq;
using UdonSharp;
using UnityEditor;
using UnityEngine;

namespace Tsvrc.UdonTestKit.Editor.Tests.Helpers
{
    // A program asset for an UdonSharp class that has none. Disposing it deletes it, along with the
    // program a compile kept for it.
    internal sealed class TemporaryProgramAsset : IDisposable
    {
        private const string Folder = "Assets/UdonTestKitTemp";

        public TemporaryProgramAsset(Type script)
        {
            Path = $"{Folder}/{script.Name}.asset";
            AssetDatabase.CreateFolder("Assets", "UdonTestKitTemp");
            var asset = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
            asset.sourceCsScript = AssetDatabase.FindAssets(script.Name + " t:MonoScript")
                .Select(guid => AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid)))
                .Single(candidate => candidate.GetClass() == script);
            AssetDatabase.CreateAsset(asset, Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            ProjectScripts.DeleteCompiledPrograms(Folder);
            AssetDatabase.DeleteAsset(Folder);
        }
    }
}

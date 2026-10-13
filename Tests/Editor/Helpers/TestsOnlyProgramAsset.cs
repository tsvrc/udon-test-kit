using System;
using System.Linq;
using Tsvrc.UdonTestKit.Tests;
using UdonSharp;
using UnityEditor;
using UnityEngine;

namespace Tsvrc.UdonTestKit.Editor.Tests.Helpers
{
    // A program asset for TestsOnlyBehaviour. Disposing it deletes it.
    internal sealed class TestsOnlyProgramAsset : IDisposable
    {
        private const string Folder = "Assets/UdonTestKitTemp";

        public TestsOnlyProgramAsset()
        {
            AssetDatabase.CreateFolder("Assets", "UdonTestKitTemp");
            var asset = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
            asset.sourceCsScript = AssetDatabase.FindAssets(nameof(TestsOnlyBehaviour) + " t:MonoScript")
                .Select(guid => AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid)))
                .Single(script => script.GetClass() == typeof(TestsOnlyBehaviour));
            AssetDatabase.CreateAsset(asset, Folder + "/TestsOnlyBehaviour.asset");
        }

        public void Dispose()
        {
            AssetDatabase.DeleteAsset(Folder);
        }
    }
}

using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim.Persistence;

namespace UdonTestKit.Editor.Tests.Problems
{
    // Problem: Unity behaviors the kit's code has to handle. Resources.FindObjectsOfTypeAll also
    // returns loaded prefab assets, whose scene isn't valid; DestroyImmediate refuses an asset
    // without allowDestroyingAssets; and a destroyed object is null only through Unity's ==.
    // Solved by the scene guard and plain DestroyImmediate in ClientSimSession.DestroySavedDataObjects
    // (tested in Solutions/ClientSimSessionTests), and by SavedData.CountAlive.
    // A failure here means Unity changed, so that handling may no longer be needed.
    public class UnityObjectProblemTests
    {
        private const string PlayerDataPrefabResourcePath = "ClientSim/Prefabs/ClientSimPlayerData";

        [Test]
        public void FindObjectsOfTypeAll_PlayerDataPrefabLoaded_ReturnsTheAssetOutsideAnyScene()
        {
            // Arrange
            var prefab = Resources.Load<GameObject>(PlayerDataPrefabResourcePath);
            Assume.That(prefab, Is.Not.Null, $"ClientSim's prefab moved from Resources/{PlayerDataPrefabResourcePath}.");

            // Act
            ClientSimPlayerObjectStorage[] found = Resources.FindObjectsOfTypeAll<ClientSimPlayerObjectStorage>();

            // Assert
            ClientSimPlayerObjectStorage fromAsset = found.FirstOrDefault(storage => storage.gameObject == prefab);
            Assert.That(fromAsset != null, "FindObjectsOfTypeAll didn't return the loaded prefab asset.");
            Assert.That(fromAsset.gameObject.scene.IsValid(), Is.False);
        }

        [Test]
        public void DestroyImmediate_PrefabAssetWithoutAllowDestroyingAssets_IsRefused()
        {
            // Arrange
            var prefab = Resources.Load<GameObject>(PlayerDataPrefabResourcePath);
            Assume.That(prefab, Is.Not.Null, $"ClientSim's prefab moved from Resources/{PlayerDataPrefabResourcePath}.");
            LogAssert.Expect(LogType.Error, new Regex("Destroying assets is not permitted"));

            // Act
            Object.DestroyImmediate(prefab);

            // Assert
            Assert.That(prefab != null, "DestroyImmediate destroyed a prefab asset without allowDestroyingAssets.");
        }

        [Test]
        public void DestroyImmediate_GameObject_IsNullOnlyThroughUnityEquality()
        {
            // Arrange
            var gameObject = new GameObject(nameof(UnityObjectProblemTests));

            // Act
            Object.DestroyImmediate(gameObject);

            // Assert
            Assert.That(gameObject == null, Is.True, "Unity's == should report a destroyed object as null.");
            Assert.That((object)gameObject, Is.Not.Null, "The C# reference should survive destruction.");
        }
    }
}

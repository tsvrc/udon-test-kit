using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim.Persistence;

namespace UdonTestKit.Editor.Tests.Learning
{
    // FindObjectsOfTypeAll also returns prefab assets, DestroyImmediate refuses an asset, and a
    // destroyed object is null only through Unity's ==. ClientSimSession.SavedDataObjects and the
    // saved-data tests rely on all three.
    public class UnityObjectTests
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
            var gameObject = new GameObject(nameof(UnityObjectTests));

            // Act
            Object.DestroyImmediate(gameObject);

            // Assert
            Assert.That(gameObject == null, Is.True, "Unity's == should report a destroyed object as null.");
            Assert.That((object)gameObject, Is.Not.Null, "The C# reference should survive destruction.");
        }
    }
}

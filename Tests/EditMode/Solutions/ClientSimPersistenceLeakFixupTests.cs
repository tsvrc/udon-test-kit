using NUnit.Framework;
using UdonTestKit.ClientSim;
using UnityEngine;
using VRC.SDK3.ClientSim.Persistence;

namespace UdonTestKit.Tests.EditMode.Solutions
{
    // Solution: ClientSimPersistenceLeakFixup destroys leaked storages, tested here against hand-made
    // stand-ins. The problem is proven in PlayMode/Problems/ClientSimPersistenceLeakProblemTests.
    public class ClientSimPersistenceLeakFixupTests
    {
        private const string PlayerDataPrefabResourcePath = "ClientSim/Prefabs/ClientSimPlayerData";

        private GameObject _leaked;

        [TearDown]
        public void TearDown()
        {
            if (_leaked != null)
                Object.DestroyImmediate(_leaked);
        }

        [Test]
        public void OnUnityTearDown_HiddenStorageInScene_DestroysIt()
        {
            _leaked = new GameObject("LeakedClientSimPlayerObjectStorage");
            _leaked.AddComponent<ClientSimPlayerObjectStorage>();
            _leaked.hideFlags = HideFlags.HideInHierarchy;

            new ClientSimPersistenceLeakFixup().OnUnityTearDown();

            Assert.That(_leaked == null, "The leaked ClientSimPlayerObjectStorage was not destroyed.");
        }

        [Test]
        public void OnUnityTearDown_NoStorageInScene_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new ClientSimPersistenceLeakFixup().OnUnityTearDown());
        }

        [Test]
        public void OnUnityTearDown_ClientSimPlayerDataPrefabLoaded_LeavesTheAssetIntact()
        {
            // Arrange
            var prefab = Resources.Load<GameObject>(PlayerDataPrefabResourcePath);
            Assume.That(prefab, Is.Not.Null, $"ClientSim's prefab moved from Resources/{PlayerDataPrefabResourcePath}.");
            Assume.That(prefab.GetComponent<ClientSimPlayerObjectStorage>(), Is.Not.Null);

            // Act
            new ClientSimPersistenceLeakFixup().OnUnityTearDown();

            // Assert
            Assert.That(prefab != null, "The fixup destroyed ClientSim's own prefab asset, not just a scene leak.");
        }
    }
}

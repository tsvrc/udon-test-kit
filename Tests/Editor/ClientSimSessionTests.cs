using System.IO;
using NUnit.Framework;
using UnityEngine;
using VRC.SDK3.ClientSim.Persistence;

namespace Tsvrc.TestKit.Editor.Tests
{
    public class ClientSimSessionTests
    {
        private const string PlayerDataPrefabResourcePath = "ClientSim/Prefabs/ClientSimPlayerData";

        private GameObject _leaked;
        private string _storageRoot;

        [TearDown]
        public void TearDown()
        {
            if (_leaked != null)
            {
                Object.DestroyImmediate(_leaked);
            }
            if (_storageRoot != null)
            {
                Directory.Delete(_storageRoot, recursive: true);
            }
            _storageRoot = null;
        }

        [Test]
        public void DestroySavedDataObjects_HiddenStorageInScene_DestroysIt()
        {
            // Arrange
            _leaked = new GameObject("LeakedClientSimPlayerObjectStorage");
            _leaked.AddComponent<ClientSimPlayerObjectStorage>();
            _leaked.hideFlags = HideFlags.HideInHierarchy;

            // Act
            ClientSimSession.DestroySavedDataObjects();

            // Assert
            Assert.That(_leaked == null, "The leaked storage was not destroyed.");
        }

        [Test]
        public void DestroySavedDataObjects_ClientSimPrefabLoaded_LeavesTheAssetIntact()
        {
            // Arrange
            var prefab = Resources.Load<GameObject>(PlayerDataPrefabResourcePath);
            Assume.That(prefab, Is.Not.Null, $"ClientSim's prefab moved from Resources/{PlayerDataPrefabResourcePath}.");
            Assume.That(prefab.GetComponent<ClientSimPlayerObjectStorage>(), Is.Not.Null);

            // Act
            ClientSimSession.DestroySavedDataObjects();

            // Assert
            Assert.That(prefab != null, "ClientSim's own prefab asset was destroyed.");
        }

        [Test]
        public void DeleteSavedData_FilesForSeveralScenes_DeletesOnlyTheGivenScenes()
        {
            // Arrange
            _storageRoot = Path.Combine(Path.GetTempPath(), "Tsvrc.TestKit.ClientSimSessionTests");
            string data = CreateFile("PlayerData", "PlayerData_1_World.json");
            string objects = CreateFile("PlayerObjects", "PlayerObject_2_World.json");
            string otherScene = CreateFile("PlayerData", "PlayerData_1_My_World.json");

            // Act
            ClientSimSession.DeleteSavedData(_storageRoot, "World");

            // Assert
            Assert.That(File.Exists(data), Is.False);
            Assert.That(File.Exists(objects), Is.False);
            Assert.That(File.Exists(otherScene), Is.True);
        }

        private string CreateFile(string folder, string name)
        {
            string path = Path.Combine(_storageRoot, folder, name);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "{}");
            return path;
        }
    }
}

using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.ClientSim.Persistence;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    public class ClientSimSessionTests
    {
        private const string PlayerDataPrefabResourcePath = "ClientSim/Prefabs/ClientSimPlayerData";

        private GameObject _leaked;
        private string _storageRoot;

        [SetUp]
        public void SetUp() => _storageRoot = Path.GetFullPath(FileUtil.GetUniqueTempPathInProject());

        [TearDown]
        public void TearDown()
        {
            if (_leaked != null)
            {
                Object.DestroyImmediate(_leaked);
            }
            if (Directory.Exists(_storageRoot))
            {
                Directory.Delete(_storageRoot, recursive: true);
            }
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

        [Test]
        public void DeleteSavedData_SceneNameWithARegexCharacter_MatchesItAsWritten()
        {
            // Arrange
            string data = CreateFile("PlayerData", "PlayerData_1_My.World.json");
            string lookalike = CreateFile("PlayerData", "PlayerData_1_MyxWorld.json");

            // Act
            ClientSimSession.DeleteSavedData(_storageRoot, "My.World");

            // Assert
            Assert.That(File.Exists(data), Is.False);
            Assert.That(File.Exists(lookalike), Is.True);
        }

        [Test]
        public void Constructor_Default_StartsClientSimAtOnceAndWaitsTenSeconds()
        {
            // Act
            var session = new ClientSimSession();

            // Assert
            Assert.That(PrivateFieldAccess.GetField<float>(session, "_initializationDelay"), Is.Zero);
            Assert.That(session.ReadyTimeoutSeconds, Is.EqualTo(10f));
        }

        [Test]
        public void ReadyTimeoutSeconds_SetAboveZero_IsKept()
        {
            // Arrange
            var session = new ClientSimSession();

            // Act
            session.ReadyTimeoutSeconds = 0.5f;

            // Assert
            Assert.That(session.ReadyTimeoutSeconds, Is.EqualTo(0.5f));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void ReadyTimeoutSeconds_NotAFiniteNumberAboveZero_ThrowsAndKeepsTheTimeout(float seconds)
        {
            // Arrange
            var session = new ClientSimSession();

            // Act
            var failure = Assert.Throws<System.ArgumentOutOfRangeException>(() => session.ReadyTimeoutSeconds = seconds);

            // Assert
            Assert.That(failure.Message, Does.StartWith("The timeout needs a finite number of seconds above 0."));
            Assert.That(session.ReadyTimeoutSeconds, Is.EqualTo(10f));
        }

        [Test]
        public void DeleteSavedData_NoStorageFolder_DoesNotThrow()
        {
            Assert.That(() => ClientSimSession.DeleteSavedData(_storageRoot, "World"), Throws.Nothing);
        }

        [Test]
        public void DeleteTestSceneSavedData_FilesOfSeveralScenes_DeletesOnlyThoseOfTestScenes()
        {
            // Arrange
            string data = CreateFile("PlayerData", "PlayerData_1_InitTestScene639276975825145332.json");
            string objects = CreateFile("PlayerObjects", "PlayerObject_2_InitTestScene639276991293267109.json");
            string world = CreateFile("PlayerData", "PlayerData_1_World.json");
            string noDigits = CreateFile("PlayerData", "PlayerData_1_InitTestScene.json");
            string prefixed = CreateFile("PlayerData", "PlayerData_1_MyInitTestScene1.json");
            string suffixed = CreateFile("PlayerObjects", "PlayerObject_1_InitTestScene1Copy.json");

            // Act
            ClientSimSession.DeleteTestSceneSavedData(_storageRoot);

            // Assert
            Assert.That(File.Exists(data), Is.False);
            Assert.That(File.Exists(objects), Is.False);
            Assert.That(new[] { world, noDigits, prefixed, suffixed }, Has.All.Matches<string>(File.Exists));
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

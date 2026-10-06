using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UdonTestKit.Tests.PlayMode.Helpers;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim.Persistence;

namespace UdonTestKit.Tests.PlayMode.Solutions
{
    // Solution: ClientSimPersistenceLeakFixup destroys the storage a real ClientSim session leaks,
    // which stops its save file writes. The problem is proven in Problems/ClientSimPersistenceLeakProblemTests.
    public class ClientSimPersistenceLeakFixupTests
    {
        private const float WriteWindowSeconds = 1f;
        private const float SettleSeconds = 0.5f;

        private readonly ClientSimPlayerEnvironment _players = new ClientSimPlayerEnvironment();
        private List<ClientSimPlayerObjectStorage> _existingBefore;

        [SetUp]
        public void SetUp() => _existingBefore = PlayerObjectStorages.FindInScene();

        [TearDown]
        public void TearDown()
        {
            _players.Teardown();
            new ClientSimPersistenceLeakFixup().OnUnityTearDown();
        }

        [UnityTest]
        public IEnumerator OnUnityTearDown_AfterRealSession_DestroysTheLeakedStorage()
        {
            // Arrange
            yield return _players.Start();
            List<ClientSimPlayerObjectStorage> created = CreatedStorages();
            Assume.That(created, Is.Not.Empty, PlayerObjectStorages.PersistenceOffMessage);
            _players.Teardown();

            // Act
            new ClientSimPersistenceLeakFixup().OnUnityTearDown();

            // Assert
            Assert.That(PlayerObjectStorages.CountAlive(created), Is.Zero);
        }

        [UnityTest]
        public IEnumerator OnUnityTearDown_AfterRealSession_StopsSaveFileWrites()
        {
            // Arrange
            yield return _players.Start();
            ClientSimPlayerObjectStorage storage = CreatedStorages().FirstOrDefault();
            Assume.That(storage != null, PlayerObjectStorages.PersistenceOffMessage);
            string saveFile = PlayerObjectStorages.FilePathOf(storage);
            Assume.That(File.Exists(saveFile), Is.True, "The storage never created its save file.");
            _players.Teardown();

            // Act
            new ClientSimPersistenceLeakFixup().OnUnityTearDown();
            yield return new WaitForSecondsRealtime(SettleSeconds);
            DateTime lastWriteAfterFixup = File.GetLastWriteTimeUtc(saveFile);
            yield return new WaitForSecondsRealtime(WriteWindowSeconds);

            // Assert
            Assert.That(File.GetLastWriteTimeUtc(saveFile), Is.EqualTo(lastWriteAfterFixup));
        }

        private List<ClientSimPlayerObjectStorage> CreatedStorages() =>
            PlayerObjectStorages.FindInScene().Except(_existingBefore).ToList();
    }
}

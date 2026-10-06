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

namespace UdonTestKit.Tests.PlayMode.Problems
{
    // Problem: with player persistence on, every ClientSim session creates a hidden, unparented
    // ClientSimPlayerObjectStorage that ClientSim's own teardown never destroys. It keeps rewriting
    // its save file, one piles up per session, and they all share one file.
    // Solved by ClientSimPersistenceLeakFixup, tested in Solutions/ClientSimPersistenceLeakFixupTests.
    // A failure here means ClientSim changed, so the fixup may no longer be needed.
    public class ClientSimPersistenceLeakProblemTests
    {
        // Long enough for a live storage to save again.
        private const float WriteWindowSeconds = 1f;
        // A pause before recording the file time, so a save started before teardown isn't counted as one after it.
        private const float SettleSeconds = 0.5f;

        private readonly ClientSimPlayerEnvironment _players = new ClientSimPlayerEnvironment();
        private List<ClientSimPlayerObjectStorage> _existingBefore;

        [SetUp]
        public void SetUp() => _existingBefore = PlayerObjectStorages.FindInScene();

        // Cleans up with the fixup only after each test has observed ClientSim without it.
        [TearDown]
        public void TearDown()
        {
            _players.Teardown();
            new ClientSimPersistenceLeakFixup().OnUnityTearDown();
        }

        [UnityTest]
        public IEnumerator Start_PlayerPersistenceOn_StorageIsAHiddenRootObject()
        {
            // Act
            yield return _players.Start();

            // Assert
            ClientSimPlayerObjectStorage storage = CreatedStorages().FirstOrDefault();
            Assume.That(storage != null, PlayerObjectStorages.PersistenceOffMessage);
            Assert.That(storage.transform.parent, Is.Null);
            Assert.That(storage.gameObject.hideFlags & HideFlags.HideInHierarchy, Is.EqualTo(HideFlags.HideInHierarchy));
        }

        [UnityTest]
        public IEnumerator Teardown_PlayerPersistenceOn_LeavesTheStorageAlive()
        {
            // Arrange
            yield return _players.Start();
            List<ClientSimPlayerObjectStorage> created = CreatedStorages();
            Assume.That(created, Is.Not.Empty, PlayerObjectStorages.PersistenceOffMessage);

            // Act
            _players.Teardown();
            // Lets any Object.Destroy ClientSim queued actually run, so a deferred cleanup isn't
            // mistaken for a leak.
            yield return null;

            // Assert
            Assert.That(PlayerObjectStorages.CountAlive(created), Is.EqualTo(created.Count));
        }

        [UnityTest]
        public IEnumerator Teardown_LeakedStorage_KeepsRewritingItsSaveFile()
        {
            // Arrange
            yield return _players.Start();
            ClientSimPlayerObjectStorage storage = CreatedStorages().FirstOrDefault();
            Assume.That(storage != null, PlayerObjectStorages.PersistenceOffMessage);
            string saveFile = PlayerObjectStorages.FilePathOf(storage);
            _players.Teardown();
            yield return new WaitForSecondsRealtime(SettleSeconds);
            DateTime lastWriteAfterTeardown = File.GetLastWriteTimeUtc(saveFile);

            // Act
            yield return new WaitForSecondsRealtime(WriteWindowSeconds);

            // Assert
            Assert.That(File.GetLastWriteTimeUtc(saveFile), Is.GreaterThan(lastWriteAfterTeardown));
        }

        [UnityTest]
        public IEnumerator Teardown_BackToBackSessions_OneLeakPilesUpPerSession()
        {
            // Arrange
            yield return _players.Start();
            _players.Teardown();
            int leakedByFirstSession = CreatedStorages().Count;
            Assume.That(leakedByFirstSession, Is.GreaterThan(0), PlayerObjectStorages.PersistenceOffMessage);

            // Act
            yield return _players.Start();
            _players.Teardown();
            yield return null;

            // Assert
            Assert.That(CreatedStorages().Count, Is.EqualTo(leakedByFirstSession * 2));
        }

        [UnityTest]
        public IEnumerator Teardown_BackToBackSessions_LeaksShareOneSaveFile()
        {
            // Arrange
            yield return _players.Start();
            _players.Teardown();

            // Act
            yield return _players.Start();
            _players.Teardown();

            // Assert
            List<ClientSimPlayerObjectStorage> leaked = CreatedStorages();
            Assume.That(leaked.Count, Is.EqualTo(2), PlayerObjectStorages.PersistenceOffMessage);
            Assert.That(PlayerObjectStorages.FilePathOf(leaked[1]), Is.EqualTo(PlayerObjectStorages.FilePathOf(leaked[0])));
        }

        private List<ClientSimPlayerObjectStorage> CreatedStorages() =>
            PlayerObjectStorages.FindInScene().Except(_existingBefore).ToList();
    }
}

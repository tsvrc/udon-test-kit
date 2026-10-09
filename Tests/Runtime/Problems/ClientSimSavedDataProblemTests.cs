using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UdonTestKit.Tests.Helpers;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim.Persistence;
using VRC.SDK3.Persistence;
using VRC.SDKBase;

namespace UdonTestKit.Tests.Problems
{
    // Problem: with player persistence on, ClientSim keeps each player's saved data on a hidden root
    // object that its own teardown never destroys. That object keeps rewriting the player's save
    // file, which the next session in the same scene uses too, so it overwrites what that session
    // saves. And ClientSim reports ready before it restores the local player's saved data, when
    // PlayerData changes aren't saved yet.
    // Solved by ClientSimSession.End and Start, tested in Solutions/ClientSimSessionSavedDataTests.
    // A failure here means ClientSim changed, so that handling may no longer be needed.
    public class ClientSimSavedDataProblemTests
    {
        // Long enough for a live storage to save again.
        private const float WriteWindowSeconds = 1f;
        // A pause before reading the file time, so a save started before End isn't counted after it.
        private const float SettleSeconds = 0.5f;
        private const string NewerSave = "{\"1\":[]}";
        private const string Key = "UdonTestKit.ProblemKey";

        private readonly RawClientSim _clientSim = new RawClientSim();
        private List<ClientSimPlayerObjectStorage> _existingBefore;

        [SetUp]
        public void SetUp() => _existingBefore = SavedData.FindStorages();

        // Cleans up with the kit only after each test has observed ClientSim without it.
        [TearDown]
        public void TearDown()
        {
            _clientSim.End();
            ClientSimSession.DestroySavedDataObjects();
            ClientSimSession.DeleteSavedData();
        }

        [UnityTest]
        public IEnumerator Start_PlayerPersistenceOn_StorageIsAHiddenRootObject()
        {
            // Act
            yield return _clientSim.Start();

            // Assert
            ClientSimPlayerObjectStorage storage = CreatedStorages().FirstOrDefault();
            Assume.That(storage != null, SavedData.PersistenceOffMessage);
            Assert.That(storage.transform.parent, Is.Null);
            Assert.That(storage.gameObject.hideFlags & HideFlags.HideInHierarchy, Is.EqualTo(HideFlags.HideInHierarchy));
        }

        [UnityTest]
        public IEnumerator End_PlayerPersistenceOn_LeavesTheStorageAlive()
        {
            // Arrange
            yield return _clientSim.Start();
            List<ClientSimPlayerObjectStorage> created = CreatedStorages();
            Assume.That(created, Is.Not.Empty, SavedData.PersistenceOffMessage);

            // Act
            _clientSim.End();
            // Lets any Object.Destroy ClientSim queued run, so a deferred cleanup isn't taken for a leak.
            yield return null;

            // Assert
            Assert.That(SavedData.CountAlive(created), Is.EqualTo(created.Count));
        }

        [UnityTest]
        public IEnumerator End_LeakedStorage_KeepsRewritingItsSaveFile()
        {
            // Arrange
            yield return _clientSim.Start();
            ClientSimPlayerObjectStorage storage = CreatedStorages().FirstOrDefault();
            Assume.That(storage != null, SavedData.PersistenceOffMessage);
            string saveFile = SavedData.PlayerObjectFileOf(storage);
            _clientSim.End();
            yield return new WaitForSecondsRealtime(SettleSeconds);
            DateTime lastWriteAfterEnd = File.GetLastWriteTimeUtc(saveFile);

            // Act
            yield return new WaitForSecondsRealtime(WriteWindowSeconds);

            // Assert
            Assert.That(File.GetLastWriteTimeUtc(saveFile), Is.GreaterThan(lastWriteAfterEnd));
        }

        [UnityTest]
        public IEnumerator End_BackToBackSessions_LeaksShareOneSaveFile()
        {
            // Arrange
            yield return _clientSim.Start();
            _clientSim.End();

            // Act
            yield return _clientSim.Start();
            _clientSim.End();

            // Assert
            List<ClientSimPlayerObjectStorage> leaked = CreatedStorages();
            Assume.That(leaked.Count, Is.EqualTo(2), SavedData.PersistenceOffMessage);
            Assert.That(SavedData.PlayerObjectFileOf(leaked[1]), Is.EqualTo(SavedData.PlayerObjectFileOf(leaked[0])));
        }

        [UnityTest]
        public IEnumerator End_LeakedStorage_OverwritesANewerSave()
        {
            // Arrange
            yield return _clientSim.Start();
            ClientSimPlayerObjectStorage storage = CreatedStorages().FirstOrDefault();
            Assume.That(storage != null, SavedData.PersistenceOffMessage);
            string saveFile = SavedData.PlayerObjectFileOf(storage);
            _clientSim.End();
            yield return new WaitForSecondsRealtime(SettleSeconds);

            // Act: a newer save lands in the file, as the next session's would.
            File.WriteAllText(saveFile, NewerSave);
            yield return new WaitForSecondsRealtime(WriteWindowSeconds);

            // Assert
            Assert.That(File.ReadAllText(saveFile), Is.Not.EqualTo(NewerSave));
        }

        [UnityTest]
        public IEnumerator SetString_RightAfterReady_IsNotSaved()
        {
            // Arrange
            yield return _clientSim.Start();
            Assume.That(CreatedStorages(), Is.Not.Empty, SavedData.PersistenceOffMessage);

            // Act
            PlayerData.SetString(Key, "value");
            yield return new WaitForSecondsRealtime(SettleSeconds);

            // Assert
            Assert.That(File.ReadAllText(SavedData.PlayerDataFileOf(Networking.LocalPlayer)), Does.Not.Contain(Key));
        }

        private List<ClientSimPlayerObjectStorage> CreatedStorages() =>
            SavedData.FindStorages().Except(_existingBefore).ToList();
    }
}

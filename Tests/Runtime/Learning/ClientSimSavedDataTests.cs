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

namespace UdonTestKit.Tests.Learning
{
    // The hidden object ClientSim saves a player's data with outlives ClientSim's teardown and
    // overwrites newer saves, and a change made before the local player is restored isn't saved.
    // ClientSimSession.End and Start handle both.
    public class ClientSimSavedDataTests
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

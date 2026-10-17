using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Tests.Helpers;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim.Persistence;
using VRC.SDK3.Persistence;
using VRC.SDKBase;

namespace Tsvrc.UdonTestKit.Tests.Learning
{
    // The hidden object ClientSim saves a player's data with outlives ClientSim's teardown and
    // overwrites newer saves, and a save file that isn't whole JSON makes the next session log an
    // error, which fails the test running then. A change made before the local player is restored
    // isn't saved. ClientSimSession.End and Start handle each, and the learning tests that start
    // ClientSim themselves destroy the storage after each test.
    public class ClientSimSavedDataTests
    {
        private readonly RawClientSim _clientSim = new RawClientSim();
        private List<ClientSimPlayerObjectStorage> _existingBefore;

        [SetUp]
        public void SetUp() => _existingBefore = ClientSimSession.SavedDataObjects().ToList();

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
            yield return new WaitForSecondsRealtime(SavedData.SettleSeconds);
            DateTime lastWriteAfterEnd = File.GetLastWriteTimeUtc(saveFile);

            // Act
            yield return new WaitForSecondsRealtime(SavedData.WriteWindowSeconds);

            // Assert
            Assert.That(File.GetLastWriteTimeUtc(saveFile), Is.GreaterThan(lastWriteAfterEnd));
        }

        // The storage of the first session is destroyed before the second starts: left alive, it can be
        // writing the file as the second session reads it.
        [UnityTest]
        public IEnumerator Start_SecondSessionInTheSameScene_SavesToTheFileOfTheFirst()
        {
            // Arrange
            yield return _clientSim.Start();
            ClientSimPlayerObjectStorage first = CreatedStorages().FirstOrDefault();
            Assume.That(first != null, SavedData.PersistenceOffMessage);
            string firstFile = SavedData.PlayerObjectFileOf(first);
            _clientSim.End();
            ClientSimSession.DestroySavedDataObjects();
            yield return new WaitForSecondsRealtime(SavedData.SettleSeconds);

            // Act
            yield return _clientSim.Start();

            // Assert
            Assert.That(SavedData.PlayerObjectFileOf(CreatedStorages().Single()), Is.EqualTo(firstFile));
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
            yield return new WaitForSecondsRealtime(SavedData.SettleSeconds);

            // Act: a newer save lands in the file, as the next session's would.
            File.WriteAllText(saveFile, SavedData.NewerSave);
            yield return new WaitForSecondsRealtime(SavedData.WriteWindowSeconds);

            // Assert
            Assert.That(File.ReadAllText(saveFile), Is.Not.EqualTo(SavedData.NewerSave));
        }

        // ClientSim writes a save file on a worker thread, so a leaked storage can leave one half
        // written as the next session reads it.
        [UnityTest]
        public IEnumerator Start_PlayerObjectsFileNotWholeJson_LogsThatItCantParseIt()
        {
            // Arrange: a first session gives the file the local player's PlayerObjects come from.
            yield return _clientSim.Start();
            ClientSimPlayerObjectStorage storage = CreatedStorages().FirstOrDefault();
            Assume.That(storage != null, SavedData.PersistenceOffMessage);
            string saveFile = SavedData.PlayerObjectFileOf(storage);
            _clientSim.End();
            ClientSimSession.DestroySavedDataObjects();
            yield return new WaitForSecondsRealtime(SavedData.SettleSeconds);
            File.WriteAllText(saveFile, "{\"unfinished");
            var errors = new List<string>();
            Application.LogCallback record = (message, stackTrace, type) =>
            {
                if (type == LogType.Error)
                {
                    errors.Add(message);
                }
            };

            // Act
            Application.logMessageReceived += record;
            try
            {
                yield return _clientSim.Start();
                for (int frame = 0; frame < 10 && errors.Count == 0; frame++)
                {
                    yield return null;
                }
            }
            finally
            {
                Application.logMessageReceived -= record;
            }

            // Assert
            Assert.That(errors, Has.Some.Contains("Error initializing PlayerObjects: UnableToParse"));
            LogAssert.Expect(LogType.Error, new Regex("Error initializing PlayerObjects: UnableToParse"));
        }

        [UnityTest]
        public IEnumerator SetString_RightAfterReady_IsNotSaved()
        {
            // Arrange
            yield return _clientSim.Start();
            Assume.That(CreatedStorages(), Is.Not.Empty, SavedData.PersistenceOffMessage);

            // Act
            PlayerData.SetString(SavedData.Key, "value");
            yield return new WaitForSecondsRealtime(SavedData.SettleSeconds);

            // Assert
            Assert.That(File.ReadAllText(SavedData.PlayerDataFileOf(Networking.LocalPlayer)), Does.Not.Contain(SavedData.Key));
        }

        private List<ClientSimPlayerObjectStorage> CreatedStorages() =>
            ClientSimSession.SavedDataObjects().Except(_existingBefore).ToList();
    }
}

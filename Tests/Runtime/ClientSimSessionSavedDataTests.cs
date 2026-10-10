using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Tsvrc.TestKit.Tests.Helpers;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim.Persistence;
using VRC.SDK3.Persistence;
using VRC.SDKBase;

namespace Tsvrc.TestKit.Tests
{
    public class ClientSimSessionSavedDataTests : ClientSimTestBase
    {
        private const string Value = "saved";

        private List<ClientSimPlayerObjectStorage> _existingBefore;

        [SetUp]
        public void SetUp() => _existingBefore = ClientSimSession.SavedDataObjects().ToList();

        [TearDown]
        public void TearDown()
        {
            Session.End();
            ClientSimSession.DeleteSavedData();
        }

        [UnityTest]
        public IEnumerator End_AfterASession_DestroysItsStorage()
        {
            // Arrange
            yield return Session.Start();
            List<ClientSimPlayerObjectStorage> created = CreatedStorages();
            Assume.That(created, Is.Not.Empty, SavedData.PersistenceOffMessage);

            // Act
            Session.End();

            // Assert
            Assert.That(SavedData.CountAlive(created), Is.Zero);
        }

        [UnityTest]
        public IEnumerator End_AfterASession_LeavesANewerSaveAlone()
        {
            // Arrange
            yield return Session.Start();
            ClientSimPlayerObjectStorage storage = CreatedStorages().FirstOrDefault();
            Assume.That(storage != null, SavedData.PersistenceOffMessage);
            string saveFile = SavedData.PlayerObjectFileOf(storage);
            Session.End();

            // Act
            File.WriteAllText(saveFile, SavedData.NewerSave);
            yield return new WaitForSecondsRealtime(SavedData.WriteWindowSeconds);

            // Assert
            Assert.That(File.ReadAllText(saveFile), Is.EqualTo(SavedData.NewerSave));
        }

        [UnityTest]
        public IEnumerator SetString_RightAfterStart_IsSaved()
        {
            // Arrange
            yield return Session.Start();
            Assume.That(CreatedStorages(), Is.Not.Empty, SavedData.PersistenceOffMessage);

            // Act
            PlayerData.SetString(SavedData.Key, Value);
            yield return null;

            // Assert
            Assert.That(File.ReadAllText(SavedData.PlayerDataFileOf(Networking.LocalPlayer)), Does.Contain(SavedData.Key));
        }

        [UnityTest]
        public IEnumerator Start_AfterAnEndedSessionInTheSameScene_RestoresWhatItSaved()
        {
            // Arrange
            yield return Session.Start();
            Assume.That(CreatedStorages(), Is.Not.Empty, SavedData.PersistenceOffMessage);
            PlayerData.SetString(SavedData.Key, Value);
            yield return null;
            Session.End();

            // Act
            yield return Session.Start();

            // Assert
            Assert.That(PlayerData.TryGetString(Networking.LocalPlayer, SavedData.Key, out string restored), Is.True);
            Assert.That(restored, Is.EqualTo(Value));
        }

        [UnityTest]
        public IEnumerator DeleteSavedData_BetweenSessions_NextSessionStartsWithNone()
        {
            // Arrange
            yield return Session.Start();
            Assume.That(CreatedStorages(), Is.Not.Empty, SavedData.PersistenceOffMessage);
            PlayerData.SetString(SavedData.Key, Value);
            yield return null;
            Session.End();

            // Act
            ClientSimSession.DeleteSavedData();
            yield return Session.Start();

            // Assert
            Assert.That(PlayerData.HasKey(Networking.LocalPlayer, SavedData.Key), Is.False);
        }

        private List<ClientSimPlayerObjectStorage> CreatedStorages() =>
            ClientSimSession.SavedDataObjects().Except(_existingBefore).ToList();
    }
}

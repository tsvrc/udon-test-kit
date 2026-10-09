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

namespace UdonTestKit.Tests.Solutions
{
    // Solution: ClientSimSession.End stops an ended session's saved data from being written, so the
    // next session in the same scene starts from what the last one saved and keeps what it saves
    // itself. Start returns once the local player's saved data is restored, so a test can use
    // PlayerData right away. DeleteSavedData gives the next session none. The problems are proven in
    // Problems/ClientSimSavedDataProblemTests.
    public class ClientSimSessionSavedDataTests : ClientSimTestBase
    {
        private const float WriteWindowSeconds = 1f;
        private const string NewerSave = "{\"1\":[]}";
        private const string Key = "UdonTestKit.SolutionKey";
        private const string Value = "saved";

        private List<ClientSimPlayerObjectStorage> _existingBefore;

        [SetUp]
        public void SetUp() => _existingBefore = SavedData.FindStorages();

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
            File.WriteAllText(saveFile, NewerSave);
            yield return new WaitForSecondsRealtime(WriteWindowSeconds);

            // Assert
            Assert.That(File.ReadAllText(saveFile), Is.EqualTo(NewerSave));
        }

        [UnityTest]
        public IEnumerator SetString_RightAfterStart_IsSaved()
        {
            // Arrange
            yield return Session.Start();
            Assume.That(CreatedStorages(), Is.Not.Empty, SavedData.PersistenceOffMessage);

            // Act
            PlayerData.SetString(Key, Value);
            yield return null;

            // Assert
            Assert.That(File.ReadAllText(SavedData.PlayerDataFileOf(Networking.LocalPlayer)), Does.Contain(Key));
        }

        [UnityTest]
        public IEnumerator Start_AfterAnEndedSessionInTheSameScene_RestoresWhatItSaved()
        {
            // Arrange
            yield return Session.Start();
            Assume.That(CreatedStorages(), Is.Not.Empty, SavedData.PersistenceOffMessage);
            PlayerData.SetString(Key, Value);
            yield return null;
            Session.End();

            // Act
            yield return Session.Start();

            // Assert
            Assert.That(PlayerData.TryGetString(Networking.LocalPlayer, Key, out string restored), Is.True);
            Assert.That(restored, Is.EqualTo(Value));
        }

        [UnityTest]
        public IEnumerator DeleteSavedData_BetweenSessions_NextSessionStartsWithNone()
        {
            // Arrange
            yield return Session.Start();
            Assume.That(CreatedStorages(), Is.Not.Empty, SavedData.PersistenceOffMessage);
            PlayerData.SetString(Key, Value);
            yield return null;
            Session.End();

            // Act
            ClientSimSession.DeleteSavedData();
            yield return Session.Start();

            // Assert
            Assert.That(PlayerData.HasKey(Networking.LocalPlayer, Key), Is.False);
        }

        private List<ClientSimPlayerObjectStorage> CreatedStorages() =>
            SavedData.FindStorages().Except(_existingBefore).ToList();
    }
}

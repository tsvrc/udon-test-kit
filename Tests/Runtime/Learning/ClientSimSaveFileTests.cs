using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine.TestTools;
using VRC.SDK3.Persistence;
using VRC.SDKBase;

namespace Tsvrc.UdonTestKit.Tests.Learning
{
    // ClientSim saves the PlayerData and PlayerObjects of each player to a file each in the
    // ClientSimStorage folder of the project, named after the player id and the active scene. So
    // ClientSimSession.DeleteSavedData and TestSceneSavedData find the files by those names.
    public class ClientSimSaveFileTests : ClientSimTestBase
    {
        private const string SceneName = "Tsvrc.UdonTestKit.SaveFiles";

        public ClientSimSaveFileTests() : base(TestScene.Empty(SceneName))
        {
        }

        // Runs after the base class has ended the session, while the scene is still active.
        [UnityTearDown]
        public IEnumerator DeleteTheSavedData()
        {
            ClientSimSession.DeleteSavedData();
            yield break;
        }

        [UnityTest]
        public IEnumerator SetString_AFrameLater_IsInTheFilesNamedAfterThePlayerAndTheScene()
        {
            // Arrange
            yield return Session.Start();
            int player = Networking.LocalPlayer.playerId;

            // Act
            PlayerData.SetString(SceneName, "saved");
            yield return null;

            // Assert
            string data = Path.Combine(ClientSimSession.StorageRoot, "PlayerData", $"PlayerData_{player}_{SceneName}.json");
            string objects = Path.Combine(ClientSimSession.StorageRoot, "PlayerObjects", $"PlayerObject_{player}_{SceneName}.json");
            Assert.That(File.ReadAllText(data), Does.Contain("saved"));
            Assert.That(File.Exists(objects), Is.True, $"No file at {objects}.");
        }
    }
}

using System.Collections;
using System.Linq;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Tests.Helpers;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim;
using VRC.SDKBase;

namespace Tsvrc.UdonTestKit.Tests.Learning
{
    // ClientSim gives a remote player the name it's spawned with without checking that no other
    // player has it, and names one spawned without a name after its id. So
    // ClientSimSession.SpawnRemotePlayer, which finds the player it spawned by name, refuses an
    // empty name and a name a player in the session has already.
    public class ClientSimPlayerNameTests
    {
        private readonly RawClientSim _clientSim = new RawClientSim();

        [TearDown]
        public void TearDown()
        {
            _clientSim.End();
            ClientSimSession.DestroySavedDataObjects();
        }

        [UnityTest]
        public IEnumerator SpawnRemotePlayer_NameInUse_SpawnsASecondPlayerWithIt()
        {
            // Arrange
            yield return _clientSim.Start();

            // Act
            ClientSimMain.SpawnRemotePlayer("Remote");
            ClientSimMain.SpawnRemotePlayer("Remote");

            // Assert
            Assert.That(Players().Count(player => player.displayName == "Remote"), Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator SpawnRemotePlayer_NoName_NamesThePlayerAfterItsId()
        {
            // Arrange
            yield return _clientSim.Start();

            // Act
            ClientSimMain.SpawnRemotePlayer("");

            // Assert
            VRCPlayerApi remote = Players().Single(player => !player.isLocal);
            Assert.That(remote.displayName, Is.EqualTo($"[{remote.playerId}] Remote Player"));
        }

        private static VRCPlayerApi[] Players()
        {
            var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(players);
            return players;
        }
    }
}

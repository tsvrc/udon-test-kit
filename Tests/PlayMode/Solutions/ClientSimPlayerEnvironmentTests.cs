using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UdonTestKit.ClientSim;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace UdonTestKit.Tests.PlayMode.Solutions
{
    // Solution: ClientSimPlayerEnvironment starts ClientSim with the scene descriptor it needs and
    // waits until ClientSim is ready, or throws TimeoutException if it never is. The problems are
    // proven in Problems/ClientSimStartupProblemTests. Extending ClientSimTestBase also runs every
    // registered fixup around these tests, as a world's tests do.
    public class ClientSimPlayerEnvironmentTests : ClientSimTestBase
    {
        private const string RemoteName = "Remote";

        private ClientSimPlayerEnvironment _slowPlayers;

        [TearDown]
        public void TearDown() => _slowPlayers?.Teardown();

        [UnityTest]
        public IEnumerator StartClientSim_WhenItReturns_LocalPlayerIsEnabled()
        {
            yield return StartClientSim();

            Assert.That(Networking.LocalPlayer.gameObject.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator Start_ClientSimNotReadyWithinTimeout_ThrowsTimeoutException()
        {
            // Arrange: ClientSim takes 5 s to get ready, the timeout is 0.2 s.
            _slowPlayers = new ClientSimPlayerEnvironment(readyTimeoutSeconds: 0.2f, initializationDelay: 5f);
            IEnumerator start = _slowPlayers.Start();

            // Act
            TimeoutException failure;
            while (MoveNext(start, out failure))
                yield return start.Current;

            // Assert
            Assert.That(failure, Is.Not.Null, "Start finished instead of timing out.");
        }

        [UnityTest]
        public IEnumerator Teardown_AfterStart_LeavesNoClientSimTestModeOrDescriptor()
        {
            // Arrange
            yield return StartClientSim();

            // Act
            Players.Teardown();

            // Assert
            Assert.That(ClientSimMain.HasInstance(), Is.False);
            Assert.That(ClientSimRuntimeLoader.IsInUnityTest(), Is.False);
            Assert.That(Resources.FindObjectsOfTypeAll<VRCSceneDescriptor>().Where(descriptor => descriptor.gameObject.scene.IsValid()), Is.Empty);
        }

        [UnityTest]
        public IEnumerator StartClientSim_LocalPlayerIsMasterTrue_LocalPlayerIsMaster()
        {
            yield return StartClientSim(localPlayerIsMaster: true);

            Assert.That(Networking.IsMaster, Is.True);
        }

        [UnityTest]
        public IEnumerator StartClientSim_LocalPlayerIsMasterFalse_LocalPlayerIsNotMaster()
        {
            yield return StartClientSim(localPlayerIsMaster: false);

            Assert.That(Networking.IsMaster, Is.False);
        }

        [UnityTest]
        public IEnumerator StartClientSim_Default_LocalPlayerIsMaster()
        {
            yield return StartClientSim();

            Assert.That(Networking.IsMaster, Is.True);
        }

        [UnityTest]
        public IEnumerator SpawnRemotePlayer_SameFrame_IsFoundByName()
        {
            // Arrange
            yield return StartClientSim();

            // Act
            Players.SpawnRemotePlayer(RemoteName);

            // Assert
            VRCPlayerApi found = ClientSimPlayerEnvironment.FindPlayerByName(RemoteName);
            Assert.That(found, Is.Not.Null);
            Assert.That(found.isLocal, Is.False);
        }

        [UnityTest]
        public IEnumerator SpawnRemotePlayer_TwoPlayers_GetDistinctIds()
        {
            // Arrange
            yield return StartClientSim();

            // Act
            Players.SpawnRemotePlayer("Alpha");
            Players.SpawnRemotePlayer("Beta");

            // Assert
            VRCPlayerApi alpha = ClientSimPlayerEnvironment.FindPlayerByName("Alpha");
            VRCPlayerApi beta = ClientSimPlayerEnvironment.FindPlayerByName("Beta");
            Assert.That(alpha, Is.Not.Null);
            Assert.That(beta, Is.Not.Null);
            Assert.That(alpha.playerId, Is.Not.EqualTo(beta.playerId));
        }

        [UnityTest]
        public IEnumerator RemovePlayer_SameFrame_IsNoLongerFoundByName()
        {
            // Arrange
            yield return StartClientSim();
            Players.SpawnRemotePlayer(RemoteName);
            VRCPlayerApi remote = ClientSimPlayerEnvironment.FindPlayerByName(RemoteName);
            Assume.That(remote, Is.Not.Null, "The remote player never spawned.");

            // Act
            Players.RemovePlayer(remote);

            // Assert
            Assert.That(ClientSimPlayerEnvironment.FindPlayerByName(RemoteName), Is.Null);
        }

        [UnityTest]
        public IEnumerator FindPlayerByName_UnknownName_ReturnsNull()
        {
            yield return StartClientSim();

            Assert.That(ClientSimPlayerEnvironment.FindPlayerByName("NoSuchPlayer"), Is.Null);
        }

        // Steps a coroutine, capturing a TimeoutException instead of letting it fail the test.
        private static bool MoveNext(IEnumerator routine, out TimeoutException failure)
        {
            failure = null;
            try
            {
                return routine.MoveNext();
            }
            catch (TimeoutException e)
            {
                failure = e;
                return false;
            }
        }
    }
}

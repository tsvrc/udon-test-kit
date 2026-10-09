using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDKBase;

namespace UdonTestKit.Tests.Solutions
{
    // Solution: FakeOwnershipSyncable lets a test make any player the owner of a GameObject, so
    // the local player can be master and still not own it. The problem is proven in
    // Problems/ClientSimOwnershipProblemTests.
    public class FakeOwnershipSyncableTests : ClientSimTestBase
    {
        private const string RemoteName = "Remote";

        private GameObject _target;

        [TearDown]
        public void TearDown()
        {
            if (_target != null)
                Object.DestroyImmediate(_target);
        }

        [UnityTest]
        public IEnumerator SetOwner_RemotePlayerId_LocalMasterIsNotOwner()
        {
            // Arrange
            yield return StartClientSim(localPlayerIsMaster: true);
            VRCPlayerApi remote = SpawnRemote();
            FakeOwnershipSyncable syncable = CreateTarget();

            // Act
            syncable.SetOwner(remote.playerId);

            // Assert
            Assert.That(Networking.IsOwner(Networking.LocalPlayer, _target), Is.False);
            Assert.That(Networking.GetOwner(_target), Is.EqualTo(remote));
        }

        [UnityTest]
        public IEnumerator NetworkingSetOwner_RemotePlayer_RemoteBecomesOwner()
        {
            // Arrange
            yield return StartClientSim(localPlayerIsMaster: true);
            VRCPlayerApi remote = SpawnRemote();
            CreateTarget().SetOwner(Networking.LocalPlayer.playerId);

            // Act
            Networking.SetOwner(remote, _target);

            // Assert
            Assert.That(Networking.IsOwner(remote, _target), Is.True);
            Assert.That(Networking.IsOwner(Networking.LocalPlayer, _target), Is.False);
        }

        [UnityTest]
        public IEnumerator GetOwner_SetOwnerNeverCalled_IsNobody()
        {
            // Arrange
            yield return StartClientSim(localPlayerIsMaster: true);
            CreateTarget();

            // Act
            VRCPlayerApi owner = Networking.GetOwner(_target);

            // Assert
            Assert.That(owner, Is.Null);
        }

        private VRCPlayerApi SpawnRemote()
        {
            Players.SpawnRemotePlayer(RemoteName);
            return ClientSimPlayerEnvironment.FindPlayerByName(RemoteName);
        }

        private FakeOwnershipSyncable CreateTarget()
        {
            _target = new GameObject(nameof(FakeOwnershipSyncableTests));
            return _target.AddComponent<FakeOwnershipSyncable>();
        }
    }
}

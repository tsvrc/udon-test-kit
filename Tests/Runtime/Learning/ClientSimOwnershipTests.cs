using System.Collections;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Tests.Helpers;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim;
using VRC.SDKBase;

namespace Tsvrc.UdonTestKit.Tests.Learning
{
    // In ClientSim the master owns every GameObject without VRCObjectSync or VRCObjectPool, including
    // one holding an UdonSharpBehaviour, and Networking.SetOwner can't change that. In VRChat every
    // networked GameObject's owner can change. ClientSimSession.SetOwner gives a GameObject an owner
    // of its own.
    public class ClientSimOwnershipTests
    {
        private const string RemoteName = "Remote";

        private readonly RawClientSim _clientSim = new RawClientSim();
        private GameObject _target;

        [TearDown]
        public void TearDown()
        {
            if (_target != null)
            {
                Object.DestroyImmediate(_target);
            }
            _clientSim.End();
            ClientSimSession.DestroySavedDataObjects();
        }

        [UnityTest]
        public IEnumerator GetOwner_GameObjectWithAnUdonSharpBehaviour_IsTheMaster()
        {
            // Arrange
            yield return _clientSim.Start();
            _target = CreateTarget();

            // Act
            VRCPlayerApi owner = Networking.GetOwner(_target);

            // Assert
            Assert.That(owner, Is.EqualTo(Networking.LocalPlayer));
            Assert.That(Networking.LocalPlayer.isMaster, Is.True);
        }

        [UnityTest]
        public IEnumerator SetOwner_GameObjectWithAnUdonSharpBehaviourToRemotePlayer_MasterStaysOwner()
        {
            // Arrange
            yield return _clientSim.Start();
            ClientSimMain.SpawnRemotePlayer(RemoteName);
            VRCPlayerApi remote = ClientSimSession.FindPlayerByName(RemoteName);
            _target = CreateTarget();

            // Act
            Networking.SetOwner(remote, _target);

            // Assert
            Assert.That(Networking.IsOwner(Networking.LocalPlayer, _target), Is.True);
            Assert.That(Networking.IsOwner(remote, _target), Is.False);
        }

        // ClientSim hands a leaving player's GameObjects to the master only for owners registered with
        // its ClientSimSyncedObjectManager, as the owners of VRCObjectSync and VRCObjectPool are.
        // ClientSimSession.SetOwner registers the owner it adds for this reason.
        [UnityTest]
        public IEnumerator RemovePlayer_OwnerOfAnUnregisteredSyncable_StaysItsOwner()
        {
            // Arrange
            yield return _clientSim.Start();
            ClientSimMain.SpawnRemotePlayer(RemoteName);
            VRCPlayerApi remote = ClientSimSession.FindPlayerByName(RemoteName);
            _target = new GameObject(nameof(ClientSimOwnershipTests));
            var owner = _target.AddComponent<UnregisteredSyncable>();
            owner.SetOwner(remote.playerId);

            // Act
            ClientSimMain.RemovePlayer(remote);

            // Assert
            Assert.That(owner.GetOwner(), Is.EqualTo(remote.playerId));
        }

        // ClientSim marks its synced object manager DontSave, and FindObjectOfType skips DontSave
        // objects, so ClientSimSession.SetOwner finds the manager with FindObjectsOfTypeAll.
        [UnityTest]
        public IEnumerator FindObjectOfType_ClientSimSyncedObjectManagerWhileRunning_FindsNothing()
        {
            // Arrange
            yield return _clientSim.Start();

            // Act
            var manager = Object.FindObjectOfType<ClientSimSyncedObjectManager>(true);

            // Assert
            Assert.That(manager == null, "FindObjectOfType found ClientSim's synced object manager.");
        }

        private static GameObject CreateTarget()
        {
            var target = new GameObject(nameof(ClientSimOwnershipTests));
            target.AddComponent<TargetBehaviour>();
            return target;
        }
    }
}

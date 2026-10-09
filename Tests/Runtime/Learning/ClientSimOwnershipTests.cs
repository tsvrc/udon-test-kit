using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDKBase;

namespace UdonTestKit.Tests.Learning
{
    // In ClientSim the master owns every GameObject without an IClientSimSyncable, and
    // Networking.SetOwner can't change that. FakeOwnershipSyncable gives a GameObject its own owner.
    public class ClientSimOwnershipTests
    {
        private const string RemoteName = "Remote";

        private readonly ClientSimSession _session = new ClientSimSession();
        private GameObject _target;

        [TearDown]
        public void TearDown()
        {
            if (_target != null)
                Object.DestroyImmediate(_target);
            _session.End();
        }

        [UnityTest]
        public IEnumerator GetOwner_PlainGameObject_IsTheMaster()
        {
            // Arrange
            yield return _session.Start(localPlayerIsMaster: true);
            _target = new GameObject(nameof(ClientSimOwnershipTests));

            // Act
            VRCPlayerApi owner = Networking.GetOwner(_target);

            // Assert
            Assert.That(owner, Is.EqualTo(Networking.LocalPlayer));
            Assert.That(Networking.LocalPlayer.isMaster, Is.True);
        }

        [UnityTest]
        public IEnumerator SetOwner_PlainGameObjectToRemotePlayer_MasterStaysOwner()
        {
            // Arrange
            yield return _session.Start(localPlayerIsMaster: true);
            VRCPlayerApi remote = _session.SpawnRemotePlayer(RemoteName);
            _target = new GameObject(nameof(ClientSimOwnershipTests));

            // Act
            Networking.SetOwner(remote, _target);

            // Assert
            Assert.That(Networking.IsOwner(Networking.LocalPlayer, _target), Is.True);
            Assert.That(Networking.IsOwner(remote, _target), Is.False);
        }
    }
}

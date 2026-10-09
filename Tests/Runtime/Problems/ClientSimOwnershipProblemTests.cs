using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDKBase;

namespace UdonTestKit.Tests.Problems
{
    // Problem: in ClientSim, a GameObject without an IClientSimSyncable is always owned by the
    // master, and Networking.SetOwner can't change that. So while the local player is master, a
    // test can't make it "not the owner" of such a GameObject.
    // Solved by FakeOwnershipSyncable, tested in Solutions/FakeOwnershipSyncableTests.
    // A failure here means ClientSim changed, so the double may no longer be needed.
    public class ClientSimOwnershipProblemTests
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
            _target = new GameObject(nameof(ClientSimOwnershipProblemTests));

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
            _target = new GameObject(nameof(ClientSimOwnershipProblemTests));

            // Act
            Networking.SetOwner(remote, _target);

            // Assert
            Assert.That(Networking.IsOwner(Networking.LocalPlayer, _target), Is.True);
            Assert.That(Networking.IsOwner(remote, _target), Is.False);
        }
    }
}

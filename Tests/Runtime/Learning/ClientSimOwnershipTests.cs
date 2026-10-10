using System.Collections;
using NUnit.Framework;
using UdonTestKit.Tests.Helpers;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim;
using VRC.SDKBase;

namespace UdonTestKit.Tests.Learning
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

        private static GameObject CreateTarget()
        {
            var target = new GameObject(nameof(ClientSimOwnershipTests));
            target.AddComponent<TargetBehaviour>();
            return target;
        }
    }
}

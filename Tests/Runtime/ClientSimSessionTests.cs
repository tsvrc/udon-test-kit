using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace Tsvrc.TestKit.Tests
{
    public class ClientSimSessionTests : ClientSimTestBase
    {
        private const string RemoteName = "Remote";

        private ClientSimSession _slowSession;
        private GameObject _sceneDescriptor;
        private GameObject _owned;
        private Action _restore;

        [TearDown]
        public void TearDown()
        {
            _slowSession?.End();
            if (_sceneDescriptor != null)
            {
                UnityEngine.Object.DestroyImmediate(_sceneDescriptor);
            }
            if (_owned != null)
            {
                UnityEngine.Object.DestroyImmediate(_owned);
            }
            _restore?.Invoke();
            _restore = null;
        }

#if UNITY_EDITOR
        [Test]
        public void Start_ClientSimSettingsInvalid_FailsNamingThemBeforeClientSimStarts()
        {
            // Arrange: breaks the audio spatializer setting in memory only.
            var audioManager = new UnityEditor.SerializedObject(
                UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("ProjectSettings/AudioManager.asset"));
            UnityEditor.SerializedProperty spatializer = audioManager.FindProperty("m_SpatializerPlugin");
            string original = spatializer.stringValue;
            Assume.That(original, Is.Not.Empty, "This project has no audio spatializer to break.");
            _restore = () => SetSpatializer(audioManager, original);
            SetSpatializer(audioManager, "");

            // Act
            var failure = Assert.Throws<AssertionException>(() => Session.Start().MoveNext());

            // Assert
            Assert.That(failure.Message, Does.Contain("audio spatializer"));
            Assert.That(ClientSimMain.HasInstance(), Is.False);
        }

        private static void SetSpatializer(UnityEditor.SerializedObject audioManager, string plugin)
        {
            audioManager.Update();
            audioManager.FindProperty("m_SpatializerPlugin").stringValue = plugin;
            audioManager.ApplyModifiedProperties();
        }
#endif

        [UnityTest]
        public IEnumerator Start_WhenItReturns_LocalPlayerIsEnabled()
        {
            yield return Session.Start();

            Assert.That(Networking.LocalPlayer.gameObject.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator Start_ClientSimNotReadyWithinTimeout_ThrowsTimeoutException()
        {
            // Arrange: ClientSim takes 5 s to get ready, the timeout is 0.2 s.
            _slowSession = new ClientSimSession(readyTimeoutSeconds: 0.2f, initializationDelay: 5f);
            IEnumerator start = _slowSession.Start();

            // Act
            TimeoutException failure;
            while (MoveNext(start, out failure))
            {
                yield return start.Current;
            }

            // Assert
            Assert.That(failure, Is.Not.Null, "Start finished instead of timing out.");
        }

        [UnityTest]
        public IEnumerator Start_Default_LocalPlayerIsMaster()
        {
            yield return Session.Start();

            Assert.That(Networking.IsMaster, Is.True);
        }

        [UnityTest]
        public IEnumerator Start_LocalPlayerIsMasterFalse_LocalPlayerIsNotMaster()
        {
            yield return Session.Start(localPlayerIsMaster: false);

            Assert.That(Networking.IsMaster, Is.False);
        }

        [UnityTest]
        public IEnumerator Start_SceneHasADescriptor_SpawnsAtItsSpawnPoint()
        {
            // Arrange
            var spawnPosition = new Vector3(5f, 0f, 7f);
            _sceneDescriptor = ClientSimSession.CreateSceneDescriptor(spawnPosition);

            // Act
            yield return Session.Start();

            // Assert
            Assert.That(Vector3.Distance(Networking.LocalPlayer.GetPosition(), spawnPosition), Is.LessThan(0.5f));
            Assert.That(SceneDescriptors().Count(), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator End_SceneHasADescriptor_LeavesIt()
        {
            // Arrange
            _sceneDescriptor = ClientSimSession.CreateSceneDescriptor(Vector3.zero);
            yield return Session.Start();

            // Act
            Session.End();

            // Assert
            Assert.That(_sceneDescriptor != null, "End destroyed the scene's own descriptor.");
        }

        [UnityTest]
        public IEnumerator End_AfterStart_LeavesNoClientSimTestModeOrDescriptor()
        {
            // Arrange
            yield return Session.Start();

            // Act
            Session.End();

            // Assert
            Assert.That(ClientSimMain.HasInstance(), Is.False);
            Assert.That(ClientSimRuntimeLoader.IsInUnityTest(), Is.False);
            Assert.That(SceneDescriptors(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator SpawnRemotePlayer_SameFrame_ReturnsTheRemotePlayer()
        {
            // Arrange
            yield return Session.Start();

            // Act
            VRCPlayerApi remote = Session.SpawnRemotePlayer(RemoteName);

            // Assert
            Assert.That(remote.displayName, Is.EqualTo(RemoteName));
            Assert.That(remote.isLocal, Is.False);
        }

        [UnityTest]
        public IEnumerator SpawnRemotePlayer_TwoPlayers_GetDistinctIds()
        {
            // Arrange
            yield return Session.Start();

            // Act
            VRCPlayerApi alpha = Session.SpawnRemotePlayer("Alpha");
            VRCPlayerApi beta = Session.SpawnRemotePlayer("Beta");

            // Assert
            Assert.That(alpha.playerId, Is.Not.EqualTo(beta.playerId));
        }

        [UnityTest]
        public IEnumerator RemovePlayer_SameFrame_IsNoLongerFoundByName()
        {
            // Arrange
            yield return Session.Start();
            VRCPlayerApi remote = Session.SpawnRemotePlayer(RemoteName);

            // Act
            Session.RemovePlayer(remote);

            // Assert
            Assert.That(ClientSimSession.FindPlayerByName(RemoteName), Is.Null);
        }

        [UnityTest]
        public IEnumerator FindPlayerByName_UnknownName_ReturnsNull()
        {
            yield return Session.Start();

            Assert.That(ClientSimSession.FindPlayerByName("NoSuchPlayer"), Is.Null);
        }

        [UnityTest]
        public IEnumerator SetOwner_RemotePlayer_IsTheOwnerInsteadOfTheLocalMaster()
        {
            // Arrange
            yield return Session.Start();
            VRCPlayerApi remote = Session.SpawnRemotePlayer(RemoteName);
            _owned = new GameObject(nameof(ClientSimSessionTests));

            // Act
            Session.SetOwner(remote, _owned);

            // Assert
            Assert.That(Networking.GetOwner(_owned), Is.EqualTo(remote));
            Assert.That(Networking.IsOwner(Networking.LocalPlayer, _owned), Is.False);
        }

        [UnityTest]
        public IEnumerator NetworkingSetOwner_AfterSessionSetOwner_MovesTheOwnership()
        {
            // Arrange
            yield return Session.Start();
            VRCPlayerApi remote = Session.SpawnRemotePlayer(RemoteName);
            _owned = new GameObject(nameof(ClientSimSessionTests));
            Session.SetOwner(remote, _owned);

            // Act
            Networking.SetOwner(Networking.LocalPlayer, _owned);

            // Assert
            Assert.That(Networking.IsOwner(Networking.LocalPlayer, _owned), Is.True);
        }

        [UnityTest]
        public IEnumerator SetOwner_CalledTwice_AddsOneOwnershipComponent()
        {
            // Arrange
            yield return Session.Start();
            VRCPlayerApi remote = Session.SpawnRemotePlayer(RemoteName);
            _owned = new GameObject(nameof(ClientSimSessionTests));

            // Act
            Session.SetOwner(remote, _owned);
            Session.SetOwner(Networking.LocalPlayer, _owned);

            // Assert
            Assert.That(_owned.GetComponents<IClientSimSyncable>(), Has.Length.EqualTo(1));
        }

        private static IEnumerable<VRCSceneDescriptor> SceneDescriptors() =>
            Resources.FindObjectsOfTypeAll<VRCSceneDescriptor>().Where(descriptor => descriptor.gameObject.scene.IsValid());

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

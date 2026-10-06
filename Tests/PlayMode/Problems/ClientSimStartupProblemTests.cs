using NUnit.Framework;
using UdonTestKit.ClientSim;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace UdonTestKit.Tests.PlayMode.Problems
{
    // Problem: ClientSim refuses to start without a VRCSceneDescriptor in the scene, and when it
    // does start, the local player isn't enabled until ClientSim finishes initializing.
    // Solved by ClientSimPlayerEnvironment.Start, tested in Solutions/ClientSimPlayerEnvironmentTests.
    // A failure here means ClientSim changed, so that setup may no longer be needed.
    //
    // ClientSim is started directly, bypassing ClientSimPlayerEnvironment, so nothing hides the problem.
    public class ClientSimStartupProblemTests
    {
        private GameObject _descriptor;

        [TearDown]
        public void TearDown()
        {
            if (ClientSimMain.HasInstance())
                ClientSimMain.RemoveInstance();
            ClientSimRuntimeLoader.EndUnityTesting();

            if (_descriptor != null)
                Object.DestroyImmediate(_descriptor);
            new ClientSimPersistenceLeakFixup().OnUnityTearDown();
        }

        [Test]
        public void StartClientSim_NoSceneDescriptor_RefusesToStart()
        {
            // Arrange
            LogAssert.Expect(LogType.Warning, "Cannot start ClientSim if there is no scene descriptor!");

            // Act
            StartClientSim();

            // Assert
            Assert.That(ClientSimMain.HasInstance(), Is.False);
        }

        [Test]
        public void StartClientSim_SameFrame_LocalPlayerIsNotEnabledYet()
        {
            // Arrange
            CreateDescriptor();

            // Act
            StartClientSim();

            // Assert
            Assert.That(Networking.LocalPlayer, Is.Not.Null, "ClientSim should create the local player right away.");
            Assert.That(Networking.LocalPlayer.gameObject.activeSelf, Is.False);
        }

        private static void StartClientSim()
        {
            var settings = new ClientSimSettings
            {
                enableClientSim = true,
                spawnPlayer = true,
                localPlayerIsMaster = true,
                initializationDelay = 0f,
            };

            ClientSimRuntimeLoader.BeginUnityTesting(settings);
            ClientSimRuntimeLoader.StartClientSim(settings);
        }

        private void CreateDescriptor()
        {
            _descriptor = new GameObject("__ProblemTestSceneDescriptor");
            VRCSceneDescriptor descriptor = _descriptor.AddComponent<VRCSceneDescriptor>();

            var spawn = new GameObject("__ProblemTestSpawn");
            spawn.transform.SetParent(_descriptor.transform);
            descriptor.spawns = new[] { spawn.transform };
        }
    }
}

using NUnit.Framework;
using UdonTestKit.Tests.Helpers;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim;
using VRC.SDKBase;

namespace UdonTestKit.Tests.Problems
{
    // Problem: ClientSim refuses to start without a VRCSceneDescriptor in the scene, and when it
    // does start, the local player isn't enabled until ClientSim finishes initializing.
    // Solved by ClientSimSession.Start, tested in Solutions/ClientSimSessionTests.
    // A failure here means ClientSim changed, so that setup may no longer be needed.
    public class ClientSimStartupProblemTests
    {
        private readonly RawClientSim _clientSim = new RawClientSim();

        [TearDown]
        public void TearDown()
        {
            _clientSim.End();
            ClientSimSession.DestroySavedDataObjects();
        }

        [Test]
        public void Begin_NoSceneDescriptor_RefusesToStart()
        {
            // Arrange
            LogAssert.Expect(LogType.Warning, "Cannot start ClientSim if there is no scene descriptor!");

            // Act
            _clientSim.Begin(withDescriptor: false);

            // Assert
            Assert.That(ClientSimMain.HasInstance(), Is.False);
        }

        [Test]
        public void Begin_SameFrame_LocalPlayerIsNotEnabledYet()
        {
            // Act
            _clientSim.Begin();

            // Assert
            Assert.That(Networking.LocalPlayer, Is.Not.Null, "ClientSim should create the local player right away.");
            Assert.That(Networking.LocalPlayer.gameObject.activeSelf, Is.False);
        }
    }
}

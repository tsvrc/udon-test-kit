using NUnit.Framework;
using Tsvrc.TestKit.Tests.Helpers;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim;
using VRC.SDKBase;

namespace Tsvrc.TestKit.Tests.Learning
{
    // ClientSim won't start without a scene descriptor, and its local player isn't enabled in the
    // frame it starts. ClientSimSession.Start adds a descriptor and waits.
    public class ClientSimStartupTests
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

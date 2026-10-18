using System.Collections;
using NUnit.Framework;
using Tsvrc.UdonTestKit;
using UnityEngine.TestTools;
using VRC.SDKBase;
using VRC.Udon;

namespace Tsvrc.UdonTestKit.Tests
{
    public class ExamplePlayModeTests : UdonTestBase
    {
        [UnityTest]
        public IEnumerator SpawnRemotePlayer_InClientSim_JoinsThePlayerList()
        {
            yield return Session.Start();

            VRCPlayerApi remote = Session.SpawnRemotePlayer("Remote");

            Assert.That(VRCPlayerApi.GetPlayerCount(), Is.EqualTo(2));
            Assert.That(remote.isLocal, Is.False);
        }

        [Test]
        public void SendCustomEvent_OnExample_IsLoggedFromUdon()
        {
            UdonBehaviour log = Udon.Create<UdonCallLog>("Log");
            UdonBehaviour example = Udon.Create<ExampleUdonDouble>("Example");
            example.SetProgramVariable(nameof(ExampleUdonDouble.Log), log);

            example.SendCustomEvent(nameof(ExampleUdonDouble.OnExample));

            Assert.That(UdonHost.Calls(log), Is.EqualTo(new[] { "Example.OnExample" }));
        }
    }
}

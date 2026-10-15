using System.Collections;
using NUnit.Framework;
using Tsvrc.UdonTestKit;
using UnityEngine.TestTools;
using VRC.SDKBase;

public class ExamplePlayModeTests : ClientSimTestBase
{
    [UnityTest]
    public IEnumerator SpawnRemotePlayer_InClientSim_JoinsThePlayerList()
    {
        yield return Session.Start();

        VRCPlayerApi remote = Session.SpawnRemotePlayer("Remote");

        Assert.That(VRCPlayerApi.GetPlayerCount(), Is.EqualTo(2));
        Assert.That(remote.isLocal, Is.False);
    }
}

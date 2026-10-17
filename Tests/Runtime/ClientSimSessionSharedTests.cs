using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim;
using VRC.SDKBase;

namespace Tsvrc.UdonTestKit.Tests
{
    // One session for every test in the fixture. The Unity Test Framework the kit depends on has no
    // yielding one-time setup, so the first [UnitySetUp] starts it and [OneTimeTearDown] ends it.
    public class ClientSimSessionSharedTests
    {
        private readonly ClientSimSession _session = new ClientSimSession();
        private int _starts;
        private int _localPlayerId;

        [UnitySetUp]
        public IEnumerator StartOnce()
        {
            if (_starts > 0)
            {
                yield break;
            }
            _starts++;
            yield return _session.Start();
            _localPlayerId = Networking.LocalPlayer.playerId;
        }

        [OneTimeTearDown]
        public void EndSession() => _session.End();

        [UnityTest]
        public IEnumerator SharedSession_EachTest_SeesTheSessionStartedOnce([Values(1, 2)] int test)
        {
            yield return null;

            Assert.That(_starts, Is.EqualTo(1));
            Assert.That(ClientSimMain.HasInstance(), Is.True);
            Assert.That(Networking.LocalPlayer.playerId, Is.EqualTo(_localPlayerId));
        }
    }
}

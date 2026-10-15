using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim;

namespace Tsvrc.UdonTestKit.Tests
{
    public class ClientSimTestBaseTests : ClientSimTestBase
    {
        private bool _checkSessionInTearDown;
        private bool _checkSessionInUnityTearDown;

        [TearDown]
        public void TearDown()
        {
            bool check = _checkSessionInTearDown;
            _checkSessionInTearDown = false;
            if (check)
            {
                Assert.That(ClientSimMain.HasInstance(), Is.True, "The base class ended the session before this teardown.");
            }
        }

        // Runs after every [TearDown], so it sees what the base class left after the test.
        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            bool check = _checkSessionInUnityTearDown;
            _checkSessionInUnityTearDown = false;
            if (check)
            {
                Assert.That(ClientSimMain.HasInstance(), Is.False, "The base class didn't end the session after the test.");
            }
            yield break;
        }

        [UnityTest]
        public IEnumerator DerivedTearDown_AfterStart_SessionIsStillRunning()
        {
            yield return Session.Start();

            _checkSessionInTearDown = true;
        }

        [UnityTest]
        public IEnumerator EndClientSimSession_AfterATest_EndsTheSession()
        {
            yield return Session.Start();

            _checkSessionInUnityTearDown = true;
        }
    }
}

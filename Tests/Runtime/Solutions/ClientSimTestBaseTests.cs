using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim;

namespace UdonTestKit.Tests.Solutions
{
    // Solution: ClientSimTestBase ends its session after every test, and only after the derived
    // class's own teardown, which can still use the session.
    public class ClientSimTestBaseTests : ClientSimTestBase
    {
        private bool _checkSessionInTearDown;

        [TearDown]
        public void TearDown()
        {
            bool check = _checkSessionInTearDown;
            _checkSessionInTearDown = false;
            if (check)
                Assert.That(ClientSimMain.HasInstance(), Is.True, "The base class ended the session before this teardown.");
        }

        [UnityTest]
        public IEnumerator DerivedTearDown_AfterStart_SessionIsStillRunning()
        {
            yield return Session.Start();

            _checkSessionInTearDown = true;
        }

        [UnityTest]
        public IEnumerator EndClientSimSession_AfterStart_EndsTheSession()
        {
            // Arrange
            yield return Session.Start();

            // Act
            EndClientSimSession();

            // Assert
            Assert.That(ClientSimMain.HasInstance(), Is.False);
        }
    }
}

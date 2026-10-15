using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tsvrc.UdonTestKit.Tests
{
    public class UdonTestBaseTests : UdonTestBase
    {
        private GameObject _created;

        // Runs after every [TearDown], so it sees what the base class left after the test.
        [UnityTearDown]
        public IEnumerator CheckWhatTheTestCreatedIsGone()
        {
            GameObject created = _created;
            _created = null;
            Assert.That(created == null, Is.True, "UdonTestBase didn't destroy what the test created.");
            yield break;
        }

        [Test]
        public void DisposeUdonHost_AfterATest_DestroysWhatTheTestCreated()
        {
            // Act
            _created = Udon.Create<UdonProbe>("Probe").gameObject;

            // Assert: in CheckWhatTheTestCreatedIsGone.
        }
    }
}

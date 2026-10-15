using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tsvrc.UdonTestKit.Tests.Learning
{
    // Unity Test Framework runs [UnityTearDown] after every [TearDown]. So ClientSimTestBase unloads
    // a test scene once the session has ended, and the tests of the base classes check from a
    // [UnityTearDown] what a base class [TearDown] did.
    public class UnityTestFrameworkTearDownTests
    {
        private bool _tearDownRan;

        [TearDown]
        public void TearDown() => _tearDownRan = true;

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            Assert.That(_tearDownRan, Is.True, "[UnityTearDown] ran before [TearDown].");
            yield break;
        }

        [Test]
        public void UnityTearDown_AfterATest_RunsAfterTearDown()
        {
            // Act and assert: in the teardowns above.
        }
    }
}

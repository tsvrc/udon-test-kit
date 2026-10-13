using NUnit.Framework;

namespace Tsvrc.UdonTestKit
{
    // The base of a test class that runs behaviours as Udon: each test gets its own UdonHost, and
    // what it created is destroyed after the test. A test that needs ClientSim starts Session.
    public abstract class UdonTestBase : ClientSimTestBase
    {
        protected UdonHost Udon { get; private set; }

        [SetUp]
        public void CreateUdonHost()
        {
            Udon = new UdonHost();
        }

        [TearDown]
        public void DisposeUdonHost()
        {
            Udon.Dispose();
        }
    }
}

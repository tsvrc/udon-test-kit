using NUnit.Framework;

namespace Tsvrc.UdonTestKit
{
    /// <summary>
    /// Base for tests that run behaviours as Udon: each test gets its own <see cref="Udon"/>, and what
    /// it created is destroyed after the test. A test that needs ClientSim starts
    /// <see cref="ClientSimTestBase.Session"/>.
    /// </summary>
    public abstract class UdonTestBase : ClientSimTestBase
    {
        /// <summary>Creates the behaviours of the current test.</summary>
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

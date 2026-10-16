using NUnit.Framework;

namespace Tsvrc.UdonTestKit
{
    /// <summary>
    /// Base for tests that run behaviours as Udon: each test gets its own <see cref="Udon"/>, and what
    /// it created is destroyed after the test. A test that needs ClientSim starts
    /// <see cref="ClientSimTestBase.Session"/>. Tests run in the Unity Test Framework test scene unless
    /// the fixture passes its own <see cref="TestScene"/>.
    /// </summary>
    public abstract class UdonTestBase : ClientSimTestBase
    {
        /// <summary>Creates the behaviours of the current test.</summary>
        protected UdonHost Udon { get; private set; }

        protected UdonTestBase()
        {
        }

        /// <summary>Runs every test in <paramref name="scene"/>, loaded before the derived class's
        /// setup and unloaded once ClientSim has ended.</summary>
        protected UdonTestBase(TestScene scene, TestSceneLifetime lifetime = TestSceneLifetime.PerTest) : base(scene, lifetime)
        {
        }

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

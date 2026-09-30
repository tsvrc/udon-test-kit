using System.Linq;
using NUnit.Framework;
using UdonTestKit.ClientSim;

namespace UdonTestKit.Tests.EditMode.Solutions
{
    // Solution: FixupRegistry decides which fixups ClientSimTestBase applies, and in what order.
    // Its disabled set is static and process-wide, so every test restores it.
    public class FixupRegistryTests
    {
        [TearDown]
        public void TearDown() => FixupRegistry.EnableAll();

        [Test]
        public void ActiveFixups_Default_IncludesProjectSettingsPreflight()
        {
            Assert.That(FixupRegistry.ActiveFixups.OfType<ClientSimProjectSettingsPreflight>(), Is.Not.Empty);
        }

        [Test]
        public void ActiveFixups_Default_IncludesPersistenceLeakFixup()
        {
            Assert.That(FixupRegistry.ActiveFixups.OfType<ClientSimPersistenceLeakFixup>(), Is.Not.Empty);
        }

        [Test]
        public void ActiveFixups_Default_RunsPreflightBeforeLeakFixup()
        {
            var order = FixupRegistry.ActiveFixups.Select(fixup => fixup.GetType()).ToList();

            Assert.That(order.IndexOf(typeof(ClientSimProjectSettingsPreflight)),
                Is.LessThan(order.IndexOf(typeof(ClientSimPersistenceLeakFixup))));
        }

        [Test]
        public void Disable_RegisteredFixup_RemovesItFromActiveFixups()
        {
            FixupRegistry.Disable<ClientSimPersistenceLeakFixup>();

            Assert.That(FixupRegistry.ActiveFixups.OfType<ClientSimPersistenceLeakFixup>(), Is.Empty);
        }

        [Test]
        public void Disable_CalledTwice_IsIdempotent()
        {
            FixupRegistry.Disable<ClientSimPersistenceLeakFixup>();

            Assert.DoesNotThrow(() => FixupRegistry.Disable<ClientSimPersistenceLeakFixup>());
        }

        [Test]
        public void EnableAll_AfterDisable_RestoresTheFixup()
        {
            FixupRegistry.Disable<ClientSimPersistenceLeakFixup>();

            FixupRegistry.EnableAll();

            Assert.That(FixupRegistry.ActiveFixups.OfType<ClientSimPersistenceLeakFixup>(), Is.Not.Empty);
        }

        [Test]
        public void Disable_UnregisteredType_LeavesRegisteredFixupsActive()
        {
            int activeBefore = FixupRegistry.ActiveFixups.Count();

            FixupRegistry.Disable<UnregisteredFixupDouble>();

            Assert.That(FixupRegistry.ActiveFixups.Count(), Is.EqualTo(activeBefore));
        }

        private sealed class UnregisteredFixupDouble : IPlayModeEnvironmentFixup
        {
            public void OnBeforeAnyTests()
            {
            }

            public void OnUnityTearDown()
            {
            }
        }
    }
}

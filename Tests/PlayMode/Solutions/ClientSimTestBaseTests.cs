using System.Collections;
using NUnit.Framework;
using UdonTestKit.ClientSim;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim;

namespace UdonTestKit.Tests.PlayMode.Solutions
{
    // Solution: ClientSimTestBase applies every active fixup in FixupRegistry: OnBeforeAnyTests from
    // its [OneTimeSetUp], and OnUnityTearDown from its [UnityTearDown] after its ClientSim session is
    // gone. A recording fixup is registered for each test and removed afterwards.
    public class ClientSimTestBaseTests
    {
        private RecordingFixup _fixup;
        private TestBase _base;

        [SetUp]
        public void SetUp()
        {
            _fixup = new RecordingFixup();
            FixupRegistry.Register(_fixup);
            _base = new TestBase();
        }

        [TearDown]
        public void TearDown()
        {
            FixupRegistry.Unregister(_fixup);
            FixupRegistry.EnableAll();
            _base.Players.Teardown();
            new ClientSimPersistenceLeakFixup().OnUnityTearDown();
        }

        [Test]
        public void OneTimeSetUp_RegisteredFixup_RunsItsOnBeforeAnyTests()
        {
            _base.ClientSimTestBase_OneTimeSetUp();

            Assert.That(_fixup.BeforeAnyTestsCalls, Is.EqualTo(1));
        }

        [Test]
        public void OneTimeSetUp_DisabledFixup_SkipsIt()
        {
            // Arrange
            FixupRegistry.Disable<RecordingFixup>();

            // Act
            _base.ClientSimTestBase_OneTimeSetUp();

            // Assert
            Assert.That(_fixup.BeforeAnyTestsCalls, Is.Zero);
        }

        [UnityTest]
        public IEnumerator UnityTearDown_AfterASession_RunsFixupOnceClientSimIsGone()
        {
            // Arrange
            yield return _base.StartSession();
            Assume.That(ClientSimMain.HasInstance(), Is.True, "The session didn't start.");

            // Act
            yield return _base.ClientSimTestBase_UnityTearDown();

            // Assert
            Assert.That(_fixup.UnityTearDownCalls, Is.EqualTo(1));
            Assert.That(_fixup.ClientSimWasRunningAtTearDown, Is.False);
        }

        // Has no tests of its own, so NUnit never runs it as a fixture.
        private sealed class TestBase : ClientSimTestBase
        {
            public new ClientSimPlayerEnvironment Players => base.Players;

            public IEnumerator StartSession() => StartClientSim();
        }

        private sealed class RecordingFixup : IPlayModeEnvironmentFixup
        {
            public int BeforeAnyTestsCalls;
            public int UnityTearDownCalls;
            public bool ClientSimWasRunningAtTearDown;

            public void OnBeforeAnyTests() => BeforeAnyTestsCalls++;

            public void OnUnityTearDown()
            {
                UnityTearDownCalls++;
                ClientSimWasRunningAtTearDown = ClientSimMain.HasInstance();
            }
        }
    }
}

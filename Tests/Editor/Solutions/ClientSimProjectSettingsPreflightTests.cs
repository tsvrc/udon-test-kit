using System;
using NUnit.Framework;
using VRC.SDK3.ClientSim.Editor;

namespace UdonTestKit.Editor.Tests.Solutions
{
    // Solution: ClientSimProjectSettingsPreflight fails before any test when ClientSim's own
    // settings check would fail. The problem is proven in Runtime/Problems/ClientSimInvalidSettingsProblemTests.
    // The checks are replaced, so both outcomes are tested without touching real settings.
    public class ClientSimProjectSettingsPreflightTests
    {
        private static readonly string[] NoneNamed = Array.Empty<string>();
        private static readonly string[] LayersAndMatrixNamed = { "VRChat layers", "collision matrix" };

        [Test]
        public void OnBeforeAnyTests_ClientSimCheckPasses_DoesNotFail()
        {
            var preflight = new ClientSimProjectSettingsPreflight(() => true, () => LayersAndMatrixNamed);

            Assert.DoesNotThrow(() => preflight.OnBeforeAnyTests());
        }

        [Test]
        public void OnBeforeAnyTests_ClientSimCheckFails_FailsNamingEachFailingSetting()
        {
            // Arrange
            var preflight = new ClientSimProjectSettingsPreflight(() => false, () => LayersAndMatrixNamed);

            // Act
            var failure = Assert.Throws<AssertionException>(() => preflight.OnBeforeAnyTests());

            // Assert
            Assert.That(failure.Message, Does.Contain("VRChat layers").And.Contain("collision matrix"));
        }

        [Test]
        public void OnBeforeAnyTests_ClientSimCheckFails_NamesTheFixMenuItem()
        {
            // Arrange
            var preflight = new ClientSimProjectSettingsPreflight(() => false, () => LayersAndMatrixNamed);

            // Act
            var failure = Assert.Throws<AssertionException>(() => preflight.OnBeforeAnyTests());

            // Assert
            Assert.That(failure.Message, Does.Contain("Tools > Udon Test Kit > Fix ClientSim Project Settings"));
        }

        [Test]
        public void OnBeforeAnyTests_ClientSimCheckFailsWithNoSettingNamed_StillFails()
        {
            var preflight = new ClientSimProjectSettingsPreflight(() => false, () => NoneNamed);

            Assert.Throws<AssertionException>(() => preflight.OnBeforeAnyTests());
        }

        [Test]
        public void FindInvalid_ClientSimCheckPasses_NamesNothing()
        {
            // Arrange
            Assume.That(ClientSimProjectSettingsSetup.IsUsingCorrectSettings(), Is.True,
                "This project's ClientSim settings are invalid, so this can't check the passing case.");

            // Act and Assert
            Assert.That(ClientSimProjectSettings.FindInvalid(ClientSimProjectSettings.All), Is.Empty);
        }
    }
}

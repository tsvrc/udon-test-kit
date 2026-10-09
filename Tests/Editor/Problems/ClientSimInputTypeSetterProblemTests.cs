using NUnit.Framework;
using UnityEditor;
using VRC.SDK3.ClientSim.Editor;
using Object = UnityEngine.Object;

namespace UdonTestKit.Editor.Tests.Problems
{
    // Problem: ClientSim's own SetInputTypeSettings, the one its settings window runs, sets the
    // Input System only, and its IsUsingCorrectInputTypeSettings check rejects that value. So
    // ClientSim's fix for the input type leaves its check failing.
    // Solved by ClientSimProjectSettings setting both input systems, tested in
    // Solutions/ClientSimProjectSettingsTests. A failure here means ClientSim changed, so the
    // kit could call ClientSim's setter instead.
    public class ClientSimInputTypeSetterProblemTests
    {
        private int _originalInputHandler;

        [SetUp]
        public void SetUp()
        {
            _originalInputHandler = InputHandler().intValue;
        }

        [TearDown]
        public void TearDown()
        {
            SerializedObject projectSettings = ProjectSettings();
            projectSettings.FindProperty("activeInputHandler").intValue = _originalInputHandler;
            projectSettings.ApplyModifiedProperties();
        }

        [Test]
        public void SetInputTypeSettings_FromValidSetting_LeavesTheCheckFailing()
        {
            // Arrange
            Assume.That(ClientSimProjectSettingsSetup.IsUsingCorrectInputTypeSettings(), Is.True,
                "This project's input type already fails ClientSim's check.");

            // Act
            ClientSimProjectSettingsSetup.SetInputTypeSettings();

            // Assert
            Assert.That(ClientSimProjectSettingsSetup.IsUsingCorrectInputTypeSettings(), Is.False);
        }

        private static SerializedProperty InputHandler()
        {
            return ProjectSettings().FindProperty("activeInputHandler");
        }

        private static SerializedObject ProjectSettings()
        {
            return new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/ProjectSettings.asset"));
        }
    }
}

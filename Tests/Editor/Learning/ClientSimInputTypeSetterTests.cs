using NUnit.Framework;
using UnityEditor;
using VRC.SDK3.ClientSim.Editor;
using Object = UnityEngine.Object;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // ClientSim's own input type setter writes a value its own check rejects, so
    // ClientSimProjectSettings writes 2, both input systems, instead.
    public class ClientSimInputTypeSetterTests
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
            SerializedProperty inputHandler = InputHandler();
            inputHandler.intValue = _originalInputHandler;
            inputHandler.serializedObject.ApplyModifiedProperties();
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

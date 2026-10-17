using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim.Editor;
using Object = UnityEngine.Object;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    // The Fix tests break one real setting of this project in memory, run the fix, and put the
    // original value back.
    public class ClientSimProjectSettingsTests
    {
        private const int InputSystemOnly = 1;

        private Action _restore;

        [TearDown]
        public void TearDown()
        {
            _restore?.Invoke();
            _restore = null;
        }

        [Test]
        public void AssertValid_ClientSimCheckPasses_DoesNotFail()
        {
            var settings = new[] { StandIn("layers", false, new List<string>()) };

            Assert.DoesNotThrow(() => ClientSimProjectSettings.AssertValid(() => true, settings));
        }

        [Test]
        public void AssertValid_ClientSimCheckFails_NamesEachFailingSettingAndTheFixMenuItem()
        {
            // Arrange
            var settings = new[]
            {
                StandIn("valid", true, new List<string>()),
                StandIn("layers", false, new List<string>()),
                StandIn("matrix", false, new List<string>()),
            };

            // Act
            var failure = Assert.Throws<AssertionException>(() => ClientSimProjectSettings.AssertValid(() => false, settings));

            // Assert
            Assert.That(failure.Message, Is.EqualTo("The ClientSim project settings check fails on: layers, matrix. " +
                "Fix them with Tools > TsVRC > Udon Test Kit > Fix ClientSim Project Settings."));
        }

        [Test]
        public void AssertValid_ClientSimCheckFailsWithNoSettingNamed_StillFails()
        {
            // Arrange
            var settings = new[] { StandIn("valid", true, new List<string>()) };

            // Act
            var failure = Assert.Throws<AssertionException>(() => ClientSimProjectSettings.AssertValid(() => false, settings));

            // Assert
            Assert.That(failure.Message, Is.EqualTo("The ClientSim project settings check fails. " +
                "Fix them with Tools > TsVRC > Udon Test Kit > Fix ClientSim Project Settings."));
        }

        [Test]
        public void Fix_SomeSettingsInvalid_AppliesOnlyThoseAndNamesThem()
        {
            // Arrange
            var applied = new List<string>();
            var settings = new[]
            {
                StandIn("valid", true, applied),
                StandIn("invalid", false, applied),
            };

            // Act
            IReadOnlyList<string> fixedSettings = ClientSimProjectSettings.Fix(settings);

            // Assert
            Assert.That(applied, Is.EqualTo(new[] { "invalid" }));
            Assert.That(fixedSettings, Is.EqualTo(new[] { "invalid" }));
        }

        [Test]
        public void Fix_AllSettingsValid_AppliesNothing()
        {
            // Arrange
            var applied = new List<string>();
            var settings = new[] { StandIn("valid", true, applied) };

            // Act
            IReadOnlyList<string> fixedSettings = ClientSimProjectSettings.Fix(settings);

            // Assert
            Assert.That(applied, Is.Empty);
            Assert.That(fixedSettings, Is.Empty);
        }

        [Test]
        public void Fix_InputAxesBroken_MakesClientSimCheckPassAndNamesIt()
        {
            SerializedObject inputManager = Load("ProjectSettings/InputManager.asset");
            SerializedProperty firstAxisName = inputManager.FindProperty("m_Axes").GetArrayElementAtIndex(0).FindPropertyRelative("m_Name");
            BreakAndFix(firstAxisName, firstAxisName.stringValue + "_Broken", "input axes", ClientSimProjectSettingsSetup.IsUsingCorrectInputAxesSettings, saveToDisk: true);
        }

        [Test]
        public void Fix_InputTypeBroken_MakesClientSimCheckPassAndNamesIt()
        {
            SerializedObject projectSettings = Load("ProjectSettings/ProjectSettings.asset");
            BreakAndFix(projectSettings.FindProperty("activeInputHandler"), InputSystemOnly, "input type", ClientSimProjectSettingsSetup.IsUsingCorrectInputTypeSettings);
        }

        [Test]
        public void Fix_AudioSpatializerBroken_MakesClientSimCheckPassAndNamesIt()
        {
            SerializedObject audioManager = Load("ProjectSettings/AudioManager.asset");
            BreakAndFix(audioManager.FindProperty("m_SpatializerPlugin"), "", "audio spatializer", ClientSimProjectSettingsSetup.IsUsingCorrectAudioSettings);
        }

        [Test]
        public void Fix_VRChatLayerBroken_MakesClientSimCheckPassAndNamesIt()
        {
            int layer = LayerMask.NameToLayer("PlayerLocal");
            Assume.That(layer, Is.Not.EqualTo(-1), "This project has no PlayerLocal layer to break.");
            SerializedObject tagManager = Load("ProjectSettings/TagManager.asset");
            BreakAndFix(tagManager.FindProperty("layers").GetArrayElementAtIndex(layer), "", "VRChat layers", UpdateLayers.AreLayersSetup);
        }

        [Test]
        public void Fix_CollisionMatrixBroken_MakesClientSimCheckPassAndNamesIt()
        {
            // Arrange
            AssumeClientSimSettingsValid();
            bool original = Physics.GetIgnoreLayerCollision(0, 0);
            _restore = () => Physics.IgnoreLayerCollision(0, 0, original);
            Physics.IgnoreLayerCollision(0, 0, !original);
            Assume.That(UpdateLayers.IsCollisionLayerMatrixSetup(), Is.False, "Flipping Default/Default didn't break the matrix check.");
            LogAssert.Expect(LogType.Log, "[UdonTestKit] Changed the ClientSim project settings: collision matrix.");

            // Act
            ClientSimProjectSettings.Fix();

            // Assert
            Assert.That(ClientSimProjectSettingsSetup.IsUsingCorrectSettings(), Is.True);
        }

        [Test]
        public void Fix_TwoSettingsBroken_LogsBothNames()
        {
            // Arrange
            AssumeClientSimSettingsValid();
            SerializedProperty inputType = Load("ProjectSettings/ProjectSettings.asset").FindProperty("activeInputHandler");
            SerializedProperty spatializer = Load("ProjectSettings/AudioManager.asset").FindProperty("m_SpatializerPlugin");
            object originalInputType = Read(inputType);
            object originalSpatializer = Read(spatializer);
            _restore = () =>
            {
                Write(inputType, originalInputType, saveToDisk: false);
                Write(spatializer, originalSpatializer, saveToDisk: false);
            };
            Write(inputType, InputSystemOnly, saveToDisk: false);
            Write(spatializer, "", saveToDisk: false);
            LogAssert.Expect(LogType.Log, "[UdonTestKit] Changed the ClientSim project settings: input type, audio spatializer.");

            // Act
            ClientSimProjectSettings.Fix();

            // Assert
            Assert.That(ClientSimProjectSettingsSetup.IsUsingCorrectSettings(), Is.True);
        }

        [Test]
        public void FixMenuItem_SettingsCorrect_LogsThatTheyWereAlreadyCorrect()
        {
            // Arrange
            AssumeClientSimSettingsValid();
            LogAssert.Expect(LogType.Log, "[UdonTestKit] The ClientSim project settings were already correct.");

            // Act
            bool ran = EditorApplication.ExecuteMenuItem(ClientSimProjectSettings.FixMenuPath);

            // Assert
            Assert.That(ran, Is.True);
        }

        private void BreakAndFix(SerializedProperty property, object brokenValue, string setting, Func<bool> check, bool saveToDisk = false)
        {
            // Arrange
            AssumeClientSimSettingsValid();
            object original = Read(property);
            _restore = () => Write(property, original, saveToDisk);
            Write(property, brokenValue, saveToDisk);
            Assume.That(check(), Is.False, $"Setting {property.propertyPath} to '{brokenValue}' didn't break the check.");
            LogAssert.Expect(LogType.Log, $"[UdonTestKit] Changed the ClientSim project settings: {setting}.");

            // Act
            ClientSimProjectSettings.Fix();

            // Assert
            Assert.That(ClientSimProjectSettingsSetup.IsUsingCorrectSettings(), Is.True);
        }

        private static void AssumeClientSimSettingsValid()
        {
            Assume.That(ClientSimProjectSettingsSetup.IsUsingCorrectSettings(), Is.True,
                "This project's ClientSim settings are already invalid, so its original values can't be restored safely.");
        }

        private static SerializedObject Load(string path)
        {
            return new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>(path));
        }

        private static object Read(SerializedProperty property)
        {
            property.serializedObject.Update();
            return property.propertyType == SerializedPropertyType.Integer ? (object)property.intValue : property.stringValue;
        }

        // ClientSim's input axes fix replaces the file on disk, so it can't repair a change that only
        // exists in memory: that case saves the broken value. The others stay in memory: saving
        // them too starts a script recompile in the middle of the run, and a later test fails with
        // "Can't add script behaviour while compiling".
        private static void Write(SerializedProperty property, object value, bool saveToDisk)
        {
            property.serializedObject.Update();
            if (value is int number)
            {
                property.intValue = number;
            }
            else
            {
                property.stringValue = (string)value;
            }
            property.serializedObject.ApplyModifiedProperties();
            if (saveToDisk)
            {
                AssetDatabase.SaveAssets();
            }
        }

        private static ClientSimProjectSettings.Setting StandIn(string name, bool isValid, List<string> applied)
        {
            return new ClientSimProjectSettings.Setting(name, () => isValid, () => applied.Add(name));
        }
    }
}

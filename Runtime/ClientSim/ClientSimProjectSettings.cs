using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.ClientSim.Editor;
using Object = UnityEngine.Object;

namespace Tsvrc.TestKit
{
    /// <summary>
    /// The project settings ClientSim checks before it starts
    /// (ClientSimProjectSettingsSetup.IsUsingCorrectSettings), each with the change that makes it
    /// pass, and a menu item that applies the changes for the ones that fail.
    /// </summary>
    public static class ClientSimProjectSettings
    {
        public const string FixMenuPath = "Tools/TsVRC/Test Kit/Fix ClientSim Project Settings";

        private const int BothInputSystems = 2;

        internal static readonly IReadOnlyList<Setting> All = new[]
        {
            new Setting("input axes", ClientSimProjectSettingsSetup.IsUsingCorrectInputAxesSettings, ClientSimProjectSettingsSetup.ApplyClientSimInputAxes),
            new Setting("input type", ClientSimProjectSettingsSetup.IsUsingCorrectInputTypeSettings, UseBothInputSystems),
            new Setting("audio spatializer", ClientSimProjectSettingsSetup.IsUsingCorrectAudioSettings, ClientSimProjectSettingsSetup.SetAudioSettings),
            new Setting("VRChat layers", UpdateLayers.AreLayersSetup, UpdateLayers.SetupEditorLayers),
            new Setting("collision matrix", UpdateLayers.IsCollisionLayerMatrixSetup, UpdateLayers.SetupCollisionLayerMatrix),
        };

        /// <summary>Applies the change for every setting that fails ClientSim's check, and logs
        /// which ones it changed.</summary>
        [MenuItem(FixMenuPath)]
        public static void Fix()
        {
            IReadOnlyList<string> fixedSettings = Fix(All);
            Debug.Log(fixedSettings.Count == 0
                ? "[TestKit] ClientSim's project settings were already correct."
                : "[TestKit] Changed ClientSim's project settings: " + string.Join(", ", fixedSettings) + ".");
        }

        internal static IReadOnlyList<string> Fix(IEnumerable<Setting> settings)
        {
            var fixedSettings = new List<string>();
            foreach (Setting setting in settings.Where(setting => !setting.IsValid()))
            {
                setting.Apply();
                fixedSettings.Add(setting.Name);
            }
            return fixedSettings;
        }

        // When ClientSim's check fails, ClientSim opens its settings window as it starts, and in batch
        // mode that window logs errors that fail the test without naming the cause.
        internal static void AssertValid()
        {
            AssertValid(ClientSimProjectSettingsSetup.IsUsingCorrectSettings, All);
        }

        internal static void AssertValid(Func<bool> clientSimCheck, IEnumerable<Setting> settings)
        {
            if (clientSimCheck())
            {
                return;
            }

            string[] invalid = settings.Where(setting => !setting.IsValid()).Select(setting => setting.Name).ToArray();
            Assert.Fail("ClientSim's project settings check fails" +
                (invalid.Length > 0 ? " on: " + string.Join(", ", invalid) : "") +
                ". Fix them with " + FixMenuPath.Replace("/", " > ") + ".");
        }

        // ClientSim's own SetInputTypeSettings sets the Input System only (1), which its check
        // rejects. ClientSim's window asks for both input systems, which is 2.
        private static void UseBothInputSystems()
        {
            var projectSettings = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/ProjectSettings.asset"));
            projectSettings.FindProperty("activeInputHandler").intValue = BothInputSystems;
            projectSettings.ApplyModifiedProperties();
        }

        internal sealed class Setting
        {
            public readonly string Name;
            public readonly Func<bool> IsValid;
            public readonly Action Apply;

            public Setting(string name, Func<bool> isValid, Action apply)
            {
                Name = name;
                IsValid = isValid;
                Apply = apply;
            }
        }
    }
}

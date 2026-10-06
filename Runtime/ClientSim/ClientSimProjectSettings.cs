using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.ClientSim.Editor;
using Object = UnityEngine.Object;

namespace UdonTestKit
{
    /// <summary>
    /// The project settings ClientSim checks before it starts (ClientSimProjectSettingsSetup.IsUsingCorrectSettings),
    /// each with the change that makes it pass. Fix() applies the changes for the ones that fail.
    /// </summary>
    public static class ClientSimProjectSettings
    {
        public const string FixMenuPath = "Tools/Udon Test Kit/Fix ClientSim Project Settings";

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
                ? "[UdonTestKit] ClientSim's project settings were already correct."
                : "[UdonTestKit] Changed ClientSim's project settings: " + string.Join(", ", fixedSettings) + ".");
        }

        internal static IReadOnlyList<string> Fix(IEnumerable<Setting> settings)
        {
            var fixedSettings = new List<string>();
            foreach (Setting setting in settings)
            {
                if (setting.IsValid())
                    continue;
                setting.Apply();
                fixedSettings.Add(setting.Name);
            }
            return fixedSettings;
        }

        internal static IReadOnlyList<string> FindInvalid(IEnumerable<Setting> settings)
        {
            return settings.Where(setting => !setting.IsValid()).Select(setting => setting.Name).ToList();
        }

        // ClientSim's own SetInputTypeSettings sets the Input System only (1), which its check
        // rejects (proven in EditMode/Problems/ClientSimInputTypeSetterProblemTests). ClientSim's
        // window asks for both input systems, which is 2.
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

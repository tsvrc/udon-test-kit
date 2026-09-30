using System;
using System.Collections.Generic;
using NUnit.Framework;
using VRC.SDK3.ClientSim.Editor;

namespace UdonTestKit.ClientSim
{
    /// <summary>
    /// When ClientSim starts, it runs ClientSimProjectSettingsSetup.IsUsingCorrectSettings(), and if
    /// that fails it force-opens its settings window. In batch mode the window logs errors, and
    /// Unity Test Framework fails the test on them. This fixup runs the same check before any test
    /// and fails with a message naming the settings check.
    /// </summary>
    public sealed class ClientSimProjectSettingsPreflight : IPlayModeEnvironmentFixup
    {
        private readonly Func<bool> _areSettingsValid;
        private readonly Func<IReadOnlyList<string>> _findInvalidSettings;

        public ClientSimProjectSettingsPreflight()
            : this(ClientSimProjectSettingsSetup.IsUsingCorrectSettings, FindInvalidSettings)
        {
        }

        internal ClientSimProjectSettingsPreflight(Func<bool> areSettingsValid, Func<IReadOnlyList<string>> findInvalidSettings)
        {
            _areSettingsValid = areSettingsValid;
            _findInvalidSettings = findInvalidSettings;
        }

        public void OnBeforeAnyTests()
        {
            if (_areSettingsValid())
                return;

            IReadOnlyList<string> invalid = _findInvalidSettings();
            string which = invalid.Count > 0 ? " Failing: " + string.Join(", ", invalid) + "." : "";
            Assert.Fail("ClientSim's project settings check (ClientSimProjectSettingsSetup.IsUsingCorrectSettings) " +
                "fails, so ClientSim would open its settings window and fail tests in batch mode." + which);
        }

        public void OnUnityTearDown()
        {
        }

        // Names which of ClientSim's individual settings checks fail, for the failure message.
        // IsUsingCorrectSettings() stays the authority on whether the preflight fails at all.
        internal static IReadOnlyList<string> FindInvalidSettings()
        {
            var invalid = new List<string>();
            if (!ClientSimProjectSettingsSetup.IsUsingCorrectInputAxesSettings())
                invalid.Add("input axes");
            if (!ClientSimProjectSettingsSetup.IsUsingCorrectInputTypeSettings())
                invalid.Add("input type");
            if (!ClientSimProjectSettingsSetup.IsUsingCorrectAudioSettings())
                invalid.Add("audio spatializer");
            if (!UpdateLayers.AreLayersSetup())
                invalid.Add("VRChat layers");
            if (!UpdateLayers.IsCollisionLayerMatrixSetup())
                invalid.Add("collision matrix");
            return invalid;
        }
    }
}

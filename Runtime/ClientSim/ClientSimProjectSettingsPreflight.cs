using System;
using System.Collections.Generic;
using NUnit.Framework;
using VRC.SDK3.ClientSim.Editor;

namespace UdonTestKit
{
    /// <summary>
    /// When ClientSim starts, it runs ClientSimProjectSettingsSetup.IsUsingCorrectSettings(), and if
    /// that fails it force-opens its settings window. In batch mode the window logs errors, and
    /// Unity Test Framework fails the test on them. This fixup runs the same check before any test
    /// and fails with a message naming the failing settings and the menu item that fixes them.
    /// </summary>
    public sealed class ClientSimProjectSettingsPreflight : IPlayModeEnvironmentFixup
    {
        private readonly Func<bool> _areSettingsValid;
        private readonly Func<IReadOnlyList<string>> _findInvalidSettings;

        public ClientSimProjectSettingsPreflight()
            : this(ClientSimProjectSettingsSetup.IsUsingCorrectSettings, () => ClientSimProjectSettings.FindInvalid(ClientSimProjectSettings.All))
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
                "fails, so ClientSim would open its settings window and fail tests in batch mode." + which +
                " Fix it with " + ClientSimProjectSettings.FixMenuPath.Replace("/", " > ") + ".");
        }

        public void OnUnityTearDown()
        {
        }
    }
}

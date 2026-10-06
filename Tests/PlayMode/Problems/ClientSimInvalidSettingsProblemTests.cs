#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim;
using VRC.SDK3.ClientSim.Editor;

namespace UdonTestKit.Tests.PlayMode.Problems
{
    // Problem: in Play Mode, ClientSim's settings hook is ClientSimProjectSettingsSetup.IsUsingCorrectSettings.
    // When it fails, ClientSim force-opens its settings window, which logs errors in batch mode, and
    // unexpected errors fail the test (EditMode/Problems/UnityTestFrameworkLogProblemTests).
    // Solved by ClientSimProjectSettingsPreflight, tested in EditMode/Solutions/ClientSimProjectSettingsPreflightTests.
    // A failure here means ClientSim changed, so the preflight may no longer be needed.
    //
    // ClientSimMenu's two editor hooks are internal statics, reached by name. The check hook is
    // forced to fail instead of breaking this project's real settings.
    public class ClientSimInvalidSettingsProblemTests
    {
        private const string NoGraphicDevice = "No graphic device is available";

        private readonly ClientSimPlayerEnvironment _players = new ClientSimPlayerEnvironment();
        private readonly List<string> _loggedErrors = new List<string>();
        private object _savedCheckHook;
        private object _savedOpenHook;

        [SetUp]
        public void SetUp()
        {
            _savedCheckHook = CheckHookField().GetValue(null);
            _savedOpenHook = OpenHookField().GetValue(null);
        }

        [TearDown]
        public void TearDown()
        {
            CheckHookField().SetValue(null, _savedCheckHook);
            OpenHookField().SetValue(null, _savedOpenHook);
            _players.Teardown();
            new ClientSimPersistenceLeakFixup().OnUnityTearDown();

            foreach (ClientSimSettingsWindow window in Resources.FindObjectsOfTypeAll<ClientSimSettingsWindow>())
                UnityEngine.Object.DestroyImmediate(window);

            Application.logMessageReceived -= RecordError;
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void CheckValidSettingsHook_InPlayMode_IsClientSimProjectSettingsCheck()
        {
            // Arrange
            MethodInfo settingsCheck = typeof(ClientSimProjectSettingsSetup).GetMethod(nameof(ClientSimProjectSettingsSetup.IsUsingCorrectSettings));

            // Act
            var hook = (Delegate)CheckHookField().GetValue(null);

            // Assert
            Assert.That(hook?.GetInvocationList().Select(subscriber => subscriber.Method), Has.Member(settingsCheck));
        }

        [UnityTest]
        public IEnumerator Start_SettingsCheckFails_ForceOpensSettingsWindow()
        {
            // Arrange
            int openRequests = 0;
            CheckHookField().SetValue(null, (Func<bool>)(() => false));
            OpenHookField().SetValue(null, (Action)(() => openRequests++));

            // Act
            yield return _players.Start();

            // Assert
            Assert.That(openRequests, Is.EqualTo(1));
        }

        [Test]
        public void OpenSettingsWindow_InBatchMode_LogsNoGraphicDeviceError()
        {
            // Arrange
            Assume.That(Application.isBatchMode, Is.True, "Only batch mode has no graphics device for the window.");
            LogAssert.ignoreFailingMessages = true;
            Application.logMessageReceived += RecordError;

            // Act
            ClientSimSettingsWindow.Init();

            // Assert
            Assert.That(_loggedErrors, Has.Some.Contains(NoGraphicDevice));
        }

        private void RecordError(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error)
                _loggedErrors.Add(message);
        }

        private static FieldInfo CheckHookField() => HookField("checkValidSettingsHook");

        private static FieldInfo OpenHookField() => HookField("openSettingsHook");

        private static FieldInfo HookField(string name)
        {
            FieldInfo field = typeof(ClientSimMenu).GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, $"ClientSimMenu.{name} not found. Has ClientSim changed?");
            return field;
        }
    }
}
#endif

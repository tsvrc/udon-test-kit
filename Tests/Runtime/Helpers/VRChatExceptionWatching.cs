using System;
using System.IO;
using System.Reflection;
using UdonSharpEditor;
using UnityEngine;

namespace Tsvrc.UdonTestKit.Tests.Helpers
{
    // Turns UdonSharp's watching of the VRChat client's log on or off until it's disposed. UdonSharp
    // keeps its settings class internal, so this reaches it by reflection.
    internal sealed class VRChatExceptionWatching : IDisposable
    {
        private static readonly Type SettingsType =
            typeof(UdonSharpEditorUtility).Assembly.GetType("UdonSharpEditor.UdonSharpSettings", throwOnError: true);
        private static readonly FieldInfo ListenField = SettingsType.GetField("listenForVRCExceptions");
        private static readonly FieldInfo WatcherModeField = SettingsType.GetField("watcherMode");

        private readonly object _listenedBefore;
        private readonly object _watcherModeBefore;

        public VRChatExceptionWatching(bool on)
        {
            object settings = Settings;
            _listenedBefore = ListenField.GetValue(settings);
            _watcherModeBefore = WatcherModeField.GetValue(settings);
            ListenField.SetValue(settings, on);
            if (!on)
            {
                WatcherModeField.SetValue(settings, Enum.Parse(WatcherModeField.FieldType, "Disabled"));
            }
        }

        private static object Settings =>
            SettingsType.GetMethod("GetSettings", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);

        public void Dispose()
        {
            object settings = Settings;
            ListenField.SetValue(settings, _listenedBefore);
            WatcherModeField.SetValue(settings, _watcherModeBefore);
        }

        // Drops the errors UdonSharp queued while it watched for a VRChat log it couldn't find. It logs a
        // copy of each queued Udon exception once watching stops, so a test that stops it would get the
        // copies of exceptions from earlier tests.
        public static void DropQueuedErrors()
        {
            object queue = typeof(RuntimeLogWatcher)
                .GetField("_debugOutputQueue", BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null);
            queue.GetType().GetMethod("Clear").Invoke(queue, null);
        }

        // Whether the folder UdonSharp watches for the VRChat client's log exists, built the way
        // RuntimeLogWatcher builds it.
        public static bool VRChatDataFolderExists()
        {
            string folder = Application.platform == RuntimePlatform.LinuxEditor
                ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                    + "/.local/share/Steam/steamapps/compatdata/438100/pfx/drive_c/users/steamuser/AppData/LocalLow/VRChat/VRChat/"
                : Path.Combine(Directory.GetParent(Application.persistentDataPath).Parent.FullName, "VRChat", "VRChat");
            return Directory.Exists(folder);
        }
    }
}

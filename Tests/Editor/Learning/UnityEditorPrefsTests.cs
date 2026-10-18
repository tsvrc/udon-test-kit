using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // Unity keeps EditorPrefs in one store per user: the registry on Windows, a prefs file on Linux.
    // An Editor reads a key back from its own copy, though, whatever is written to the store after.
    // So BuiltWorld reads the world file path its own export recorded, even while another Unity
    // process of the same user builds a world.
    public class UnityEditorPrefsTests
    {
        private const string Key = "Tsvrc.UdonTestKit.Editor.Tests.Learning.UnityEditorPrefsTests";

        [TearDown]
        public void TearDown() => EditorPrefs.DeleteKey(Key);

        [Test, UnityPlatform(RuntimePlatform.WindowsEditor)]
        public void GetString_RegistryValueChangedAfterSetString_ReturnsWhatThisEditorWrote()
        {
            // Arrange
            EditorPrefs.SetString(Key, "Written here");

            // Act
            using (var registry = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Unity Technologies\Unity Editor 5.x", writable: true))
            {
                string name = registry.GetValueNames().Single(value => value.StartsWith(Key + "_h"));
                registry.SetValue(name, Encoding.UTF8.GetBytes("Written elsewhere\0"), Microsoft.Win32.RegistryValueKind.Binary);
            }

            // Assert
            Assert.That(EditorPrefs.GetString(Key), Is.EqualTo("Written here"));
        }

        [Test, UnityPlatform(RuntimePlatform.LinuxEditor)]
        public void GetString_PrefsFileChangedAfterSetString_ReturnsWhatThisEditorWrote()
        {
            // Arrange
            EditorPrefs.SetString(Key, "Written here");
            string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/unity3d/prefs");
            string before = File.ReadAllText(file);
            string elsewhere = Convert.ToBase64String(Encoding.UTF8.GetBytes("Written elsewhere"));

            // Act
            File.WriteAllText(file, before.Replace("</unity_prefs>", $"\t<pref name=\"{Key}\" type=\"string\">{elsewhere}</pref>\n</unity_prefs>"));
            string read;
            try
            {
                read = EditorPrefs.GetString(Key);
            }
            finally
            {
                File.WriteAllText(file, before);
            }

            // Assert
            Assert.That(read, Is.EqualTo("Written here"));
        }
    }
}

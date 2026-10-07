using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace UdonTestKit.Tests.EditMode.Solutions
{
    // Solution: CommandLineScripts writes both scripts with the path from their folder to the
    // project, so they find the project wherever both are cloned together. Files are written to a
    // temporary folder outside Assets. That the scripts run, from VCC and .unitypackage installs,
    // is checked in FINDINGS.md (F12).
    public class CommandLineScriptsTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "UdonTestKit.CommandLineScriptsTests");
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_root, recursive: true);
        }

        [Test]
        public void Create_FolderAboveTheProject_StoresThePathFromTheFolder()
        {
            // Act
            CommandLineScripts.Create(_root, Path.Combine(_root, "Bob's World"));

            // Assert
            Assert.That(PowerShellScript(), Does.Contain("Join-Path $PSScriptRoot 'Bob''s World'"));
            Assert.That(ShellScript(), Does.Contain("project=$(dirname \"$0\")/'Bob'\\''s World'"));
        }

        [Test]
        public void Create_ProjectFolder_StoresTheFolderItself()
        {
            // Act
            CommandLineScripts.Create(_root, _root);

            // Assert
            Assert.That(PowerShellScript(), Does.Contain("Join-Path $PSScriptRoot '.'"));
            Assert.That(ShellScript(), Does.Contain("project=$(dirname \"$0\")/'.'"));
        }

        [Test]
        public void Create_Scripts_PassTheScriptVersion()
        {
            // Act
            CommandLineScripts.Create(_root, _root);

            // Assert
            string version = CommandLineScripts.ScriptVersion.ToString();
            Assert.That(PowerShellScript(), Does.Contain($"'-udonTestKitScriptVersion', '{version}'"));
            Assert.That(ShellScript(), Does.Contain($"-udonTestKitScriptVersion {version} "));
        }

        [Test]
        public void Create_Scripts_UseTheEncodingAndLineEndingsTheirShellsRead()
        {
            // Act
            CommandLineScripts.Create(_root, _root);

            // Assert
            byte[] bom = Encoding.UTF8.GetPreamble();
            Assert.That(File.ReadAllBytes(Path.Combine(_root, CommandLineScripts.WindowsScriptName)).Take(bom.Length), Is.EqualTo(bom));
            Assert.That(File.ReadAllBytes(Path.Combine(_root, CommandLineScripts.LinuxScriptName)).Take(bom.Length), Is.Not.EqualTo(bom));
            Assert.That(ShellScript(), Does.Not.Contain("\r"));
        }

        [Test]
        public void Create_Twice_ReplacesTheScripts()
        {
            // Arrange
            CommandLineScripts.Create(_root, Path.Combine(_root, "First"));

            // Act
            CommandLineScripts.Create(_root, Path.Combine(_root, "Second"));

            // Assert
            Assert.That(ShellScript(), Does.Contain("/'Second'").And.Not.Contain("/'First'"));
        }

        [Test]
        public void Create_ProjectOnAnotherDrive_Throws()
        {
            Assume.That(Application.platform, Is.EqualTo(RuntimePlatform.WindowsEditor), "Only Windows has drives.");
            string otherDrive = _root.StartsWith("Z:", StringComparison.OrdinalIgnoreCase) ? "Y:\\World" : "Z:\\World";

            Assert.That(() => CommandLineScripts.Create(_root, otherDrive), Throws.ArgumentException);
        }

        private string PowerShellScript() => File.ReadAllText(Path.Combine(_root, CommandLineScripts.WindowsScriptName));

        private string ShellScript() => File.ReadAllText(Path.Combine(_root, CommandLineScripts.LinuxScriptName));
    }
}

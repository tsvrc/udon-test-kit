using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // Invoke-UnityTests.ps1 lists the assembly definitions Unity imports, and Windows can mark a file
    // or folder hidden with an attribute.
    [UnityPlatform(RuntimePlatform.WindowsEditor)]
    public class UnityHiddenAttributeTests
    {
        private const string Folder = "Assets/UdonTestKitHiddenAttributeTests";
        private const string HiddenFolder = Folder + "/Hidden";

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(HiddenFolder))
            {
                new DirectoryInfo(HiddenFolder).Attributes &= ~FileAttributes.Hidden;
            }
            AssetDatabase.DeleteAsset(Folder);
        }

        [Test]
        public void Refresh_FolderWithTheHiddenAttribute_IsNotImported()
        {
            // Arrange
            Directory.CreateDirectory(HiddenFolder);
            File.WriteAllText(HiddenFolder + "/Note.txt", "");
            new DirectoryInfo(HiddenFolder).Attributes |= FileAttributes.Hidden;

            // Act
            AssetDatabase.Refresh();

            // Assert
            Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(HiddenFolder + "/Note.txt"), Is.Null);
        }

        [Test]
        public void Refresh_FileWithTheHiddenAttribute_IsImported()
        {
            // Arrange
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Folder + "/Note.txt", "");
            File.SetAttributes(Folder + "/Note.txt", FileAttributes.Hidden);

            // Act
            AssetDatabase.Refresh();

            // Assert
            Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + "/Note.txt"), Is.Not.Null);
        }
    }
}

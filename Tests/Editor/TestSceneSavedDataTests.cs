using System.IO;
using System.Linq;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;
using UnityEditor;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    public class TestSceneSavedDataTests
    {
        private string _storageRoot;

        [TearDown]
        public void TearDown()
        {
            if (_storageRoot != null)
            {
                Directory.Delete(_storageRoot, recursive: true);
            }
            _storageRoot = null;
        }

        [Test]
        public void RunFinished_TestSceneSavedData_DeletesIt()
        {
            // Arrange
            _storageRoot = Path.GetFullPath(FileUtil.GetUniqueTempPathInProject());
            string file = Path.Combine(_storageRoot, "PlayerData", "PlayerData_1_InitTestScene639276975825145332.json");
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file, "{}");

            // Act
            new TestSceneSavedData.Callbacks(_storageRoot).RunFinished(null);

            // Assert
            Assert.That(File.Exists(file), Is.False);
        }

        [Test]
        public void Callbacks_AfterTheDomainLoad_AreRegisteredOnceForTheClientSimStorage()
        {
            // Act
            TestSceneSavedData.Callbacks[] registered = TestRunnerCallbacks.Registered().OfType<TestSceneSavedData.Callbacks>().ToArray();

            // Assert
            Assert.That(registered, Has.Length.EqualTo(1));
            Assert.That(PrivateFieldAccess.GetField<string>(registered[0], "_storageRoot"), Is.EqualTo(ClientSimSession.StorageRoot));
        }
    }
}

using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Player;

namespace UdonTestKit.Tests.EditMode.Solutions
{
    // Solution: the kit's assembly compiles only when UNITY_INCLUDE_TESTS is defined, so a world
    // build leaves it out, even though the assembly targets every platform and is auto-referenced.
    // Without that constraint the world's player scripts don't compile at all.
    public class PlayerBuildTests
    {
        private string _outputFolder;

        [TearDown]
        public void TearDown()
        {
            if (_outputFolder != null && Directory.Exists(_outputFolder))
                Directory.Delete(_outputFolder, recursive: true);
        }

        [Test]
        public void CompilePlayerScripts_WindowsBuild_LeavesOutTheKit()
        {
            // Arrange
            _outputFolder = Path.Combine(Path.GetTempPath(), "UdonTestKit.PlayerBuildTests");
            var settings = new ScriptCompilationSettings
            {
                target = BuildTarget.StandaloneWindows64,
                group = BuildTargetGroup.Standalone,
            };

            // Act
            ScriptCompilationResult result = PlayerBuildInterface.CompilePlayerScripts(settings, _outputFolder);

            // Assert
            TestContext.WriteLine("Player assemblies: " + string.Join(", ", result.assemblies));
            Assert.That(result.assemblies, Has.Some.StartsWith("VRC."),
                "The world's player scripts didn't compile.");
            Assert.That(result.assemblies, Has.None.StartsWith("UdonTestKit"));
        }
    }
}

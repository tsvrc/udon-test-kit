using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Player;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    public class PlayerBuildTests
    {
        private string _outputFolder;

        [TearDown]
        public void TearDown()
        {
            if (_outputFolder != null && Directory.Exists(_outputFolder))
            {
                Directory.Delete(_outputFolder, recursive: true);
            }
        }

        [Test]
        public void CompilePlayerScripts_WindowsBuild_HoldsOnlyTheUdonAssembliesOfTheKit()
        {
            // Arrange
            _outputFolder = Path.Combine(Path.GetTempPath(), "Tsvrc.UdonTestKit.PlayerBuildTests");
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
            // UdonCallLog and the kit's Udon test doubles run as Udon, and the compile for a world
            // upload reads only player assemblies.
            Assert.That(result.assemblies.Where(assembly => assembly.StartsWith("Tsvrc.UdonTestKit")),
                Is.EquivalentTo(new[] { "Tsvrc.UdonTestKit.UdonSpies.dll", "Tsvrc.UdonTestKit.Tests.UdonDoubles.dll" }));
        }
    }
}

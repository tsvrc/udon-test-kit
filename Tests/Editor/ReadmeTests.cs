using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.Compilation;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    // The README shows what the kit writes and requires.
    public class ReadmeTests
    {
        [Test]
        public void ExamplePlayModeTests_InTheReadme_IsTheExampleCreateWrites()
        {
            // Arrange
            string readme = Read("README.md");

            // Act
            int start = readme.IndexOf("```csharp\n") + "```csharp\n".Length;
            string block = readme.Substring(start, readme.IndexOf("```", start) - start);

            // Assert
            Assert.That(block, Is.EqualTo(TestAssemblyCreator.ExamplePlayModeTests.Replace("\r\n", "\n")));
        }

        [Test]
        public void MenuPath_InTheReadme_IsTheMenuItem()
        {
            Assert.That(Read("README.md"), Does.Contain($"**{TestAssemblyCreator.MenuPath.Replace("/", " > ")}**"));
        }

        // A VPM range ^3.8.2 takes 3.8.2 and every later 3.x version.
        [Test]
        public void Requirements_InTheReadme_AreThoseOfThePackageManifest()
        {
            // Arrange
            string manifest = Read("package.json");
            string unity = Regex.Match(manifest, "\"unity\": \"([^\"]+)\"").Groups[1].Value;
            Match sdk = Regex.Match(manifest, "\"com\\.vrchat\\.worlds\": \"\\^((\\d+)\\.[^\"]+)\"");

            // Act and assert
            Assert.That(Read("README.md"), Does.Contain(
                $"Requires Unity {unity} and the VRChat Worlds SDK {sdk.Groups[1].Value} or a later {sdk.Groups[2].Value}.x version."));
        }

        // A checkout on Windows can turn the line endings of a file into CRLF.
        private static string Read(string file)
        {
            string kitRoot = Path.GetDirectoryName(Path.GetDirectoryName(
                CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName("Tsvrc.UdonTestKit")));
            return File.ReadAllText(Path.Combine(kitRoot, file)).Replace("\r\n", "\n");
        }
    }
}

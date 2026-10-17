using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.Compilation;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    // The README shows what the kit writes and requires, and CONTRIBUTING what it requires.
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
            Assert.That(block, Is.EqualTo(TestAssemblyCreator.Example(TestAssemblyCreator.ExamplePlayModeTests, "MyWorld").Replace("\r\n", "\n")));
        }

        [Test]
        public void MenuPath_InTheReadme_IsTheMenuItem()
        {
            Assert.That(Read("README.md"), Does.Contain($"**{TestAssemblyCreator.MenuPath.Replace("/", " > ")}**"));
        }

        // A VPM range ^x.y.z takes that version and every later one with the same major number.
        [TestCase("README.md")]
        [TestCase("CONTRIBUTING.md")]
        public void Requirements_InTheDoc_AreThoseOfThePackageManifest(string doc)
        {
            // Arrange
            string manifest = Read("package.json");
            string unity = Regex.Match(manifest, "\"unity\": \"([^\"]+)\"").Groups[1].Value;
            Match sdk = Regex.Match(manifest, "\"com\\.vrchat\\.worlds\": \"\\^((\\d+)\\.[^\"]+)\"");

            // Act and assert: Markdown reads a line break as a space.
            Assert.That(Regex.Replace(Read(doc), @"\s+", " "), Does.Contain(
                $"Unity {unity} and the VRChat Worlds SDK {sdk.Groups[1].Value} or a later {sdk.Groups[2].Value}.x version"));
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

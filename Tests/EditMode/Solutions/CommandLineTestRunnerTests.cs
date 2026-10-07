using System.Linq;
using NUnit.Framework;
using UnityEditor.Compilation;

namespace UdonTestKit.Tests.EditMode.Solutions
{
    // Solution: CommandLineTestRunner runs the tests in the project's own assemblies, not the ones
    // that come with packages. Running the runner itself needs a separate Unity process, so its runs,
    // and the problems each part of it solves, are checked in FINDINGS.md (F11).
    public class CommandLineTestRunnerTests
    {
        [Test]
        public void ProjectAssemblies_InThisProject_IncludeTheKitsTests()
        {
            Assert.That(CommandLineTestRunner.ProjectAssemblies(),
                Is.SupersetOf(new[] { "UdonTestKit.Tests.EditMode", "UdonTestKit.Tests.PlayMode" }));
        }

        [Test]
        public void ProjectAssemblies_InThisProject_LeaveOutThePackagesTests()
        {
            // Arrange
            string[] editorAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor).Select(assembly => assembly.name).ToArray();
            Assume.That(editorAssemblies, Has.Member("UnityEditorTests"), "The VRChat SDK's UnityEditorTests assembly isn't in this project.");

            // Act
            string[] projectAssemblies = CommandLineTestRunner.ProjectAssemblies().ToArray();

            // Assert
            Assert.That(projectAssemblies, Has.No.Member("UnityEditorTests"));
        }
    }
}

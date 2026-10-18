using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine.TestTools;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // TestAssemblyCreator finds the world's own assemblies from what Unity reports of the assembly
    // definitions it imported, before any compile. Unity flags an Editor-only assembly definition
    // as an Editor assembly, and no other, so the world's Editor tooling is found by that flag. An
    // Editor-only assembly definition gets NUnit without listing it, and the player assemblies Unity
    // reports without test assemblies keep one that lists NUnit unless a define constraint leaves
    // it out. So TestAssemblyCreator takes a test assembly to be one whose definition lists NUnit.
    public class UnityCompilationPipelineTests
    {
        private const string Folder = "Assets/UdonTestKitCompilationPipelineTests";

        [UnityTearDown]
        public IEnumerator DeleteTheScripts() => ProjectScripts.DeleteFolders(Folder);

        [TestCase("Tsvrc.UdonTestKit.Editor.Tests", ExpectedResult = true)]
        [TestCase("Tsvrc.UdonTestKit.Tests", ExpectedResult = false)]
        public bool GetAssemblies_AssemblyDefinition_IsAnEditorAssemblyOnlyWhenEditorOnly(string name)
        {
            Assembly assembly = CompilationPipeline.GetAssemblies(AssembliesType.Editor).Single(candidate => candidate.name == name);

            return (assembly.flags & AssemblyFlags.EditorAssembly) != 0;
        }

        [Test]
        public void GetAssemblies_EditorOnlyAssemblyDefinitionNotListingNUnit_ReferencesNUnit()
        {
            // Arrange
            Import("Tooling", "{ \"name\": \"UdonTestKitTooling\", \"includePlatforms\": [\"Editor\"] }");

            // Act
            Assembly tooling = CompilationPipeline.GetAssemblies(AssembliesType.Editor).Single(assembly => assembly.name == "UdonTestKitTooling");

            // Assert
            Assert.That(tooling.allReferences.Select(Path.GetFileName), Has.Member("nunit.framework.dll"));
        }

        [Test]
        public void GetAssemblies_AssemblyDefinitionListingNUnitWithoutDefineConstraints_IsAPlayerAssemblyWithoutTests()
        {
            // Arrange
            Import("Tests", "{ \"name\": \"UdonTestKitListsNUnit\", \"overrideReferences\": true, \"precompiledReferences\": [\"nunit.framework.dll\"] }");

            // Act
            var playerWithoutTests = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies).Select(assembly => assembly.name);

            // Assert
            Assert.That(playerWithoutTests, Has.Member("UdonTestKitListsNUnit"));
        }

        // An assembly definition with one script, imported.
        private static void Import(string name, string definition)
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(Folder));
            }
            AssetDatabase.CreateFolder(Folder, name);
            File.WriteAllText($"{Folder}/{name}/{name}.asmdef", definition);
            File.WriteAllText($"{Folder}/{name}/UdonTestKit{name}Script.cs", $"public class UdonTestKit{name}Script {{ }}");
            AssetDatabase.ImportAsset($"{Folder}/{name}/{name}.asmdef");
            AssetDatabase.ImportAsset($"{Folder}/{name}/UdonTestKit{name}Script.cs");
        }
    }
}

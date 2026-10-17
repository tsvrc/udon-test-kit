using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    public class TestAssemblyUpdaterTests
    {
        // At the path the docs give. This project has no test assemblies the kit created.
        [Test]
        public void MenuItem_NothingToAdd_LogsThatTheTestAssembliesReferenceEveryAssemblyDefinition()
        {
            // Arrange
            LogAssert.Expect(LogType.Log, "[UdonTestKit] The test assemblies reference every assembly definition of the world.");

            // Act
            bool ran = EditorApplication.ExecuteMenuItem("Tools/TsVRC/Udon Test Kit/Update Test Assembly References");

            // Assert
            Assert.That(ran, Is.True);
        }

        [Test]
        public void CreatedAssemblyDefinitions_AssemblyNames_FindsEachKindByItsSuffixUnderAssetsButTheKitsOwn()
        {
            // Arrange
            var definitions = new Dictionary<string, string>
            {
                ["MyWorld.Tests.EditMode"] = "Assets/Tests/EditMode/MyWorld.Tests.EditMode.asmdef",
                ["MyWorld.Tests.PlayMode"] = "Assets/Tests/PlayMode/MyWorld.Tests.PlayMode.asmdef",
                ["MyWorld.Tests.Doubles"] = "Assets/Tests/Doubles/MyWorld.Tests.Doubles.asmdef",
                ["MyWorld.Tests.UdonDoubles"] = "Assets/Tests/UdonDoubles/MyWorld.Tests.UdonDoubles.asmdef",
                ["MyWorld.Runtime"] = "Assets/MyWorld/MyWorld.Runtime.asmdef",
                ["MyWorld.Tests.EditMode.Helpers"] = "Assets/Tests/Helpers/MyWorld.Tests.EditMode.Helpers.asmdef",
                ["Tsvrc.UdonTestKit.Tests.UdonDoubles"] = "Assets/udon-test-kit/Tests/UdonDoubles/Tsvrc.UdonTestKit.Tests.UdonDoubles.asmdef",
                ["Other.Tests.PlayMode"] = "Packages/com.example.other/Tests/Other.Tests.PlayMode.asmdef",
            };

            // Act
            var created = TestAssemblyUpdater.CreatedAssemblyDefinitions(definitions.Keys, name => definitions[name])
                .Select(definition => $"{definition.kind} {definition.definition}");

            // Assert
            Assert.That(created, Is.EqualTo(new[]
            {
                "EditModeTests Assets/Tests/EditMode/MyWorld.Tests.EditMode.asmdef", "PlayModeTests Assets/Tests/PlayMode/MyWorld.Tests.PlayMode.asmdef",
                "Doubles Assets/Tests/Doubles/MyWorld.Tests.Doubles.asmdef", "UdonDoubles Assets/Tests/UdonDoubles/MyWorld.Tests.UdonDoubles.asmdef",
            }));
        }

        // As TestAssemblyCreator writes an assembly definition.
        [Test]
        public void WithMissingReferences_ReferencesOnOneLine_AddsTheMissingOnesAfterTheOthers()
        {
            // Arrange
            const string json = "{\n    \"name\": \"A\",\n    \"references\": [\"Tsvrc.UdonTestKit\", \"Mine\"],\n    \"autoReferenced\": false\n}\n";

            // Act
            string updated = TestAssemblyUpdater.WithMissingReferences(json, new[] { "Mine", "MyWorld.Runtime" });

            // Assert
            Assert.That(updated, Is.EqualTo("{\n    \"name\": \"A\",\n    \"references\": [\"Tsvrc.UdonTestKit\", \"Mine\", \"MyWorld.Runtime\"],\n    \"autoReferenced\": false\n}\n"));
        }

        // As the Unity Editor writes an assembly definition.
        [Test]
        public void WithMissingReferences_ReferencesOnALineEach_AddsEachOnALineOfItsOwn()
        {
            // Arrange
            const string json = "{\n    \"name\": \"A\",\n    \"references\": [\n        \"Mine\"\n    ],\n    \"autoReferenced\": false\n}";

            // Act
            string updated = TestAssemblyUpdater.WithMissingReferences(json, new[] { "MyWorld.Runtime", "MyWorld.Audio" });

            // Assert
            Assert.That(updated, Is.EqualTo(
                "{\n    \"name\": \"A\",\n    \"references\": [\n        \"Mine\",\n        \"MyWorld.Runtime\",\n        \"MyWorld.Audio\"\n    ],\n    \"autoReferenced\": false\n}"));
        }

        [Test]
        public void WithMissingReferences_NoReferences_AddsThemToTheEmptyArray()
        {
            Assert.That(TestAssemblyUpdater.WithMissingReferences("{ \"references\": [] }", new[] { "MyWorld.Runtime", "MyWorld.Audio" }),
                Is.EqualTo("{ \"references\": [\"MyWorld.Runtime\", \"MyWorld.Audio\"] }"));
        }

        [Test]
        public void WithMissingReferences_SpacesInsideTheBrackets_KeepsThem()
        {
            Assert.That(TestAssemblyUpdater.WithMissingReferences("{ \"references\": [ \"Mine\" ] }", new[] { "MyWorld.Runtime" }),
                Is.EqualTo("{ \"references\": [ \"Mine\", \"MyWorld.Runtime\" ] }"));
        }

        [Test]
        public void WithMissingReferences_ReferencesByGuid_AddsTheMissingOnesByGuid()
        {
            // Arrange
            string json = $"{{ \"references\": [\"GUID:{GuidOf("Tsvrc.UdonTestKit")}\"] }}";

            // Act
            string updated = TestAssemblyUpdater.WithMissingReferences(json, new[] { "Tsvrc.UdonTestKit", "Tsvrc.UdonTestKit.UdonSpies" });

            // Assert
            Assert.That(updated, Is.EqualTo($"{{ \"references\": [\"GUID:{GuidOf("Tsvrc.UdonTestKit")}\", \"GUID:{GuidOf("Tsvrc.UdonTestKit.UdonSpies")}\"] }}"));
        }

        // Unity names an assembly by the name in its assembly definition, which the file name needn't match.
        [Test]
        public void WithMissingReferences_ReferenceByGuidToADefinitionFileOfAnotherName_FindsItsAssemblyByItsName()
        {
            // Arrange
            string assembly = CompilationPipeline.GetAssemblies(AssembliesType.Editor).Select(candidate => candidate.name)
                .First(name => CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(name) is string path
                    && Path.GetFileNameWithoutExtension(path) != name);
            string json = $"{{ \"references\": [\"GUID:{GuidOf(assembly)}\"] }}";

            // Act and assert
            Assert.That(TestAssemblyUpdater.WithMissingReferences(json, new[] { assembly }), Is.EqualTo(json));
        }

        [Test]
        public void WithMissingReferences_ReferenceByGuidToNoFile_KeepsItAndAddsTheMissingOnesByGuid()
        {
            // Arrange
            const string unknown = "GUID:0123456789abcdef0123456789abcdef";

            // Act
            string updated = TestAssemblyUpdater.WithMissingReferences($"{{ \"references\": [\"{unknown}\"] }}", new[] { "Tsvrc.UdonTestKit" });

            // Assert
            Assert.That(updated, Is.EqualTo($"{{ \"references\": [\"{unknown}\", \"GUID:{GuidOf("Tsvrc.UdonTestKit")}\"] }}"));
        }

        [Test]
        public void WithMissingReferences_NothingMissing_LeavesTheTextAsItIs()
        {
            // Arrange
            const string json = "{\n    \"references\": [\"MyWorld.Runtime\"]\n}";

            // Act and assert
            Assert.That(TestAssemblyUpdater.WithMissingReferences(json, new[] { "MyWorld.Runtime" }), Is.EqualTo(json));
        }

        [Test]
        public void WithMissingReferences_NothingToAddAndNoReferencesArray_LeavesTheTextAsItIs()
        {
            // Arrange
            const string json = "{\n    \"name\": \"A\"\n}";

            // Act and assert
            Assert.That(TestAssemblyUpdater.WithMissingReferences(json, new string[0]), Is.EqualTo(json));
        }

        [Test]
        public void WithMissingReferences_NoReferencesArray_AddsOne()
        {
            Assert.That(TestAssemblyUpdater.WithMissingReferences("{\n    \"name\": \"A\"\n}", new[] { "MyWorld.Runtime" }),
                Is.EqualTo("{\n    \"references\": [\"MyWorld.Runtime\"],\n    \"name\": \"A\"\n}"));
        }

        private static string GuidOf(string assembly) =>
            AssetDatabase.AssetPathToGUID(CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assembly));
    }
}

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Compilation;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    public class TestAssemblyUpdaterTests
    {
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
            Assert.That(TestAssemblyUpdater.WithMissingReferences("{ \"references\": [] }", new[] { "MyWorld.Runtime" }),
                Is.EqualTo("{ \"references\": [\"MyWorld.Runtime\"] }"));
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

        [Test]
        public void WithMissingReferences_NothingMissing_LeavesTheTextAsItIs()
        {
            // Arrange
            const string json = "{\n    \"references\": [\"MyWorld.Runtime\"]\n}";

            // Act and assert
            Assert.That(TestAssemblyUpdater.WithMissingReferences(json, new[] { "MyWorld.Runtime" }), Is.EqualTo(json));
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UdonTestKit.Editor.Tests.Solutions
{
    // Solution: TestAssemblyCreator writes test assemblies with every reference a test needs, so a
    // world doesn't assemble that list by hand. Files are written to a temporary folder outside
    // Assets, so nothing here is imported or compiled. That the created assemblies compile and
    // their example tests pass in a fresh project is checked in FINDINGS.md (F10).
    public class TestAssemblyCreatorTests
    {
        private const string KitTestsFolder = "Assets/udon-test-kit/Tests";

        private string _parent;

        [SetUp]
        public void SetUp()
        {
            _parent = Path.Combine(Path.GetTempPath(), "UdonTestKit.TestAssemblyCreatorTests");
            if (Directory.Exists(_parent))
                Directory.Delete(_parent, recursive: true);
            Directory.CreateDirectory(_parent);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_parent, recursive: true);
            Selection.activeObject = null;
        }

        [Test]
        public void Create_EmptyFolder_WritesBothAssembliesWithExamples()
        {
            // Act
            string tests = TestAssemblyCreator.Create(_parent, "MyWorld", new string[0]);

            // Assert
            Assert.That(File.Exists(Path.Combine(tests, "EditMode", "MyWorld.Tests.EditMode.asmdef")));
            Assert.That(File.Exists(Path.Combine(tests, "EditMode", "ExampleEditModeTests.cs")));
            Assert.That(File.Exists(Path.Combine(tests, "PlayMode", "MyWorld.Tests.PlayMode.asmdef")));
            Assert.That(File.Exists(Path.Combine(tests, "PlayMode", "ExamplePlayModeTests.cs")));
        }

        [Test]
        public void Create_TestsFolderExists_CreatesANumberedOne()
        {
            // Arrange
            Directory.CreateDirectory(Path.Combine(_parent, "Tests"));

            // Act
            string tests = TestAssemblyCreator.Create(_parent, "MyWorld", new string[0]);

            // Assert
            Assert.That(Path.GetFileName(tests), Is.EqualTo("Tests 1"));
        }

        [Test]
        public void AssemblyDefinitionJson_EditMode_IsEditorOnlyAndReferencesTheEditorRunner()
        {
            // Act
            AssemblyDefinition definition = Parse(TestAssemblyCreator.AssemblyDefinitionJson("A.Tests.EditMode", editorOnly: true, new string[0]));

            // Assert
            Assert.That(definition.name, Is.EqualTo("A.Tests.EditMode"));
            Assert.That(definition.includePlatforms, Is.EqualTo(new[] { "Editor" }));
            Assert.That(definition.references, Has.Member("UnityEditor.TestRunner"));
            Assert.That(definition.defineConstraints, Is.EqualTo(new[] { "UNITY_INCLUDE_TESTS" }));
        }

        [Test]
        public void AssemblyDefinitionJson_PlayMode_TargetsEveryPlatformWithoutTheEditorRunner()
        {
            // Act
            AssemblyDefinition definition = Parse(TestAssemblyCreator.AssemblyDefinitionJson("A.Tests.PlayMode", editorOnly: false, new string[0]));

            // Assert
            Assert.That(definition.includePlatforms, Is.Empty);
            Assert.That(definition.references, Has.No.Member("UnityEditor.TestRunner"));
        }

        [Test]
        public void AssemblyDefinitionJson_WorldAssemblies_AreReferencedOnce()
        {
            // Act
            AssemblyDefinition definition = Parse(TestAssemblyCreator.AssemblyDefinitionJson("A.Tests.PlayMode", editorOnly: false,
                new[] { "MyWorld.Runtime", "UdonTestKit" }));

            // Assert
            Assert.That(definition.references, Has.Member("MyWorld.Runtime"));
            Assert.That(definition.references.Count(reference => reference == "UdonTestKit"), Is.EqualTo(1));
        }

        [Test]
        public void SharedReferences_EachIsAnAssemblyInThisProject()
        {
            // Arrange
            HashSet<string> assemblies = new HashSet<string>(CompilationPipeline.GetAssemblies(AssembliesType.Editor).Select(assembly => assembly.name));

            // Act and Assert
            Assert.That(TestAssemblyCreator.SharedReferences.Append("UnityEditor.TestRunner"), Is.SubsetOf(assemblies));
        }

        [Test]
        public void PrecompiledReferences_EachIsADllInThisProject()
        {
            // Arrange
            IEnumerable<string> dlls = CompilationPipeline.GetPrecompiledAssemblyPaths(CompilationPipeline.PrecompiledAssemblySources.All)
                .Select(Path.GetFileName);

            // Act and Assert
            Assert.That(TestAssemblyCreator.PrecompiledReferences, Is.SubsetOf(dlls));
        }

        [Test]
        public void WorldAssemblyNames_InThisProject_LeavesOutTestAssembliesAndTheKit()
        {
            // Act
            List<string> world = TestAssemblyCreator.WorldAssemblyNames().ToList();

            // Assert
            Assert.That(world, Has.None.StartsWith("UdonTestKit"));
        }

        [Test]
        public void SelectedFolder_NothingSelected_IsAssets()
        {
            // Arrange
            Selection.activeObject = null;

            // Act and Assert
            Assert.That(TestAssemblyCreator.SelectedFolder(), Is.EqualTo("Assets"));
        }

        [Test]
        public void SelectedFolder_FolderSelected_IsThatFolder()
        {
            // Arrange
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(KitTestsFolder);
            Assume.That(Selection.activeObject, Is.Not.Null, $"{KitTestsFolder} isn't in this project.");

            // Act and Assert
            Assert.That(TestAssemblyCreator.SelectedFolder(), Is.EqualTo(KitTestsFolder));
        }

        [Test]
        public void SelectedFolder_FileSelected_IsTheFileFolder()
        {
            // Arrange
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(KitTestsFolder + "/Editor/UdonTestKit.Editor.Tests.asmdef");
            Assume.That(Selection.activeObject, Is.Not.Null, "The kit's Edit Mode assembly definition isn't in this project.");

            // Act and Assert
            Assert.That(TestAssemblyCreator.SelectedFolder(), Is.EqualTo(KitTestsFolder + "/Editor"));
        }

        [Test]
        public void UniquePrefix_NoTestAssembliesYet_KeepsThePrefix()
        {
            Assert.That(TestAssemblyCreator.UniquePrefix("MyWorld", new[] { "MyWorld.Runtime" }), Is.EqualTo("MyWorld"));
        }

        [Test]
        public void UniquePrefix_TestAssembliesExist_NumbersThePrefix()
        {
            // Arrange
            var existing = new[] { "MyWorld.Tests.EditMode", "MyWorld.Tests.PlayMode", "MyWorld2.Tests.PlayMode" };

            // Act
            string prefix = TestAssemblyCreator.UniquePrefix("MyWorld", existing);

            // Assert
            Assert.That(prefix, Is.EqualTo("MyWorld3"));
        }

        [TestCase("My World 2", "MyWorld2")]
        [TestCase("2 Fast", "World")]
        [TestCase("!!!", "World")]
        public void AssemblyPrefix_ProductName_KeepsOnlyLettersAndDigits(string productName, string expected)
        {
            Assert.That(TestAssemblyCreator.AssemblyPrefix(productName), Is.EqualTo(expected));
        }

        private static AssemblyDefinition Parse(string json)
        {
            return JsonUtility.FromJson<AssemblyDefinition>(json);
        }

        [System.Serializable]
        private class AssemblyDefinition
        {
            public string name;
            public string[] references;
            public string[] includePlatforms;
            public string[] precompiledReferences;
            public string[] defineConstraints;
        }
    }
}

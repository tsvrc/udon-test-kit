using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tsvrc.TestKit.Editor.Tests
{
    public class TestAssemblyCreatorTests
    {
        // Created by the selection tests, so they don't depend on where the kit is installed.
        private const string SelectionFolder = "Assets/TestKitSelectionTests";
        private const string SelectionFile = SelectionFolder + "/Selected.txt";

        private string _parent;

        [SetUp]
        public void SetUp()
        {
            _parent = Path.Combine(Path.GetTempPath(), "Tsvrc.TestKit.TestAssemblyCreatorTests");
            if (Directory.Exists(_parent))
            {
                Directory.Delete(_parent, recursive: true);
            }
            Directory.CreateDirectory(_parent);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_parent, recursive: true);
            Selection.activeObject = null;
            AssetDatabase.DeleteAsset(SelectionFolder);
        }

        [Test]
        public void Create_EmptyFolder_WritesTheThreeAssembliesWithExamples()
        {
            // Act
            string tests = TestAssemblyCreator.Create(_parent, "MyWorld", new string[0]);

            // Assert
            Assert.That(File.Exists(Path.Combine(tests, "EditMode", "MyWorld.Tests.EditMode.asmdef")));
            Assert.That(File.Exists(Path.Combine(tests, "EditMode", "ExampleEditModeTests.cs")));
            Assert.That(File.Exists(Path.Combine(tests, "PlayMode", "MyWorld.Tests.PlayMode.asmdef")));
            Assert.That(File.Exists(Path.Combine(tests, "PlayMode", "ExamplePlayModeTests.cs")));
            Assert.That(File.Exists(Path.Combine(tests, "Doubles", "MyWorld.Tests.Doubles.asmdef")));
            Assert.That(File.Exists(Path.Combine(tests, "Doubles", "ExampleSpy.cs")));
        }

        [TestCase("EditMode", "MyWorld.Tests.EditMode.asmdef")]
        [TestCase("PlayMode", "MyWorld.Tests.PlayMode.asmdef")]
        public void Create_EmptyFolder_TestAssemblyReferencesTheDoubles(string folder, string assemblyDefinition)
        {
            // Act
            string tests = TestAssemblyCreator.Create(_parent, "MyWorld", new[] { "MyWorld.Runtime" });

            // Assert
            AssemblyDefinition definition = Parse(File.ReadAllText(Path.Combine(tests, folder, assemblyDefinition)));
            Assert.That(definition.references, Has.Member("MyWorld.Tests.Doubles").And.Member("MyWorld.Runtime"));
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
            AssemblyDefinition definition = Parse(TestAssemblyCreator.AssemblyDefinitionJson("A.Tests.EditMode",
                TestAssemblyCreator.AssemblyKind.EditModeTests, new string[0]));

            // Assert
            Assert.That(definition.name, Is.EqualTo("A.Tests.EditMode"));
            Assert.That(definition.includePlatforms, Is.EqualTo(new[] { "Editor" }));
            Assert.That(definition.references, Has.Member("UnityEditor.TestRunner"));
            Assert.That(definition.precompiledReferences, Has.Member("nunit.framework.dll"));
            Assert.That(definition.defineConstraints, Is.EqualTo(new[] { "UNITY_INCLUDE_TESTS" }));
        }

        [Test]
        public void AssemblyDefinitionJson_PlayMode_TargetsEveryPlatformWithoutTheEditorRunner()
        {
            // Act
            AssemblyDefinition definition = Parse(TestAssemblyCreator.AssemblyDefinitionJson("A.Tests.PlayMode",
                TestAssemblyCreator.AssemblyKind.PlayModeTests, new string[0]));

            // Assert
            Assert.That(definition.includePlatforms, Is.Empty);
            Assert.That(definition.references, Has.Member("UnityEngine.TestRunner").And.No.Member("UnityEditor.TestRunner"));
            Assert.That(definition.precompiledReferences, Has.Member("nunit.framework.dll"));
        }

        [Test]
        public void AssemblyDefinitionJson_Doubles_TargetsEveryPlatformWithoutTheTestRunnerOrNUnit()
        {
            // Act
            AssemblyDefinition definition = Parse(TestAssemblyCreator.AssemblyDefinitionJson("A.Tests.Doubles",
                TestAssemblyCreator.AssemblyKind.Doubles, new string[0]));

            // Assert
            Assert.That(definition.includePlatforms, Is.Empty);
            Assert.That(definition.references, Has.Member("Tsvrc.TestKit").And.None.Contains("TestRunner"));
            Assert.That(definition.precompiledReferences, Has.No.Member("nunit.framework.dll"));
            Assert.That(definition.defineConstraints, Is.EqualTo(new[] { "UNITY_INCLUDE_TESTS" }));
        }

        [Test]
        public void AssemblyDefinitionJson_WorldAssemblies_AreReferencedOnce()
        {
            // Act
            AssemblyDefinition definition = Parse(TestAssemblyCreator.AssemblyDefinitionJson("A.Tests.PlayMode",
                TestAssemblyCreator.AssemblyKind.PlayModeTests, new[] { "MyWorld.Runtime", "Tsvrc.TestKit" }));

            // Assert
            Assert.That(definition.references, Has.Member("MyWorld.Runtime"));
            Assert.That(definition.references.Count(reference => reference == "Tsvrc.TestKit"), Is.EqualTo(1));
        }

        [Test]
        public void KitReferences_EachIsAnAssemblyInThisProject()
        {
            // Arrange
            HashSet<string> assemblies = new HashSet<string>(CompilationPipeline.GetAssemblies(AssembliesType.Editor).Select(assembly => assembly.name));

            // Act and Assert
            Assert.That(TestAssemblyCreator.KitReferences.Append("UnityEngine.TestRunner").Append("UnityEditor.TestRunner"), Is.SubsetOf(assemblies));
        }

        [Test]
        public void SdkDlls_EachIsADllInThisProject()
        {
            // Arrange
            IEnumerable<string> dlls = CompilationPipeline.GetPrecompiledAssemblyPaths(CompilationPipeline.PrecompiledAssemblySources.All)
                .Select(Path.GetFileName);

            // Act and Assert
            Assert.That(TestAssemblyCreator.SdkDlls, Is.SubsetOf(dlls));
        }

        [Test]
        public void WorldAssemblyNames_InThisProject_LeavesOutTestAssembliesAndTheKit()
        {
            // Act
            List<string> world = TestAssemblyCreator.WorldAssemblyNames().ToList();

            // Assert
            Assert.That(world, Has.None.StartsWith("Tsvrc.TestKit"));
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
            CreateSelectionFolder();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(SelectionFolder);

            // Act and Assert
            Assert.That(TestAssemblyCreator.SelectedFolder(), Is.EqualTo(SelectionFolder));
        }

        [Test]
        public void SelectedFolder_FileSelected_IsTheFileFolder()
        {
            // Arrange
            CreateSelectionFolder();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(SelectionFile);

            // Act and Assert
            Assert.That(TestAssemblyCreator.SelectedFolder(), Is.EqualTo(SelectionFolder));
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

        [Test]
        public void UniquePrefix_OnlyTheDoublesAssemblyExists_NumbersThePrefix()
        {
            Assert.That(TestAssemblyCreator.UniquePrefix("MyWorld", new[] { "MyWorld.Tests.Doubles" }), Is.EqualTo("MyWorld2"));
        }

        [TestCase("My World 2", "MyWorld2")]
        [TestCase("2 Fast", "World")]
        [TestCase("!!!", "World")]
        public void AssemblyPrefix_ProductName_KeepsOnlyLettersAndDigits(string productName, string expected)
        {
            Assert.That(TestAssemblyCreator.AssemblyPrefix(productName), Is.EqualTo(expected));
        }

        private static void CreateSelectionFolder()
        {
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(SelectionFolder));
            File.WriteAllText(SelectionFile, "");
            AssetDatabase.ImportAsset(SelectionFile);
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

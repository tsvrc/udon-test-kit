using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    public class TestAssemblyCreatorTests
    {
        // Created by the selection tests, so they don't depend on where the kit is installed.
        private const string SelectionFolder = "Assets/UdonTestKitSelectionTests";
        private const string SelectionFile = SelectionFolder + "/Selected.txt";
        private const string EditorFolderParent = "Assets/UdonTestKitEditorFolderTests";
        private const string EditorFolderScript = EditorFolderParent + "/Editor/UdonTestKitEditorFolderTests.cs";

        private string _parent;

        [SetUp]
        public void SetUp()
        {
            _parent = Path.GetFullPath(FileUtil.GetUniqueTempPathInProject());
            Directory.CreateDirectory(_parent);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_parent, recursive: true);
            Selection.activeObject = null;
            AssetDatabase.DeleteAsset(SelectionFolder);
            AssetDatabase.DeleteAsset(EditorFolderParent);
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

        // Code in a created assembly can use every public member of the kit, so each assembly such a
        // member takes, returns or derives from is a reference or a precompiled reference of it.
        [TestCase("EditMode", "MyWorld.Tests.EditMode.asmdef")]
        [TestCase("PlayMode", "MyWorld.Tests.PlayMode.asmdef")]
        [TestCase("Doubles", "MyWorld.Tests.Doubles.asmdef")]
        public void Create_EmptyFolder_AssemblyReferencesEachAssemblyThePublicKitApiUses(string folder, string assemblyDefinition)
        {
            // Act
            string tests = TestAssemblyCreator.Create(_parent, "MyWorld", new string[0]);

            // Assert
            AssemblyDefinition definition = Parse(File.ReadAllText(Path.Combine(tests, folder, assemblyDefinition)));
            Assert.That(definition.references.Concat(definition.precompiledReferences.Select(Path.GetFileNameWithoutExtension)),
                Is.SupersetOf(AssembliesThePublicKitApiUses()));
        }

        // The warning for scripts in Assembly-CSharp says Edit Mode tests for them can go in an Editor
        // folder without an assembly definition. Unity works out an assembly's references from the
        // scripts it imported, before any compile.
        [Test]
        public void AssemblyCSharpWarning_EditorFolderWithoutAnAssemblyDefinition_ReferencesTheKitNUnitAndAssemblyCSharp()
        {
            // Arrange
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(EditorFolderParent));
            AssetDatabase.CreateFolder(EditorFolderParent, "Editor");
            File.WriteAllText(EditorFolderParent + "/UdonTestKitLooseScript.cs", "public class UdonTestKitLooseScript { }");
            File.WriteAllText(EditorFolderScript, "public class UdonTestKitEditorFolderTests { }");
            AssetDatabase.ImportAsset(EditorFolderParent + "/UdonTestKitLooseScript.cs");
            AssetDatabase.ImportAsset(EditorFolderScript);

            // Act
            Assembly editorFolder = CompilationPipeline.GetAssemblies(AssembliesType.Editor)
                .Single(assembly => assembly.sourceFiles.Contains(EditorFolderScript));

            // Assert
            Assert.That(editorFolder.assemblyReferences.Select(assembly => assembly.name),
                Is.SupersetOf(new[] { "Assembly-CSharp", "Tsvrc.UdonTestKit", "Tsvrc.UdonTestKit.UdonSpies" }));
            Assert.That(editorFolder.compiledAssemblyReferences.Select(Path.GetFileName), Has.Member("nunit.framework.dll"));
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
            Assert.That(definition.references, Has.Member("Tsvrc.UdonTestKit").And.None.Contains("TestRunner"));
            Assert.That(definition.precompiledReferences, Has.No.Member("nunit.framework.dll"));
            Assert.That(definition.defineConstraints, Is.EqualTo(new[] { "UNITY_INCLUDE_TESTS" }));
        }

        [Test]
        public void AssemblyDefinitionJson_WorldAssemblies_AreReferencedOnce()
        {
            // Act
            AssemblyDefinition definition = Parse(TestAssemblyCreator.AssemblyDefinitionJson("A.Tests.PlayMode",
                TestAssemblyCreator.AssemblyKind.PlayModeTests, new[] { "MyWorld.Runtime", "Tsvrc.UdonTestKit" }));

            // Assert
            Assert.That(definition.references, Has.Member("MyWorld.Runtime"));
            Assert.That(definition.references.Count(reference => reference == "Tsvrc.UdonTestKit"), Is.EqualTo(1));
        }

        // Unity puts a script it creates in a folder into the root namespace of that folder's assembly.
        [Test]
        public void AssemblyDefinitionJson_AnyKind_HasNoRootNamespaceAndNoUnsafeCode()
        {
            // Act
            AssemblyDefinition definition = Parse(TestAssemblyCreator.AssemblyDefinitionJson("A.Tests.PlayMode",
                TestAssemblyCreator.AssemblyKind.PlayModeTests, new string[0]));

            // Assert
            Assert.That(definition.rootNamespace, Is.Null.Or.Empty);
            Assert.That(definition.allowUnsafeCode, Is.False);
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
            List<string> world = TestAssemblyCreator.WorldAssemblyNames(CompilationPipeline.GetAssemblies(AssembliesType.Editor),
                CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)).ToList();

            // Assert
            Assert.That(world, Has.None.StartsWith("Tsvrc.UdonTestKit"));
        }

        [Test]
        public void WorldAssemblyNames_PlayerAssemblies_ListsOnlyTheWorldsOwnUnderAssets()
        {
            // Arrange
            Assembly world = CompiledAssembly("MyWorld.Runtime", "Assets/MyWorld/Door.cs");
            Assembly tests = CompiledAssembly("MyWorld.Tests", "Assets/Tests/DoorTests.cs", "Library/nunit.framework.dll");
            Assembly package = CompiledAssembly("VRC.SDK3", "Packages/com.vrchat.worlds/Runtime/Sdk.cs");
            Assembly assemblyCSharp = CompiledAssembly("Assembly-CSharp", "Assets/Loose.cs");
            Assembly audio = CompiledAssembly("MyWorld.Audio", "Assets/MyWorld/Audio/Music.cs");

            // Act
            IEnumerable<string> names = TestAssemblyCreator.WorldAssemblyNames(new[] { tests }, new[] { world, tests, package, assemblyCSharp, audio });

            // Assert
            Assert.That(names, Is.EqualTo(new[] { "MyWorld.Audio", "MyWorld.Runtime" }));
        }

        [Test]
        public void CreateAndImport_FolderOutsideAssets_ReturnsTheTestsFolderAndLogsIt()
        {
            // Arrange
            string expected = Path.Combine(_parent, "Tests").Replace('\\', '/');
            LogAssert.Expect(LogType.Log, $"[UdonTestKit] Created test assemblies in {expected}.");

            // Act
            string tests = TestAssemblyCreator.CreateAndImport(_parent, new Assembly[0], new Assembly[0]);

            // Assert
            Assert.That(tests, Is.EqualTo(expected));
        }

        [Test]
        public void CreateAndImport_TestAssembliesWithTheProductNameExist_NumbersTheNewOnes()
        {
            // Arrange
            string prefix = TestAssemblyCreator.AssemblyPrefix(Application.productName);
            Assembly existing = CompiledAssembly(prefix + TestAssemblyCreator.EditModeSuffix, "Assets/Tests/EditMode/Tests.cs");

            // Act
            string tests = TestAssemblyCreator.CreateAndImport(_parent, new[] { existing }, new Assembly[0]);

            // Assert
            Assert.That(File.Exists(Path.Combine(tests, "EditMode", prefix + "2" + TestAssemblyCreator.EditModeSuffix + ".asmdef")));
        }

        [Test]
        public void CreateAndImport_WorldAssemblyInThePlayerBuild_IsReferenced()
        {
            // Arrange
            Assembly world = CompiledAssembly("MyWorld.Runtime", "Assets/MyWorld/Door.cs");

            // Act
            string tests = TestAssemblyCreator.CreateAndImport(_parent, new Assembly[0], new[] { world });

            // Assert
            AssemblyDefinition definition = Parse(File.ReadAllText(Directory.GetFiles(Path.Combine(tests, "PlayMode"), "*.asmdef").Single()));
            Assert.That(definition.references, Has.Member("MyWorld.Runtime"));
        }

        [TestCase("Assembly-CSharp", "Assets/Loose.cs", ExpectedResult = true)]
        [TestCase("Assembly-CSharp", "Packages/com.example/Loose.cs", ExpectedResult = false)]
        [TestCase("MyWorld.Runtime", "Assets/MyWorld/Door.cs", ExpectedResult = false)]
        public bool CreateAndImport_PlayerAssembly_WarnsOnlyForAssemblyCSharpScriptsUnderAssets(string assembly, string script)
        {
            // Arrange
            var warnings = new List<string>();
            Application.LogCallback record = (message, stackTrace, type) =>
            {
                if (type == LogType.Warning)
                {
                    warnings.Add(message);
                }
            };
            Application.logMessageReceived += record;

            // Act
            try
            {
                TestAssemblyCreator.CreateAndImport(_parent, new Assembly[0], new[] { CompiledAssembly(assembly, script) });
            }
            finally
            {
                Application.logMessageReceived -= record;
            }

            // Assert
            return warnings.Contains(TestAssemblyCreator.AssemblyCSharpWarning);
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
        public void SelectedFolder_FileOutsideAssetsSelected_IsAssets()
        {
            // Arrange
            Object packageFile = AssetDatabase.LoadAssetAtPath<Object>("Packages/com.unity.test-framework/package.json");
            Assert.That(packageFile, Is.Not.Null, "No package file to select.");
            Selection.activeObject = packageFile;

            // Act and Assert
            Assert.That(TestAssemblyCreator.SelectedFolder(), Is.EqualTo("Assets"));
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

        // The examples Create writes are also scripts under Tests/, which compile and run with the kit
        // tests, so the examples a world gets work.
        [Test]
        public void ExampleSpy_WrittenByCreate_IsATestedScript()
        {
            Assert.That(SourceOf(typeof(ExampleSpy)), Is.EqualTo(Lines(TestAssemblyCreator.ExampleSpy)));
        }

        [Test]
        public void ExampleEditModeTests_WrittenByCreate_IsATestedScript()
        {
            Assert.That(SourceOf(typeof(ExampleEditModeTests)), Is.EqualTo(Lines(TestAssemblyCreator.ExampleEditModeTests)));
        }

        [Test]
        public void ExamplePlayModeTests_WrittenByCreate_IsATestedScript()
        {
            Assert.That(SourceOf(typeof(ExamplePlayModeTests)), Is.EqualTo(Lines(TestAssemblyCreator.ExamplePlayModeTests)));
        }

        private static string SourceOf(System.Type script)
        {
            MonoScript source = AssetDatabase.FindAssets(script.Name + " t:MonoScript")
                .Select(guid => AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid)))
                .Single(candidate => candidate.GetClass() == script);
            return Lines(source.text);
        }

        // A checkout on Windows can turn the line endings of a file, and of the examples in
        // TestAssemblyCreator.cs, into CRLF.
        private static string Lines(string text) => text.Replace("\r\n", "\n");

        // The assemblies the public and protected members of the kit's types take, return or derive
        // from, but the ones that ship with the Unity Editor, which every assembly references.
        private static IEnumerable<string> AssembliesThePublicKitApiUses()
        {
            const System.Reflection.BindingFlags members = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly;
            string editor = Path.GetFullPath(EditorApplication.applicationContentsPath);
            return new[] { typeof(CallLog).Assembly, typeof(UdonCallLog).Assembly }
                .SelectMany(assembly => assembly.GetExportedTypes())
                .SelectMany(type => new[] { type, type.BaseType }
                    .Concat(type.GetMethods(members).Where(IsVisible).SelectMany(method =>
                        method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType)))
                    .Concat(type.GetProperties(members).Where(property => IsVisible(property.GetMethod)).Select(property => property.PropertyType))
                    .Concat(type.GetFields(members).Where(field => field.IsPublic || field.IsFamily).Select(field => field.FieldType)))
                .Where(type => type != null)
                .Select(type => type.Assembly)
                .Where(assembly => !assembly.IsDynamic && !Path.GetFullPath(assembly.Location).StartsWith(editor))
                .Select(assembly => assembly.GetName().Name)
                .Distinct();
        }

        private static bool IsVisible(System.Reflection.MethodBase method) => method != null && (method.IsPublic || method.IsFamily);

        private static Assembly CompiledAssembly(string name, string script, params string[] compiledReferences)
        {
            return new Assembly(name, $"Library/ScriptAssemblies/{name}.dll", new[] { script }, new string[0], new Assembly[0],
                compiledReferences, AssemblyFlags.None);
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
            public string rootNamespace;
            public string[] references;
            public string[] includePlatforms;
            public bool allowUnsafeCode;
            public string[] precompiledReferences;
            public string[] defineConstraints;
        }
    }
}

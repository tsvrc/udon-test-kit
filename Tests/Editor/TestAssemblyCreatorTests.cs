using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;
using Tsvrc.UdonTestKit.Tests;
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
        private const string PluginsFolder = "Assets/Plugins/UdonTestKitPluginsTests";
        private const string PluginsScript = PluginsFolder + "/UdonTestKitPluginsScript.cs";

        private string _parent;
        private bool _createdPlugins;

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
        }

        [UnityTearDown]
        public IEnumerator DeleteTheScripts()
        {
            string plugins = _createdPlugins ? "Assets/Plugins" : PluginsFolder;
            _createdPlugins = false;
            return ProjectScripts.DeleteFolders(EditorFolderParent, plugins);
        }

        [Test]
        public void Create_EmptyFolder_WritesTheFourAssembliesWithExamples()
        {
            // Act
            string tests = CreateTests();

            // Assert
            Assert.That(File.Exists(Path.Combine(tests, "EditMode", "MyWorld.Tests.EditMode.asmdef")));
            Assert.That(File.Exists(Path.Combine(tests, "EditMode", "ExampleEditModeTests.cs")));
            Assert.That(File.Exists(Path.Combine(tests, "PlayMode", "MyWorld.Tests.PlayMode.asmdef")));
            Assert.That(File.Exists(Path.Combine(tests, "PlayMode", "ExamplePlayModeTests.cs")));
            Assert.That(File.Exists(Path.Combine(tests, "Doubles", "MyWorld.Tests.Doubles.asmdef")));
            Assert.That(File.Exists(Path.Combine(tests, "Doubles", "ExampleSpy.cs")));
            Assert.That(File.Exists(Path.Combine(tests, "UdonDoubles", "MyWorld.Tests.UdonDoubles.asmdef")));
            Assert.That(File.Exists(Path.Combine(tests, "UdonDoubles", "ExampleUdonDouble.cs")));
        }

        [TestCase("EditMode", "ExampleEditModeTests.cs")]
        [TestCase("PlayMode", "ExamplePlayModeTests.cs")]
        [TestCase("Doubles", "ExampleSpy.cs")]
        [TestCase("UdonDoubles", "ExampleUdonDouble.cs")]
        public void Create_EmptyFolder_ExampleIsInTheNamespaceOfThePrefix(string folder, string example)
        {
            // Act
            string tests = CreateTests();

            // Assert
            Assert.That(Lines(File.ReadAllText(Path.Combine(tests, folder, example))), Does.Contain("namespace MyWorld.Tests\n"));
        }

        [TestCase("EditMode")]
        [TestCase("PlayMode")]
        public void Create_EmptyFolder_TestAssemblyReferencesBothDoubles(string folder)
        {
            // Act
            string tests = CreateTests();

            // Assert
            Assert.That(DefinitionIn(tests, folder).references, Has.Member("MyWorld.Tests.Doubles").And.Member("MyWorld.Tests.UdonDoubles"));
        }

        [TestCase("EditMode")]
        [TestCase("PlayMode")]
        [TestCase("Doubles")]
        [TestCase("UdonDoubles")]
        public void Create_TwoWorldAssemblies_AssemblyReferencesBoth(string folder)
        {
            // Act
            string tests = CreateTests(world: new[] { "MyWorld.Audio", "MyWorld.Runtime" });

            // Assert
            Assert.That(DefinitionIn(tests, folder).references, Has.Member("MyWorld.Audio").And.Member("MyWorld.Runtime"));
        }

        // Only the Edit Mode test assembly is Editor-only, as the world's Editor tooling is.
        [TestCase("EditMode", ExpectedResult = true)]
        [TestCase("PlayMode", ExpectedResult = false)]
        [TestCase("Doubles", ExpectedResult = false)]
        [TestCase("UdonDoubles", ExpectedResult = false)]
        public bool Create_EditorWorldAssembly_OnlyTheEditModeAssemblyReferencesIt(string folder)
        {
            // Act
            string tests = CreateTests(editorWorld: new[] { "MyWorld.Editor" });

            // Assert
            return DefinitionIn(tests, folder).references.Contains("MyWorld.Editor");
        }

        // Code in a created assembly can use every public member of the kit, so each assembly such a
        // member takes, returns or derives from is a reference or a precompiled reference of it.
        [TestCase("EditMode")]
        [TestCase("PlayMode")]
        [TestCase("Doubles")]
        public void Create_EmptyFolder_AssemblyReferencesEachAssemblyThePublicKitApiUses(string folder)
        {
            // Act
            AssemblyDefinition definition = DefinitionIn(CreateTests(), folder);

            // Assert
            Assert.That(AllReferencesOf(definition), Is.SupersetOf(AssembliesThePublicApiUses(typeof(CallLog).Assembly, typeof(UdonCallLog).Assembly)));
        }

        // An Udon double can use every public member of UdonCallLog, the one kit type in player builds.
        [Test]
        public void Create_EmptyFolder_UdonDoublesReferenceEachAssemblyThePublicApiOfUdonCallLogUses()
        {
            // Act
            AssemblyDefinition definition = DefinitionIn(CreateTests(), "UdonDoubles");

            // Assert
            Assert.That(AllReferencesOf(definition), Is.SupersetOf(AssembliesThePublicApiUses(typeof(UdonCallLog).Assembly)));
        }

        // The warning for scripts in Assembly-CSharp and Assembly-CSharp-firstpass says Edit Mode tests
        // for them can go in an Editor folder without an assembly definition. Unity works out an
        // assembly's references from the scripts it imported, before any compile.
        [Test]
        public void AssemblyCSharpWarning_EditorFolderWithoutAnAssemblyDefinition_ReferencesTheKitNUnitAndBothAssemblies()
        {
            // Arrange
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(EditorFolderParent));
            AssetDatabase.CreateFolder(EditorFolderParent, "Editor");
            File.WriteAllText(EditorFolderParent + "/UdonTestKitLooseScript.cs", "public class UdonTestKitLooseScript { }");
            File.WriteAllText(EditorFolderScript, "public class UdonTestKitEditorFolderTests { }");
            AssetDatabase.ImportAsset(EditorFolderParent + "/UdonTestKitLooseScript.cs");
            AssetDatabase.ImportAsset(EditorFolderScript);
            WritePluginsScript();

            // Act
            Assembly editorFolder = CompilationPipeline.GetAssemblies(AssembliesType.Editor)
                .Single(assembly => assembly.sourceFiles.Contains(EditorFolderScript));

            // Assert
            Assert.That(editorFolder.assemblyReferences.Select(assembly => assembly.name),
                Is.SupersetOf(new[] { "Assembly-CSharp", "Assembly-CSharp-firstpass", "Tsvrc.UdonTestKit", "Tsvrc.UdonTestKit.UdonSpies" }));
            Assert.That(editorFolder.compiledAssemblyReferences.Select(Path.GetFileName), Has.Member("nunit.framework.dll"));
            Assert.That(TestAssemblyCreator.AssemblyCSharpWarning, Is.EqualTo(
                "[UdonTestKit] Some scripts under Assets have no assembly definition, so they compile into Assembly-CSharp or " +
                "Assembly-CSharp-firstpass, which the created test assemblies can't reference. Edit Mode tests for them can go " +
                "in an Editor folder without an assembly definition. Play Mode tests need those scripts in an assembly definition."));
        }

        // A script in a Plugins folder without an assembly definition is one TestAssemblyCreator warns
        // about.
        [Test]
        public void GetAssemblies_ScriptInPluginsFolder_IsInAssemblyCSharpFirstpass()
        {
            // Arrange
            WritePluginsScript();

            // Act
            Assembly plugins = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                .Single(assembly => assembly.sourceFiles.Contains(PluginsScript));

            // Assert
            Assert.That(plugins.name, Is.EqualTo("Assembly-CSharp-firstpass"));
        }

        [Test]
        public void Create_TestsFolderExists_CreatesANumberedOne()
        {
            // Arrange
            Directory.CreateDirectory(Path.Combine(_parent, "Tests"));

            // Act
            string tests = CreateTests();

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
        public void AssemblyDefinitionJson_UdonDoubles_TargetsEveryPlatformInPlayerBuilds()
        {
            // Act
            AssemblyDefinition definition = Parse(TestAssemblyCreator.AssemblyDefinitionJson("A.Tests.UdonDoubles",
                TestAssemblyCreator.AssemblyKind.UdonDoubles, new string[0]));

            // Assert
            Assert.That(definition.includePlatforms, Is.Empty);
            Assert.That(definition.defineConstraints, Is.Empty);
            Assert.That(definition.references, Is.SupersetOf(new[] { "Tsvrc.UdonTestKit.UdonSpies", "UdonSharp.Runtime", "VRC.SDKBase", "VRC.Udon", "VRC.SDK3" }));
            Assert.That(definition.references, Has.No.Member("Tsvrc.UdonTestKit").And.None.Contains("TestRunner"));
            Assert.That(definition.precompiledReferences, Has.No.Member("nunit.framework.dll"));
        }

        // Player builds include each assembly the Udon doubles assembly references.
        [Test]
        public void UdonDoublesReferences_EachIsAPlayerAssemblyInThisProject()
        {
            // Arrange
            IEnumerable<string> player = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies).Select(assembly => assembly.name);

            // Act and assert
            Assert.That(TestAssemblyCreator.UdonDoublesReferences, Is.SubsetOf(player));
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
        public void KitReferencesOf_EachKind_IsAnAssemblyInThisProject()
        {
            // Arrange
            IEnumerable<string> references = TestAssemblyCreator.Kinds.SelectMany(TestAssemblyCreator.KitReferencesOf);
            IEnumerable<string> assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor).Select(assembly => assembly.name);

            // Act and assert
            Assert.That(references.Distinct(), Is.SubsetOf(assemblies));
        }

        [Test]
        public void SdkDlls_EachIsADllInThisProject()
        {
            // Arrange
            IEnumerable<string> dlls = CompilationPipeline.GetPrecompiledAssemblyPaths(CompilationPipeline.PrecompiledAssemblySources.All)
                .Select(Path.GetFileName);

            // Act and assert
            Assert.That(TestAssemblyCreator.SdkDlls, Is.SubsetOf(dlls));
        }

        [Test]
        public void WorldAssemblyNames_InThisProject_LeavesOutTestAssembliesAndTheKit()
        {
            // Act
            List<string> world = TestAssemblyCreator.WorldAssemblyNames(CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies),
                TestAssemblyCreator.IsTestAssembly).ToList();

            // Assert
            Assert.That(world, Has.None.StartsWith("Tsvrc.UdonTestKit"));
        }

        [Test]
        public void WorldAssemblyNames_PlayerAssemblies_ListsOnlyTheWorldsOwnUnderAssets()
        {
            // Arrange
            Assembly world = CompiledAssembly("MyWorld.Runtime", "Assets/MyWorld/Door.cs");
            Assembly tests = CompiledAssembly("MyWorld.Tests", "Assets/Tests/DoorTests.cs");
            Assembly package = CompiledAssembly("VRC.SDK3", "Packages/com.vrchat.worlds/Runtime/Sdk.cs");
            Assembly assemblyCSharp = CompiledAssembly("Assembly-CSharp", "Assets/Loose.cs");
            Assembly firstpass = CompiledAssembly("Assembly-CSharp-firstpass", "Assets/Plugins/Loose.cs");
            Assembly audio = CompiledAssembly("MyWorld.Audio", "Assets/MyWorld/Audio/Music.cs");
            Assembly udonDoubles = CompiledAssembly("MyWorld.Tests.UdonDoubles", "Assets/Tests/UdonDoubles/ExampleUdonDouble.cs");
            Assembly kit = CompiledAssembly("Tsvrc.UdonTestKit.UdonSpies", "Assets/udon-test-kit/Runtime/UdonSpies/UdonCallLog.cs");

            // Act
            IEnumerable<string> names = TestAssemblyCreator.WorldAssemblyNames(
                new[] { world, tests, package, assemblyCSharp, firstpass, audio, udonDoubles, kit }, name => name == "MyWorld.Tests");

            // Assert
            Assert.That(names, Is.EqualTo(new[] { "MyWorld.Audio", "MyWorld.Runtime" }));
        }

        [Test]
        public void EditorWorldAssemblyNames_EditorAssemblies_ListsOnlyTheWorldsOwnEditorOnlyOnesUnderAssets()
        {
            // Arrange
            Assembly tooling = CompiledAssembly("MyWorld.Editor", "Assets/MyWorld/Editor/Tool.cs", AssemblyFlags.EditorAssembly);
            Assembly world = CompiledAssembly("MyWorld.Runtime", "Assets/MyWorld/Door.cs");
            Assembly tests = CompiledAssembly("MyWorld.Tests.EditMode", "Assets/Tests/EditMode/DoorTests.cs", AssemblyFlags.EditorAssembly);
            Assembly package = CompiledAssembly("VRC.SDK3.Editor", "Packages/com.vrchat.worlds/Editor/Sdk.cs", AssemblyFlags.EditorAssembly);
            Assembly predefined = CompiledAssembly("Assembly-CSharp-Editor", "Assets/Editor/Loose.cs", AssemblyFlags.EditorAssembly);
            Assembly kit = CompiledAssembly("Tsvrc.UdonTestKit.Editor.Tests", "Assets/udon-test-kit/Tests/Editor/Tests.cs", AssemblyFlags.EditorAssembly);
            Assembly audioTooling = CompiledAssembly("MyWorld.Audio.Editor", "Assets/MyWorld/Audio/Editor/Mixer.cs", AssemblyFlags.EditorAssembly);

            // Act
            IEnumerable<string> names = TestAssemblyCreator.EditorWorldAssemblyNames(
                new[] { tooling, world, tests, package, predefined, kit, audioTooling }, name => name == "MyWorld.Tests.EditMode");

            // Assert
            Assert.That(names, Is.EqualTo(new[] { "MyWorld.Audio.Editor", "MyWorld.Editor" }));
        }

        [TestCase("Tsvrc.UdonTestKit.Editor.Tests", ExpectedResult = true)]
        [TestCase("Tsvrc.UdonTestKit.Tests", ExpectedResult = true)]
        [TestCase("Tsvrc.UdonTestKit.Tests.UdonDoubles", ExpectedResult = false)]
        [TestCase("Tsvrc.UdonTestKit.Tests.TestsOnlyDoubles", ExpectedResult = false)]
        [TestCase("Assembly-CSharp", ExpectedResult = false)]
        public bool IsTestAssembly_AssemblyInThisProject_IsTrueWhenItsDefinitionListsNUnit(string name)
        {
            return TestAssemblyCreator.IsTestAssembly(name);
        }

        // Neither folder is in the AssetDatabase, so nothing is written there even when Create goes on.
        [TestCase("Temp")]
        [TestCase("AssetsCopy")]
        public void Create_FolderOutsideAssets_ThrowsArgumentException(string folder)
        {
            var failure = Assert.Throws<System.ArgumentException>(() => TestAssemblyCreator.Create(folder));

            Assert.That(failure.Message, Does.StartWith($"{folder} isn't Assets or a folder under it, where the test assemblies go."));
        }

        [TestCase("Assembly-CSharp", "Assets/Loose.cs", ExpectedResult = true)]
        [TestCase("Assembly-CSharp-firstpass", "Assets/Plugins/Loose.cs", ExpectedResult = true)]
        [TestCase("Assembly-CSharp", "Packages/com.example/Loose.cs", ExpectedResult = false)]
        [TestCase("MyWorld.Runtime", "Assets/MyWorld/Door.cs", ExpectedResult = false)]
        public bool HasScriptsInPredefinedAssemblies_PlayerAssembly_IsTrueOnlyForTheirScriptsUnderAssets(string assembly, string script)
        {
            return TestAssemblyCreator.HasScriptsInPredefinedAssemblies(new[] { CompiledAssembly(assembly, script) });
        }

        [Test]
        public void SelectedFolder_NothingSelected_IsAssets()
        {
            // Arrange
            Selection.activeObject = null;

            // Act and assert
            Assert.That(TestAssemblyCreator.SelectedFolder(), Is.EqualTo("Assets"));
        }

        [Test]
        public void SelectedFolder_FolderSelected_IsThatFolder()
        {
            // Arrange
            CreateSelectionFolder();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(SelectionFolder);

            // Act and assert
            Assert.That(TestAssemblyCreator.SelectedFolder(), Is.EqualTo(SelectionFolder));
        }

        [Test]
        public void SelectedFolder_FileSelected_IsTheFileFolder()
        {
            // Arrange
            CreateSelectionFolder();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(SelectionFile);

            // Act and assert
            Assert.That(TestAssemblyCreator.SelectedFolder(), Is.EqualTo(SelectionFolder));
        }

        [Test]
        public void SelectedFolder_FileOutsideAssetsSelected_IsAssets()
        {
            // Arrange
            Object packageFile = AssetDatabase.LoadAssetAtPath<Object>("Packages/com.unity.test-framework/package.json");
            Assert.That(packageFile, Is.Not.Null, "No package file to select.");
            Selection.activeObject = packageFile;

            // Act and assert
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

        [TestCase("MyWorld.Tests.EditMode")]
        [TestCase("MyWorld.Tests.PlayMode")]
        [TestCase("MyWorld.Tests.Doubles")]
        [TestCase("MyWorld.Tests.UdonDoubles")]
        public void UniquePrefix_OnlyOneAssemblyOfASetExists_NumbersThePrefix(string existing)
        {
            Assert.That(TestAssemblyCreator.UniquePrefix("MyWorld", new[] { existing }), Is.EqualTo("MyWorld2"));
        }

        [TestCase("My World 2", "MyWorld2")]
        [TestCase("2 Fast", "World")]
        [TestCase("!!!", "World")]
        public void AssemblyPrefix_ProductName_KeepsOnlyLettersAndDigits(string productName, string expected)
        {
            Assert.That(TestAssemblyCreator.AssemblyPrefix(productName), Is.EqualTo(expected));
        }

        // The examples Create writes are also scripts under Tests/, in the namespace of the kit's own
        // test assemblies, which compile and run with the kit tests, so the examples a world gets work.
        [Test]
        public void ExampleSpy_WrittenByCreate_IsATestedScript()
        {
            Assert.That(SourceOf(typeof(ExampleSpy)), Is.EqualTo(KitCopy(TestAssemblyCreator.ExampleSpy)));
        }

        [Test]
        public void ExampleUdonDouble_WrittenByCreate_IsATestedScript()
        {
            Assert.That(SourceOf(typeof(ExampleUdonDouble)), Is.EqualTo(KitCopy(TestAssemblyCreator.ExampleUdonDouble)));
        }

        [Test]
        public void ExampleEditModeTests_WrittenByCreate_IsATestedScript()
        {
            Assert.That(SourceOf(typeof(ExampleEditModeTests)), Is.EqualTo(KitCopy(TestAssemblyCreator.ExampleEditModeTests)));
        }

        [Test]
        public void ExamplePlayModeTests_WrittenByCreate_IsATestedScript()
        {
            Assert.That(SourceOf(typeof(ExamplePlayModeTests)), Is.EqualTo(KitCopy(TestAssemblyCreator.ExamplePlayModeTests)));
        }

        private string CreateTests(string[] world = null, string[] editorWorld = null) =>
            TestAssemblyCreator.Create(_parent, "MyWorld", world ?? new string[0], editorWorld ?? new string[0]);

        // The assembly definition Create wrote into a folder of the set, named after it.
        private static AssemblyDefinition DefinitionIn(string tests, string folder) =>
            Parse(File.ReadAllText(Path.Combine(tests, folder, $"MyWorld.Tests.{folder}.asmdef")));

        private static IEnumerable<string> AllReferencesOf(AssemblyDefinition definition) =>
            definition.references.Concat(definition.precompiledReferences.Select(Path.GetFileNameWithoutExtension));

        private static string KitCopy(string example) => Lines(TestAssemblyCreator.Example(example, "Tsvrc.UdonTestKit"));

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

        // The assemblies the public and protected members of the types of kitAssemblies take, return or
        // derive from, but the ones that ship with the Unity Editor, which every assembly references.
        private static IEnumerable<string> AssembliesThePublicApiUses(params System.Reflection.Assembly[] kitAssemblies)
        {
            const System.Reflection.BindingFlags members = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly;
            string editor = Path.GetFullPath(EditorApplication.applicationContentsPath);
            return kitAssemblies
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

        private static Assembly CompiledAssembly(string name, string script, AssemblyFlags flags = AssemblyFlags.None)
        {
            return new Assembly(name, $"Library/ScriptAssemblies/{name}.dll", new[] { script }, new string[0], new Assembly[0], new string[0], flags);
        }

        private void WritePluginsScript()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Plugins"))
            {
                AssetDatabase.CreateFolder("Assets", "Plugins");
                _createdPlugins = true;
            }
            AssetDatabase.CreateFolder("Assets/Plugins", Path.GetFileName(PluginsFolder));
            File.WriteAllText(PluginsScript, "public class UdonTestKitPluginsScript { }");
            AssetDatabase.ImportAsset(PluginsScript);
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

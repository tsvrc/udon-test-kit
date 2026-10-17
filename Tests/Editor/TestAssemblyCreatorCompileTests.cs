using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.Udon;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    // Creates test assemblies in this project and compiles them, which reloads the domain. A test
    // resumes after a reload with its local variables reset, so it finds the assemblies again, and
    // keeps what it needs in SessionState. The classes a test writes carry a name of the run in
    // place of __RUN__, so an assembly an earlier run left compiled can't pass a test.
    public class TestAssemblyCreatorCompileTests
    {
        private const string RunKey = "Tsvrc.UdonTestKit.Editor.Tests.TestAssemblyCreatorCompileTests.Run";
        private const string LogsKey = "Tsvrc.UdonTestKit.Editor.Tests.TestAssemblyCreatorCompileTests.Logs";
        private const string CallsKey = "Tsvrc.UdonTestKit.Editor.Tests.TestAssemblyCreatorCompileTests.Calls";
        private const string UploadErrorsKey = "Tsvrc.UdonTestKit.Editor.Tests.TestAssemblyCreatorCompileTests.UploadErrors";
        private const string FolderKey = "Tsvrc.UdonTestKit.Editor.Tests.TestAssemblyCreatorCompileTests.Folder";
        private const string Parent = "Assets/UdonTestKitCreatorCompileTests";
        private const string Tests = Parent + "/Tests";
        // Compiles into Assembly-CSharp, which references every assembly that allows it.
        private const string LooseScript = Parent + "/UdonTestKitLooseScript.cs";
        private const string WorldAssembly = "UdonTestKitCreatorWorld";
        private const string WorldEditorAssembly = "UdonTestKitCreatorWorldEditor";
        private const string KitUsage = Tests + "/PlayMode/KitUsage.cs";
        private const string EditorRunnerUsage = Tests + "/EditMode/EditorRunnerUsage.cs";

        // Uses each public type of the kit, each SDK type its members take or return, and a type
        // from each other assembly the test assemblies reference. It only has to compile.
        private const string KitUsageSource = @"using System;
using System.Collections;
using Tsvrc.UdonTestKit;
using VRC.SDK3.ClientSim;
using VRC.SDK3.Components;
using VRC.SDK3.Dynamics.PhysBone;
using VRC.SDKBase;
using VRC.SDKBase.Validation;
using VRC.Udon;

public class KitSceneUsage : ClientSimTestBase
{
    public KitSceneUsage() : base(TestScene.Empty(""Usage""), TestSceneLifetime.PerFixture)
    {
    }
}

public class KitUsage__RUN__ : UdonTestBase
{
    private static readonly Type[] SdkTypes = { typeof(ClientSimMain), typeof(VRCSceneDescriptor), typeof(PhysBoneGrabHelper), typeof(WorldValidation) };

    public IEnumerator UseTheKit()
    {
        yield return Session.Start();
        VRCPlayerApi remote = Session.SpawnRemotePlayer(""Remote"");
        UdonBehaviour log = Udon.Create<UdonCallLog>(""Log"");
        Session.SetOwner(remote, log.gameObject);
        log.SendCustomEvent(nameof(UdonCallLog.Add));
        yield return UdonHost.ExpectUdonException(() => log.SendCustomEvent(""Throw""));
        using var calls = new CallLog();
        PrivateFieldAccess.SetField(calls.CreateSpy(), ""_calls"", UdonHost.Calls(log));
        if (TestRun.IsActive && BuiltWorld.FromWorldScene().HoldsProgramOf(typeof(UdonCallLog)))
        {
            UdonSharpUploadCompile.Run();
            ClientSimProjectSettings.Fix();
            TestAssemblyCreator.Create();
        }
    }
}
";

        // Uses the Editor test runner, which only the Edit Mode test assembly references.
        private const string EditorRunnerUsageSource = @"using UnityEditor.TestTools.TestRunner.Api;

public class EditorRunnerUsage__RUN__
{
    public TestRunnerApi Api;
}
";

        private static string Run => SessionState.GetString(RunKey, "");

        [TearDown]
        public void ClearTheSelectionAndSessionState()
        {
            Selection.activeObject = null;
            SessionState.EraseString(RunKey);
            SessionState.EraseString(LogsKey);
            SessionState.EraseString(CallsKey);
            SessionState.EraseString(UploadErrorsKey);
        }

        [UnityTearDown]
        public IEnumerator DeleteTheAssemblies()
        {
            string folder = SessionState.GetString(FolderKey, Parent);
            SessionState.EraseString(FolderKey);
            return ProjectScripts.DeleteFolders(folder);
        }

        [UnityTest]
        public IEnumerator MenuItem_FolderSelected_CompilesTestAssembliesThereThatUseTheKit()
        {
            // Arrange
            SessionState.SetString(RunKey, Guid.NewGuid().ToString("N"));
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(Parent));
            Write(LooseScript, "public class UdonTestKitLooseScript { }");
            Write($"{Parent}/World/{WorldAssembly}.asmdef", $"{{ \"name\": \"{WorldAssembly}\" }}");
            Write($"{Parent}/World/{WorldAssembly}.cs", $"public class {WorldAssembly} {{ }}");
            Write($"{Parent}/WorldEditor/{WorldEditorAssembly}.asmdef", $"{{ \"name\": \"{WorldEditorAssembly}\", \"includePlatforms\": [\"Editor\"] }}");
            Write($"{Parent}/WorldEditor/{WorldEditorAssembly}.cs", $"public class {WorldEditorAssembly} {{ }}");
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>(Parent);

            // Act
            SessionState.SetString(LogsKey, string.Join("\n", LogsOf(() => EditorApplication.ExecuteMenuItem(TestAssemblyCreator.MenuPath))));
            File.WriteAllText(KitUsage, KitUsageSource.Replace("__RUN__", Run));
            File.WriteAllText(EditorRunnerUsage, EditorRunnerUsageSource.Replace("__RUN__", Run));
            AssetDatabase.ImportAsset(KitUsage);
            AssetDatabase.ImportAsset(EditorRunnerUsage);
            yield return new WaitForDomainReload();

            // Assert
            Assembly[] editor = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
            Assembly editMode = AssemblyOf(editor, Tests + "/EditMode/ExampleEditModeTests.cs");
            Assembly playMode = AssemblyOf(editor, Tests + "/PlayMode/ExamplePlayModeTests.cs");
            Assembly doubles = AssemblyOf(editor, Tests + "/Doubles/ExampleSpy.cs");
            Assembly udonDoubles = AssemblyOf(editor, Tests + "/UdonDoubles/ExampleUdonDouble.cs");
            Assembly[] created = { editMode, playMode, doubles, udonDoubles };
            string[] createdNames = created.Select(assembly => assembly.name).ToArray();
            Assert.That(SessionState.GetString(LogsKey, "").Split('\n'),
                Has.Member($"[UdonTestKit] Created test assemblies in {Tests}.").And.Member(TestAssemblyCreator.AssemblyCSharpWarning));
            Assert.That(Type.GetType("KitUsage" + Run + ", " + playMode.name), Is.Not.Null);
            Assert.That(Type.GetType("EditorRunnerUsage" + Run + ", " + editMode.name), Is.Not.Null);
            Assert.That(CompilationPipeline.GetAssemblies(AssembliesType.Player).Select(assembly => assembly.name),
                Is.SupersetOf(new[] { playMode.name, doubles.name, udonDoubles.name }).And.No.Member(editMode.name));
            Assert.That(CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies).Select(assembly => assembly.name)
                .Intersect(createdNames), Is.EqualTo(new[] { udonDoubles.name }));
            Assert.That(editMode.compiledAssemblyReferences.Select(Path.GetFileName), Has.Member("nunit.framework.dll"));
            Assert.That(playMode.compiledAssemblyReferences.Select(Path.GetFileName), Has.Member("nunit.framework.dll"));
            Assert.That(doubles.compiledAssemblyReferences.Select(Path.GetFileName), Has.No.Member("nunit.framework.dll"));
            Assert.That(udonDoubles.compiledAssemblyReferences.Select(Path.GetFileName), Has.No.Member("nunit.framework.dll"));
            Assert.That(created.Select(ReferencesOf), Has.All.Member(WorldAssembly));
            Assert.That(ReferencesOf(editMode), Has.Member(WorldEditorAssembly));
            Assert.That(new[] { playMode, doubles, udonDoubles }.Select(ReferencesOf), Has.None.Member(WorldEditorAssembly));
            Assert.That(ReferencesOf(AssemblyOf(editor, LooseScript)).Intersect(createdNames), Is.Empty);

            // A second set, with the first compiled, numbers its names and leaves the assemblies of the
            // first, which only tests use, out of its references.
            string second = TestAssemblyCreator.Create(Parent);
            string prefix = editMode.name.Substring(0, editMode.name.Length - TestAssemblyCreator.EditModeSuffix.Length);
            string secondEditMode = File.ReadAllText($"{second}/EditMode/{prefix}2{TestAssemblyCreator.EditModeSuffix}.asmdef");
            string secondPlayMode = File.ReadAllText($"{second}/PlayMode/{prefix}2{TestAssemblyCreator.PlayModeSuffix}.asmdef");
            Assert.That(second, Is.EqualTo(Tests + " 1"));
            Assert.That(secondEditMode, Does.Contain($"\"{WorldEditorAssembly}\"").And.Not.Contain($"\"{editMode.name}\""));
            Assert.That(secondPlayMode, Does.Not.Contain($"\"{doubles.name}\"").And.Not.Contain($"\"{udonDoubles.name}\""));
        }

        // The Udon doubles assembly is a player assembly with a U# assembly definition, so its example
        // compiles for a world upload and runs as Udon. This assembly can't reference it, so the
        // test reaches it by reflection.
        [UnityTest]
        public IEnumerator Create_NoFolderGiven_CreatesAnUdonDoubleThatCompilesForUploadAndRunsAsUdon()
        {
            // Arrange
            SessionState.SetString(FolderKey, TestAssemblyCreator.Create());
            yield return new WaitForDomainReload();

            // Act
            SessionState.SetString(UploadErrorsKey, string.Join("\n", UdonSharpUploadCompile.Run()));
            yield return new EnterPlayMode();
            SessionState.SetString(CallsKey, string.Join("\n", RunTheExampleUdonDouble(SessionState.GetString(FolderKey, ""))));
            yield return new ExitPlayMode();

            // Assert
            Assert.That(SessionState.GetString(FolderKey, ""), Does.StartWith("Assets/Tests"));
            Assert.That(SessionState.GetString(UploadErrorsKey, "not run"), Is.Empty);
            Assert.That(SessionState.GetString(CallsKey, ""), Is.EqualTo("Example.OnExample"));
        }

        private static IEnumerable<string> ReferencesOf(Assembly assembly) => assembly.assemblyReferences.Select(reference => reference.name);

        private static string[] RunTheExampleUdonDouble(string tests)
        {
            Assembly udonDoubles = AssemblyOf(CompilationPipeline.GetAssemblies(AssembliesType.Editor), tests + "/UdonDoubles/ExampleUdonDouble.cs");
            string prefix = udonDoubles.name.Substring(0, udonDoubles.name.Length - TestAssemblyCreator.UdonDoublesSuffix.Length);
            Type exampleType = Type.GetType($"{prefix}.Tests.ExampleUdonDouble, {udonDoubles.name}", throwOnError: true);

            using var udon = new UdonHost();
            UdonBehaviour log = udon.Create<UdonCallLog>("Log");
            var example = (UdonBehaviour)typeof(UdonHost).GetMethod(nameof(UdonHost.Create)).MakeGenericMethod(exampleType)
                .Invoke(udon, new object[] { "Example", true });
            example.SetProgramVariable("Log", log);
            example.SendCustomEvent("OnExample");
            return UdonHost.Calls(log);
        }

        private static List<string> LogsOf(Action act)
        {
            var logs = new List<string>();
            Application.LogCallback record = (message, stackTrace, type) => logs.Add(message);
            Application.logMessageReceived += record;
            try
            {
                act();
            }
            finally
            {
                Application.logMessageReceived -= record;
            }
            return logs;
        }

        private static void Write(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text);
        }

        private static Assembly AssemblyOf(Assembly[] assemblies, string script)
        {
            return assemblies.Single(assembly => assembly.sourceFiles.Contains(script));
        }
    }
}

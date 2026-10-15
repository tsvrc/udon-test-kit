using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine.TestTools;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    // Creates test assemblies in this project and compiles them, which reloads the domain. A test
    // resumes after a reload with its local variables reset, so it finds the assemblies again. The
    // probe classes carry a name of the run in place of __RUN__, so an assembly an earlier run left
    // compiled can't pass a test; SessionState keeps the name through the reload.
    public class TestAssemblyCreatorCompileTests
    {
        private const string RunKey = "Tsvrc.UdonTestKit.Editor.Tests.TestAssemblyCreatorCompileTests.Run";
        private const string Parent = "Assets/UdonTestKitCreatorCompileTests";
        private const string Tests = Parent + "/Tests";
        // Compiles into Assembly-CSharp, which references every assembly that allows it.
        private const string LooseScript = Parent + "/UdonTestKitLooseScript.cs";
        private const string KitProbe = Tests + "/PlayMode/KitProbe.cs";
        private const string EditorProbe = Tests + "/EditMode/EditorProbe.cs";
        private const string EditorFolderTests = Parent + "/Editor/UdonTestKitEditorFolderTests.cs";

        // Uses each public type of the kit, each SDK type its members take or return, and a type
        // from each other assembly the test assemblies reference. It only has to compile.
        private const string KitProbeSource = @"using System;
using System.Collections;
using Tsvrc.UdonTestKit;
using VRC.SDK3.ClientSim;
using VRC.SDK3.Components;
using VRC.SDK3.Dynamics.PhysBone;
using VRC.SDKBase;
using VRC.SDKBase.Validation;
using VRC.Udon;

public class KitSceneProbe : ClientSimTestBase
{
    public KitSceneProbe() : base(TestScene.Empty(""Probe""), TestSceneLifetime.PerFixture)
    {
    }
}

public class KitProbe__RUN__ : UdonTestBase
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
        if (TestRun.IsActive && BuiltWorld.FromOpenScene().HoldsProgramOf(typeof(UdonCallLog)))
        {
            UdonSharpUploadCompile.Run();
            ClientSimProjectSettings.Fix();
            TestAssemblyCreator.Create();
        }
    }
}
";

        // Uses the Editor test runner, which only the Edit Mode test assembly references.
        private const string EditorProbeSource = @"using UnityEditor.TestTools.TestRunner.Api;

public class EditorProbe__RUN__
{
    public TestRunnerApi Api;
}
";

        // An Edit Mode test in an Editor folder without an assembly definition, for a script in
        // Assembly-CSharp. It only has to compile.
        private const string EditorFolderTestsSource = @"using NUnit.Framework;
using Tsvrc.UdonTestKit;

public class UdonTestKitEditorFolderTests__RUN__
{
    [Test]
    public void UseTheKit()
    {
        using var calls = new CallLog();
        Assert.That(new UdonTestKitLooseScript(), Is.Not.Null);
        Assert.That(typeof(UdonCallLog), Is.Not.Null);
    }
}
";

        private static string Run => SessionState.GetString(RunKey, "");

        [TearDown]
        public void ClearTheSelectionAndRun()
        {
            Selection.activeObject = null;
            SessionState.EraseString(RunKey);
        }

        [UnityTearDown]
        public IEnumerator DeleteTheAssemblies() => ProjectScripts.DeleteFolder(Parent);

        [UnityTest]
        public IEnumerator MenuItem_FolderSelected_CompilesTestAssembliesThereThatUseTheKit()
        {
            // Arrange
            SessionState.SetString(RunKey, System.Guid.NewGuid().ToString("N"));
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(Parent));
            File.WriteAllText(LooseScript, "public class UdonTestKitLooseScript { }");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>(Parent);

            // Act
            EditorApplication.ExecuteMenuItem(TestAssemblyCreator.MenuPath);
            File.WriteAllText(KitProbe, KitProbeSource.Replace("__RUN__", Run));
            File.WriteAllText(EditorProbe, EditorProbeSource.Replace("__RUN__", Run));
            AssetDatabase.ImportAsset(KitProbe);
            AssetDatabase.ImportAsset(EditorProbe);
            yield return new WaitForDomainReload();

            // Assert
            Assembly[] editor = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
            Assembly editMode = AssemblyOf(editor, Tests + "/EditMode/ExampleEditModeTests.cs");
            Assembly playMode = AssemblyOf(editor, Tests + "/PlayMode/ExamplePlayModeTests.cs");
            Assembly doubles = AssemblyOf(editor, Tests + "/Doubles/ExampleSpy.cs");
            string[] created = { editMode.name, playMode.name, doubles.name };
            Assert.That(System.Type.GetType("KitProbe" + Run + ", " + playMode.name), Is.Not.Null);
            Assert.That(System.Type.GetType("EditorProbe" + Run + ", " + editMode.name), Is.Not.Null);
            Assert.That(CompilationPipeline.GetAssemblies(AssembliesType.Player).Select(assembly => assembly.name),
                Has.Member(playMode.name).And.Member(doubles.name).And.No.Member(editMode.name));
            Assert.That(CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies).Select(assembly => assembly.name)
                .Intersect(created), Is.Empty);
            Assert.That(editMode.compiledAssemblyReferences.Select(Path.GetFileName), Has.Member("nunit.framework.dll"));
            Assert.That(playMode.compiledAssemblyReferences.Select(Path.GetFileName), Has.Member("nunit.framework.dll"));
            Assert.That(doubles.compiledAssemblyReferences.Select(Path.GetFileName), Has.No.Member("nunit.framework.dll"));
            Assert.That(AssemblyOf(editor, LooseScript).assemblyReferences.Select(assembly => assembly.name).Intersect(created), Is.Empty);

            // A second run, with the first set compiled, numbers its names and leaves the first doubles
            // assembly, which only tests use, out of its references.
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>(Parent);
            EditorApplication.ExecuteMenuItem(TestAssemblyCreator.MenuPath);
            string prefix = editMode.name.Substring(0, editMode.name.Length - TestAssemblyCreator.EditModeSuffix.Length);
            string second = File.ReadAllText($"{Tests} 1/PlayMode/{prefix}2{TestAssemblyCreator.PlayModeSuffix}.asmdef");
            Assert.That(second, Does.Not.Contain($"\"{doubles.name}\""));
        }

        // The warning for scripts in Assembly-CSharp says Edit Mode tests for them can go in an Editor
        // folder without an assembly definition.
        [UnityTest]
        public IEnumerator AssemblyCSharpWarning_EditModeTestsInAnEditorFolder_CompileWithTheKit()
        {
            // Arrange
            SessionState.SetString(RunKey, System.Guid.NewGuid().ToString("N"));
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(Parent));
            AssetDatabase.CreateFolder(Parent, "Editor");
            File.WriteAllText(LooseScript, "public class UdonTestKitLooseScript { }");
            File.WriteAllText(EditorFolderTests, EditorFolderTestsSource.Replace("__RUN__", Run));

            // Act
            AssetDatabase.ImportAsset(LooseScript);
            AssetDatabase.ImportAsset(EditorFolderTests);
            yield return new WaitForDomainReload();

            // Assert
            Assert.That(System.Type.GetType("UdonTestKitEditorFolderTests" + Run + ", Assembly-CSharp-Editor"), Is.Not.Null);
        }

        private static Assembly AssemblyOf(Assembly[] assemblies, string script)
        {
            return assemblies.Single(assembly => assembly.sourceFiles.Contains(script));
        }
    }
}

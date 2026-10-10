using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace UdonTestKit
{
    /// <summary>
    /// Creates a Tests folder with an Edit Mode and a Play Mode test assembly, each referencing
    /// everything a test needs: Unity Test Framework, the kit, UdonSharp, the VRChat SDK,
    /// ClientSim, and the world's own assemblies under Assets. Each gets an example test.
    /// </summary>
    public static class TestAssemblyCreator
    {
        public const string MenuPath = "Assets/Create/Udon Test Kit/Test Assemblies";

        internal static readonly string[] SharedReferences =
        {
            "UnityEngine.TestRunner", "UdonTestKit", "UdonSharp.Runtime", "VRC.SDKBase", "VRC.SDK3", "VRC.ClientSim",
        };

        internal static readonly string[] PrecompiledReferences = { "nunit.framework.dll", "VRCSDKBase.dll", "VRCSDK3.dll" };

        internal const string AssemblyCSharpWarning =
            "[UdonTestKit] Some scripts under Assets have no assembly definition, so they compile into " +
            "Assembly-CSharp, which the created test assemblies can't reference. Edit Mode tests for them can go " +
            "in an Editor folder without an assembly definition. Play Mode tests need those scripts in an assembly definition.";

        [MenuItem(MenuPath, priority = 90)]
        private static void CreateFromMenu()
        {
            Create(SelectedFolder());
        }

        /// <summary>Creates the Tests folder inside <paramref name="parentFolder"/>, imports it, and
        /// returns its path. An existing Tests folder is left alone; the new one gets a number.</summary>
        public static string Create(string parentFolder = "Assets")
        {
            IEnumerable<string> existing = CompilationPipeline.GetAssemblies(AssembliesType.Editor).Select(assembly => assembly.name);
            string prefix = UniquePrefix(AssemblyPrefix(Application.productName), existing);
            string folder = Create(parentFolder, prefix, WorldAssemblyNames());
            AssetDatabase.Refresh();
            Debug.Log($"[UdonTestKit] Created test assemblies in {folder}.");
            if (HasScriptsInAssemblyCSharp())
                Debug.LogWarning(AssemblyCSharpWarning);
            return folder;
        }

        internal static bool HasScriptsInAssemblyCSharp()
        {
            return CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                .Any(assembly => assembly.name == "Assembly-CSharp" && assembly.sourceFiles.Any(file => file.StartsWith("Assets/")));
        }

        // Writes the Tests folder into parentFolder and returns its path, without refreshing the
        // AssetDatabase.
        internal static string Create(string parentFolder, string assemblyPrefix, IEnumerable<string> worldAssemblies)
        {
            string testsFolder = UniqueFolder(Path.Combine(parentFolder, "Tests"));
            List<string> world = worldAssemblies.ToList();

            WriteAssembly(Path.Combine(testsFolder, "EditMode"), assemblyPrefix + ".Tests.EditMode", editorOnly: true, world,
                "ExampleEditModeTests.cs", ExampleEditModeTests);
            WriteAssembly(Path.Combine(testsFolder, "PlayMode"), assemblyPrefix + ".Tests.PlayMode", editorOnly: false, world,
                "ExamplePlayModeTests.cs", ExamplePlayModeTests);

            return testsFolder.Replace('\\', '/');
        }

        internal static string AssemblyDefinitionJson(string name, bool editorOnly, IEnumerable<string> worldAssemblies)
        {
            IEnumerable<string> references = editorOnly
                ? SharedReferences.Concat(new[] { "UnityEditor.TestRunner" })
                : SharedReferences;
            references = references.Concat(worldAssemblies).Distinct();

            var json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine($"    \"name\": \"{name}\",");
            json.AppendLine($"    \"rootNamespace\": \"\",");
            json.AppendLine($"    \"references\": [{JsonList(references)}],");
            json.AppendLine($"    \"includePlatforms\": [{(editorOnly ? "\"Editor\"" : "")}],");
            json.AppendLine("    \"excludePlatforms\": [],");
            json.AppendLine("    \"allowUnsafeCode\": false,");
            json.AppendLine("    \"overrideReferences\": true,");
            json.AppendLine($"    \"precompiledReferences\": [{JsonList(PrecompiledReferences)}],");
            json.AppendLine("    \"autoReferenced\": false,");
            json.AppendLine("    \"defineConstraints\": [\"UNITY_INCLUDE_TESTS\"],");
            json.AppendLine("    \"versionDefines\": [],");
            json.AppendLine("    \"noEngineReferences\": false");
            json.AppendLine("}");
            return json.ToString();
        }

        // The world's own assemblies: assembly definitions with source files under Assets, other
        // than test assemblies. Scripts without an assembly definition compile into
        // Assembly-CSharp, which an assembly definition can't reference.
        internal static IEnumerable<string> WorldAssemblyNames()
        {
            HashSet<string> testAssemblies = new HashSet<string>(
                CompilationPipeline.GetAssemblies(AssembliesType.Editor)
                    .Where(assembly => assembly.allReferences.Any(path => Path.GetFileName(path) == "nunit.framework.dll"))
                    .Select(assembly => assembly.name));

            return CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                .Where(assembly => assembly.name != "Assembly-CSharp" && !testAssemblies.Contains(assembly.name))
                .Where(assembly => assembly.sourceFiles.Any(file => file.StartsWith("Assets/")))
                .Select(assembly => assembly.name)
                .OrderBy(name => name);
        }

        internal static string AssemblyPrefix(string productName)
        {
            string prefix = Regex.Replace(productName, "[^A-Za-z0-9]", "");
            return prefix.Length > 0 && !char.IsDigit(prefix[0]) ? prefix : "World";
        }

        // Two assembly definitions with the same name stop the whole project compiling, so a second
        // set of test assemblies gets a numbered prefix.
        internal static string UniquePrefix(string prefix, IEnumerable<string> existingAssemblies)
        {
            var existing = new HashSet<string>(existingAssemblies);
            string candidate = prefix;
            for (int i = 2; existing.Contains(candidate + ".Tests.EditMode") || existing.Contains(candidate + ".Tests.PlayMode"); i++)
                candidate = prefix + i;
            return candidate;
        }

        private static void WriteAssembly(string folder, string name, bool editorOnly, IEnumerable<string> worldAssemblies,
            string exampleFileName, string exampleSource)
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, name + ".asmdef"), AssemblyDefinitionJson(name, editorOnly, worldAssemblies));
            File.WriteAllText(Path.Combine(folder, exampleFileName), exampleSource);
        }

        private static string UniqueFolder(string folder)
        {
            string candidate = folder;
            for (int i = 1; Directory.Exists(candidate); i++)
                candidate = folder + " " + i;
            return candidate;
        }

        // The folder selected in the Project window, or the folder of a selected file, as long as
        // it's under Assets; otherwise Assets.
        internal static string SelectedFolder()
        {
            foreach (Object selected in Selection.GetFiltered<Object>(SelectionMode.Assets))
            {
                string path = AssetDatabase.GetAssetPath(selected);
                if (!AssetDatabase.IsValidFolder(path))
                    path = Path.GetDirectoryName(path)?.Replace('\\', '/');
                if (path != null && (path == "Assets" || path.StartsWith("Assets/")))
                    return path;
            }
            return "Assets";
        }

        private static string JsonList(IEnumerable<string> values)
        {
            return string.Join(", ", values.Select(value => "\"" + value + "\""));
        }

        internal const string ExampleEditModeTests =
@"using NUnit.Framework;
using UdonTestKit;

public class ExampleEditModeTests
{
    [Test]
    public void SendCustomEvent_CallbackA_IsRecorded()
    {
        using var calls = new CallLog();
        CallbackSpy spy = calls.CreateSpy();

        spy.SendCustomEvent(nameof(CallbackSpy.CallbackA));

        Assert.That(calls, Is.EqualTo(new[] { ""Spy.CallbackA"" }));
    }
}
";

        internal const string ExamplePlayModeTests =
@"using System.Collections;
using NUnit.Framework;
using UdonTestKit;
using UnityEngine.TestTools;
using VRC.SDKBase;

public class ExamplePlayModeTests : ClientSimTestBase
{
    [UnityTest]
    public IEnumerator SpawnRemotePlayer_InClientSim_JoinsThePlayerList()
    {
        yield return Session.Start();

        VRCPlayerApi remote = Session.SpawnRemotePlayer(""Remote"");

        Assert.That(VRCPlayerApi.GetPlayerCount(), Is.EqualTo(2));
        Assert.That(remote.isLocal, Is.False);
    }
}
";
    }
}

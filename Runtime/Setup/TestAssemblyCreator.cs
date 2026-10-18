using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tsvrc.UdonTestKit
{
    /// <summary>
    /// Creates a Tests folder with an Edit Mode and a Play Mode test assembly, and two test doubles
    /// assemblies both of them reference: one for behaviours the tests add to GameObjects, such as
    /// spies, and one for behaviours that run as Udon. They reference the kit, UdonSharp, the VRChat
    /// SDK, and the world's own assemblies under Assets. Each gets an example.
    /// </summary>
    public static class TestAssemblyCreator
    {
        public const string MenuPath = "Assets/Create/TsVRC/Udon Test Kit/Test Assemblies";

        internal const string EditModeSuffix = ".Tests.EditMode";
        internal const string PlayModeSuffix = ".Tests.PlayMode";
        internal const string DoublesSuffix = ".Tests.Doubles";
        internal const string UdonDoublesSuffix = ".Tests.UdonDoubles";

        internal static readonly string[] KitReferences = { "Tsvrc.UdonTestKit", "Tsvrc.UdonTestKit.UdonSpies", "UdonSharp.Runtime", "VRC.SDKBase", "VRC.Udon", "VRC.SDK3", "VRC.ClientSim" };

        // Player builds leave out the test assemblies and the kit's own, so the Udon doubles
        // assembly references none of them.
        internal static readonly string[] UdonDoublesReferences = { "Tsvrc.UdonTestKit.UdonSpies", "UdonSharp.Runtime", "VRC.SDKBase", "VRC.Udon", "VRC.SDK3" };

        internal static readonly string[] SdkDlls = { "VRCSDKBase.dll", "VRCSDK3.dll" };

        internal const string AssemblyCSharpWarning =
            "[UdonTestKit] Some scripts under Assets have no assembly definition, so they compile into " +
            "Assembly-CSharp, which the created test assemblies can't reference. Edit Mode tests for them can go " +
            "in an Editor folder without an assembly definition. Play Mode tests need those scripts in an assembly definition.";

        // The examples go in the namespace <prefix>.Tests, which this stands for.
        internal const string PrefixToken = "__PREFIX__";

        internal enum AssemblyKind
        {
            EditModeTests,
            PlayModeTests,
            // Targets every platform: Unity won't add a behaviour from an Editor-only assembly to a GameObject.
            Doubles,
            // Has no define constraint, so player builds include it: the compile UdonSharp runs for a
            // world upload reads only player assemblies.
            UdonDoubles,
        }

        [MenuItem(MenuPath, priority = 90)]
        private static void CreateFromMenu()
        {
            Create(SelectedFolder());
        }

        /// <summary>Creates the Tests folder inside <paramref name="parentFolder"/>, which is Assets or
        /// a folder under it, imports it, and returns its path. An existing Tests folder is left alone;
        /// the new one gets a number.</summary>
        public static string Create(string parentFolder = "Assets")
        {
            // The kit imports what it writes, and creates assets next to it.
            if (parentFolder != "Assets" && !parentFolder.StartsWith("Assets/"))
            {
                throw new ArgumentException($"{parentFolder} isn't Assets or a folder under it, where the test assemblies go.", nameof(parentFolder));
            }

            Assembly[] editor = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
            Assembly[] player = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);
            string prefix = UniquePrefix(AssemblyPrefix(Application.productName), editor.Select(assembly => assembly.name));
            string folder = Create(parentFolder, prefix, WorldAssemblyNames(editor, player));
            AssetDatabase.Refresh();
            CreateUdonSharpAssets(folder + "/UdonDoubles", prefix + UdonDoublesSuffix);
            Debug.Log($"[UdonTestKit] Created test assemblies in {folder}.");
            if (HasScriptsInAssemblyCSharp(player))
            {
                Debug.LogWarning(AssemblyCSharpWarning);
            }
            return folder;
        }

        internal static bool HasScriptsInAssemblyCSharp(IEnumerable<Assembly> playerAssemblies)
        {
            return playerAssemblies.Any(assembly => assembly.name == "Assembly-CSharp" && assembly.sourceFiles.Any(file => file.StartsWith("Assets/")));
        }

        // Writes the Tests folder into parentFolder and returns its path, without refreshing the
        // AssetDatabase.
        internal static string Create(string parentFolder, string assemblyPrefix, IEnumerable<string> worldAssemblies)
        {
            string testsFolder = UniqueFolder(Path.Combine(parentFolder, "Tests"));
            List<string> world = worldAssemblies.ToList();
            string doubles = assemblyPrefix + DoublesSuffix;
            string udonDoubles = assemblyPrefix + UdonDoublesSuffix;
            List<string> worldAndDoubles = world.Append(doubles).Append(udonDoubles).ToList();

            WriteAssembly(Path.Combine(testsFolder, "UdonDoubles"), udonDoubles, AssemblyKind.UdonDoubles, world,
                "ExampleUdonDouble.cs", Example(ExampleUdonDouble, assemblyPrefix));
            WriteAssembly(Path.Combine(testsFolder, "Doubles"), doubles, AssemblyKind.Doubles, world,
                "ExampleSpy.cs", Example(ExampleSpy, assemblyPrefix));
            WriteAssembly(Path.Combine(testsFolder, "EditMode"), assemblyPrefix + EditModeSuffix, AssemblyKind.EditModeTests, worldAndDoubles,
                "ExampleEditModeTests.cs", Example(ExampleEditModeTests, assemblyPrefix));
            WriteAssembly(Path.Combine(testsFolder, "PlayMode"), assemblyPrefix + PlayModeSuffix, AssemblyKind.PlayModeTests, worldAndDoubles,
                "ExamplePlayModeTests.cs", Example(ExamplePlayModeTests, assemblyPrefix));

            return testsFolder.Replace('\\', '/');
        }

        internal static string Example(string example, string assemblyPrefix) => example.Replace(PrefixToken, assemblyPrefix);

        internal static string AssemblyDefinitionJson(string name, AssemblyKind kind, IEnumerable<string> otherReferences)
        {
            bool tests = kind == AssemblyKind.EditModeTests || kind == AssemblyKind.PlayModeTests;
            IEnumerable<string> testRunner = kind == AssemblyKind.EditModeTests ? new[] { "UnityEngine.TestRunner", "UnityEditor.TestRunner" }
                : kind == AssemblyKind.PlayModeTests ? new[] { "UnityEngine.TestRunner" }
                : Enumerable.Empty<string>();
            IEnumerable<string> kit = kind == AssemblyKind.UdonDoubles ? UdonDoublesReferences : KitReferences;
            IEnumerable<string> references = testRunner.Concat(kit).Concat(otherReferences).Distinct();
            IEnumerable<string> dlls = tests ? SdkDlls.Prepend("nunit.framework.dll") : SdkDlls;

            var json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine($"    \"name\": \"{name}\",");
            json.AppendLine($"    \"rootNamespace\": \"\",");
            json.AppendLine($"    \"references\": [{JsonList(references)}],");
            json.AppendLine($"    \"includePlatforms\": [{(kind == AssemblyKind.EditModeTests ? "\"Editor\"" : "")}],");
            json.AppendLine("    \"excludePlatforms\": [],");
            json.AppendLine("    \"allowUnsafeCode\": false,");
            json.AppendLine("    \"overrideReferences\": true,");
            json.AppendLine($"    \"precompiledReferences\": [{JsonList(dlls)}],");
            json.AppendLine("    \"autoReferenced\": false,");
            json.AppendLine($"    \"defineConstraints\": [{(kind == AssemblyKind.UdonDoubles ? "" : "\"UNITY_INCLUDE_TESTS\"")}],");
            json.AppendLine("    \"versionDefines\": [],");
            json.AppendLine("    \"noEngineReferences\": false");
            json.AppendLine("}");
            return json.ToString();
        }

        // The world's own assemblies: assembly definitions with source files under Assets, other
        // than test assemblies, the kit's, and the Udon doubles assemblies the kit created. Scripts
        // without an assembly definition compile into Assembly-CSharp, which an assembly definition
        // can't reference.
        internal static IEnumerable<string> WorldAssemblyNames(IEnumerable<Assembly> editorAssemblies, IEnumerable<Assembly> playerAssemblies)
        {
            HashSet<string> testAssemblies = new HashSet<string>(editorAssemblies
                .Where(assembly => assembly.allReferences.Any(path => Path.GetFileName(path) == "nunit.framework.dll"))
                .Select(assembly => assembly.name));

            return playerAssemblies
                .Where(assembly => assembly.name != "Assembly-CSharp" && !testAssemblies.Contains(assembly.name) && !assembly.name.StartsWith("Tsvrc.UdonTestKit")
                    && !assembly.name.EndsWith(UdonDoublesSuffix))
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
            for (int i = 2; new[] { EditModeSuffix, PlayModeSuffix, DoublesSuffix, UdonDoublesSuffix }.Any(suffix => existing.Contains(candidate + suffix)); i++)
            {
                candidate = prefix + i;
            }
            return candidate;
        }

        // UdonSharp compiles the scripts of an assembly only when a U# assembly definition names it,
        // and runs a script as Udon from its program asset. Both are made as UdonSharp's own Create
        // menu items make them.
        private static void CreateUdonSharpAssets(string folder, string assemblyName)
        {
            var assemblyDefinition = ScriptableObject.CreateInstance<UdonSharpAssemblyDefinition>();
            assemblyDefinition.sourceAssembly = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>($"{folder}/{assemblyName}.asmdef");
            AssetDatabase.CreateAsset(assemblyDefinition, $"{folder}/{assemblyName}.asset");

            var programAsset = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
            programAsset.sourceCsScript = AssetDatabase.LoadAssetAtPath<MonoScript>($"{folder}/ExampleUdonDouble.cs");
            AssetDatabase.CreateAsset(programAsset, $"{folder}/ExampleUdonDouble.asset");
        }

        private static void WriteAssembly(string folder, string name, AssemblyKind kind, IEnumerable<string> references,
            string exampleFileName, string exampleSource)
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, name + ".asmdef"), AssemblyDefinitionJson(name, kind, references));
            File.WriteAllText(Path.Combine(folder, exampleFileName), exampleSource);
        }

        private static string UniqueFolder(string folder)
        {
            string candidate = folder;
            for (int i = 1; Directory.Exists(candidate); i++)
            {
                candidate = folder + " " + i;
            }
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
                {
                    path = Path.GetDirectoryName(path).Replace('\\', '/');
                }
                if (path.StartsWith("Assets/"))
                {
                    return path;
                }
            }
            return "Assets";
        }

        private static string JsonList(IEnumerable<string> values)
        {
            return string.Join(", ", values.Select(value => "\"" + value + "\""));
        }

        internal const string ExampleSpy =
@"using Tsvrc.UdonTestKit;

namespace __PREFIX__.Tests
{
    // A spy with a callback named after what your code calls. Test doubles that tests add to
    // GameObjects go in this assembly: Unity can't add one from the Edit Mode test assembly.
    public class ExampleSpy : CallbackSpy
    {
        public void OnExample() => Record();
    }
}
";

        internal const string ExampleUdonDouble =
@"using Tsvrc.UdonTestKit;
using UdonSharp;

namespace __PREFIX__.Tests
{
    // A test double that runs as Udon and adds each call it gets to a log. Doubles that UdonHost runs
    // go in this assembly, which player builds include, as the compile for a world upload needs.
    // UdonSharp compiles every U# script of a project together, so no two of them can share a
    // namespace and name.
    public class ExampleUdonDouble : UdonSharpBehaviour
    {
        public UdonCallLog Log;

        public void OnExample()
        {
            Log.Add(name + "".OnExample"");
        }
    }
}
";

        internal const string ExampleEditModeTests =
@"using NUnit.Framework;
using Tsvrc.UdonTestKit;

namespace __PREFIX__.Tests
{
    public class ExampleEditModeTests
    {
        [Test]
        public void SendCustomEvent_OnExample_IsRecorded()
        {
            using var calls = new CallLog();
            ExampleSpy spy = calls.CreateSpy<ExampleSpy>(""Example"");

            spy.SendCustomEvent(nameof(ExampleSpy.OnExample));

            Assert.That(calls, Is.EqualTo(new[] { ""Example.OnExample"" }));
        }
    }
}
";

        internal const string ExamplePlayModeTests =
@"using System.Collections;
using NUnit.Framework;
using Tsvrc.UdonTestKit;
using UnityEngine.TestTools;
using VRC.SDKBase;
using VRC.Udon;

namespace __PREFIX__.Tests
{
    public class ExamplePlayModeTests : UdonTestBase
    {
        [UnityTest]
        public IEnumerator SpawnRemotePlayer_InClientSim_JoinsThePlayerList()
        {
            yield return Session.Start();

            VRCPlayerApi remote = Session.SpawnRemotePlayer(""Remote"");

            Assert.That(VRCPlayerApi.GetPlayerCount(), Is.EqualTo(2));
            Assert.That(remote.isLocal, Is.False);
        }

        [Test]
        public void SendCustomEvent_OnExample_IsLoggedFromUdon()
        {
            UdonBehaviour log = Udon.Create<UdonCallLog>(""Log"");
            UdonBehaviour example = Udon.Create<ExampleUdonDouble>(""Example"");
            example.SetProgramVariable(nameof(ExampleUdonDouble.Log), log);

            example.SendCustomEvent(nameof(ExampleUdonDouble.OnExample));

            Assert.That(UdonHost.Calls(log), Is.EqualTo(new[] { ""Example.OnExample"" }));
        }
    }
}
";
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using AssemblyKind = Tsvrc.UdonTestKit.TestAssemblyCreator.AssemblyKind;

namespace Tsvrc.UdonTestKit
{
    /// <summary>
    /// Adds to the test assemblies <see cref="TestAssemblyCreator"/> created under Assets the world's
    /// assembly definitions they don't reference yet, such as one the world got after they were
    /// created, as TestAssemblyCreator gives them: the Editor-only ones to the Edit Mode test assembly
    /// only. Nothing is removed, and the rest of each assembly definition stays as it is.
    /// </summary>
    public static class TestAssemblyUpdater
    {
        public const string MenuPath = "Tools/TsVRC/Udon Test Kit/Update Test Assembly References";

        private const string GuidReference = "GUID:";

        private static readonly AssemblyKind[] Kinds = { AssemblyKind.EditModeTests, AssemblyKind.PlayModeTests, AssemblyKind.Doubles, AssemblyKind.UdonDoubles };

        [MenuItem(MenuPath)]
        private static void UpdateFromMenu()
        {
            Update();
        }

        /// <summary>Adds the references, imports the assembly definitions it changed, and returns
        /// their paths.</summary>
        public static IReadOnlyList<string> Update()
        {
            Assembly[] editor = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
            Assembly[] player = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);
            List<string> world = TestAssemblyCreator.WorldAssemblyNames(player, TestAssemblyCreator.IsTestAssembly).ToList();
            List<string> editorWorld = TestAssemblyCreator.EditorWorldAssemblyNames(editor, TestAssemblyCreator.IsTestAssembly).ToList();

            var changed = new List<string>();
            foreach ((string definition, AssemblyKind kind) in CreatedAssemblyDefinitions(editor.Select(assembly => assembly.name),
                CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName))
            {
                string json = File.ReadAllText(definition);
                string updated = WithMissingReferences(json, TestAssemblyCreator.WorldReferencesOf(kind, world, editorWorld));
                if (updated != json)
                {
                    File.WriteAllText(definition, updated);
                    changed.Add(definition);
                }
            }
            AssetDatabase.Refresh();

            Debug.Log(changed.Count == 0 ? "[UdonTestKit] The test assemblies reference every assembly definition of the world."
                : $"[UdonTestKit] Added the world's assembly definitions to {string.Join(", ", changed)}.");
            return changed;
        }

        // The assembly definitions under Assets of the assemblies named as TestAssemblyCreator names
        // them, but the kit's own. Assembly definitions in packages are left alone.
        internal static IEnumerable<(string definition, AssemblyKind kind)> CreatedAssemblyDefinitions(IEnumerable<string> assemblyNames,
            Func<string, string> definitionOf)
        {
            return from name in assemblyNames
                   where !name.StartsWith("Tsvrc.UdonTestKit")
                   from kind in Kinds
                   where name.EndsWith(TestAssemblyCreator.SuffixOf(kind))
                   let definition = definitionOf(name)
                   where definition.StartsWith("Assets/")
                   select (definition, kind);
        }

        // The text of an assembly definition with the references it lacks added after the ones it has.
        // Unity requires one form for all the references of an assembly definition, names or GUIDs,
        // so the added ones take the form of those already there.
        internal static string WithMissingReferences(string json, IEnumerable<string> references)
        {
            List<string> present = AssemblyDefinitionText.Items(json, "references");
            bool guids = present.Any(reference => reference.StartsWith(GuidReference));
            var named = new HashSet<string>(present.Select(reference => reference.StartsWith(GuidReference) ? AssemblyNameOf(reference) : reference));
            IEnumerable<string> missing = references.Distinct().Where(name => !named.Contains(name))
                .Select(name => guids ? GuidReference + AssetDatabase.AssetPathToGUID(CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(name)) : name);
            return AssemblyDefinitionText.WithItemsAdded(json, "references", missing);
        }

        private static string AssemblyNameOf(string guidReference)
        {
            string path = AssetDatabase.GUIDToAssetPath(guidReference.Substring(GuidReference.Length));
            return string.IsNullOrEmpty(path) ? guidReference : JsonUtility.FromJson<Named>(File.ReadAllText(path)).name;
        }

        [Serializable]
        private class Named
        {
            public string name;
        }
    }
}

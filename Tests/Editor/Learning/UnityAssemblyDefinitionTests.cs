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

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // TestAssemblyCreator leaves Assembly-CSharp out of the references it writes, and numbers the
    // names of a second set of test assemblies. Each test here breaks the compile on purpose: a
    // failed compile keeps the current domain, and the teardown compiles the project without the
    // broken assembly definitions.
    public class UnityAssemblyDefinitionTests
    {
        private const string Folder = "Assets/UdonTestKitAssemblyDefinitionTests";

        private readonly List<string> _compiled = new List<string>();
        private readonly List<string> _errors = new List<string>();

        [UnityTearDown]
        public IEnumerator DeleteTheFolder() => ProjectScripts.DeleteFolders(Folder);

        [UnityTest]
        public IEnumerator Compile_AssemblyDefinitionReferencingAssemblyCSharp_CantUseItsScripts()
        {
            // Arrange
            Write("UdonTestKitLooseScript.cs", "public class UdonTestKitLooseScript { }");
            Write("Referencing/UdonTestKitReferencing.asmdef", "{ \"name\": \"UdonTestKitReferencing\", \"references\": [ \"Assembly-CSharp\" ] }");
            Write("Referencing/UdonTestKitUsesTheLooseScript.cs", "public class UdonTestKitUsesTheLooseScript { public UdonTestKitLooseScript Loose; }");

            // Act
            yield return Compile();

            // Assert
            Assert.That(_errors, Has.Some.Contains("error CS0246: The type or namespace name 'UdonTestKitLooseScript' could not be found"));
        }

        [UnityTest]
        public IEnumerator Compile_TwoAssemblyDefinitionsWithTheSameName_CompilesNothing()
        {
            // Arrange
            Write("UdonTestKitLooseScript.cs", "public class UdonTestKitLooseScript { }");
            Write("A/UdonTestKitTwice.asmdef", "{ \"name\": \"UdonTestKitTwice\" }");
            Write("A/UdonTestKitTwiceA.cs", "public class UdonTestKitTwiceA { }");
            Write("B/UdonTestKitTwice.asmdef", "{ \"name\": \"UdonTestKitTwice\" }");
            Write("B/UdonTestKitTwiceB.cs", "public class UdonTestKitTwiceB { }");

            // Act
            yield return Compile();

            // Assert
            Assert.That(_errors, Has.Some.StartsWith("Assembly with name 'UdonTestKitTwice' already exists"));
            Assert.That(EditorUtility.scriptCompilationFailed, Is.True);
            Assert.That(_compiled, Is.Empty);
        }

        private static void Write(string path, string text)
        {
            string file = Path.Combine(Folder, path);
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file, text);
        }

        // Imports the folder and runs the compile that follows, recording which assemblies compiled
        // and every error, compiler or logged.
        private IEnumerator Compile()
        {
            _compiled.Clear();
            _errors.Clear();
            System.Action<string, CompilerMessage[]> collect = (assembly, messages) =>
            {
                _compiled.Add(Path.GetFileNameWithoutExtension(assembly));
                _errors.AddRange(messages.Where(message => message.type == CompilerMessageType.Error).Select(message => message.message));
            };
            Application.LogCallback log = (message, stackTrace, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception)
                {
                    _errors.Add(message);
                }
            };
            // The errors are what the test is after.
            LogAssert.ignoreFailingMessages = true;
            CompilationPipeline.assemblyCompilationFinished += collect;
            Application.logMessageReceived += log;
            try
            {
                AssetDatabase.Refresh();
                yield return ProjectScripts.CompileThatFails();
            }
            finally
            {
                CompilationPipeline.assemblyCompilationFinished -= collect;
                Application.logMessageReceived -= log;
            }
        }
    }
}

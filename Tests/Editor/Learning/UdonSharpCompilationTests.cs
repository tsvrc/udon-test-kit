using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;
using UdonSharp.Compiler;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // UdonSharp compiles the scripts of Assembly-CSharp and of each assembly a U# assembly definition
    // names, and no others, so TestAssemblyCreator creates one for the Udon doubles assembly. It
    // compiles them all as one compilation, so two U# classes with the same namespace and name fail
    // the compile of every program. So the examples TestAssemblyCreator writes go in a namespace
    // named after their assemblies, apart from the kit's own copies and those of another set.
    public class UdonSharpCompilationTests
    {
        private const string Folder = "Assets/UdonTestKitCompilationTests";
        private const string Copy = Folder + "/UdonDouble.cs";

        private readonly List<string> _errors = new List<string>();

        [TearDown]
        public void CompileWithoutTheCopy()
        {
            AssetDatabase.DeleteAsset(Copy);
            UdonSharpEditorUtility.ResetAssemblyCaches();
            UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions());
        }

        [UnityTearDown]
        public IEnumerator DeleteTheFolder() => ProjectScripts.DeleteFolders(Folder);

        [Test]
        public void CompileSync_ProgramAssetOfAScriptInAnAssemblyNoUdonSharpAssemblyDefinitionNames_LogsAnError()
        {
            // Arrange: the Editor test assembly of the kit has no U# assembly definition.
            using var programAsset = new TemporaryProgramAsset(typeof(EditorOnlyBehaviour));

            // Act
            RecordErrors(() => UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions()));

            // Assert
            Assert.That(_errors, Has.Some.Contains("EditorOnlyBehaviour.cs' does not belong to a U# assembly"));
        }

        [Test]
        public void CompileSync_ClassOfTheSameNamespaceAndNameInAnotherAssembly_FailsWithCS0101()
        {
            // Arrange: the class of a kit Udon double, again, in Assembly-CSharp.
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(Folder));
            File.WriteAllText(Copy, "namespace Tsvrc.UdonTestKit.Tests { public class UdonDouble : UdonSharp.UdonSharpBehaviour { } }");
            AssetDatabase.ImportAsset(Copy);
            UdonSharpEditorUtility.ResetAssemblyCaches();

            // Act
            RecordErrors(() => UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions()));

            // Assert
            Assert.That(_errors, Has.Some.Contains("error CS0101: The namespace 'Tsvrc.UdonTestKit.Tests' already contains a definition for 'UdonDouble'"));
        }

        private void RecordErrors(Action compile)
        {
            Application.LogCallback record = (message, stackTrace, type) =>
            {
                if (type == LogType.Error)
                {
                    _errors.Add(message);
                }
            };
            Application.logMessageReceived += record;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                compile();
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
                Application.logMessageReceived -= record;
            }
        }
    }
}

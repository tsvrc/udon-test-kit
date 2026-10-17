using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;
using Tsvrc.UdonTestKit.Tests;
using UdonSharp;
using UdonSharp.Compiler;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // Before a world build, the VRChat SDK has UdonSharp compile every program for upload. The Editor
    // compile reads every assembly that has a U# assembly definition, but the upload compile reads only
    // player assemblies. A program asset whose script is outside the assemblies
    // the upload compile reads makes it log an error and compile nothing, and the build still goes on.
    // So UdonCallLog and the kit's own Udon test doubles are in player assemblies. The upload compile
    // leaves out code inside #if UNITY_EDITOR, so UdonSharpUploadCompile compiles for the Editor again
    // after it.
    public class UdonSharpBuildCompileTests
    {
        private const string CompiledLog = "Compile of ";

        private readonly List<(LogType type, string message)> _logs = new List<(LogType, string)>();

        [TearDown]
        public void TearDown()
        {
            _logs.Clear();
            UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions());
        }

        [Test]
        public void CompileSync_ForTheEditorWithAProgramAssetOfATestsOnlyScript_CompilesWithoutErrors()
        {
            // Arrange
            using var programAsset = new TemporaryProgramAsset(typeof(TestsOnlyBehaviour));

            // Act
            Record(() => UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions()));

            // Assert
            Assert.That(Errors, Is.Empty);
            Assert.That(Messages, Has.Some.Contains(CompiledLog));
        }

        [Test]
        public void BuildRequested_ProgramAssetOfATestsOnlyScript_LogsAnErrorAndCompilesNothing()
        {
            // Arrange
            using var programAsset = new TemporaryProgramAsset(typeof(TestsOnlyBehaviour));
            // As in a world, where the Editor compiled it before any upload.
            UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions());

            // Act
            bool buildGoesOn = RequestBuild();

            // Assert
            Assert.That(Errors, Is.Not.Empty.And.All.Contains("TestsOnlyBehaviour.cs' does not belong to a U# assembly"));
            Assert.That(Messages, Has.None.Contains(CompiledLog));
            Assert.That(buildGoesOn, Is.True);
        }

        [Test]
        public void BuildRequested_EveryScriptInAPlayerAssembly_CompilesThemWithoutErrors()
        {
            // Act
            bool buildGoesOn = RequestBuild();

            // Assert
            Assert.That(Errors, Is.Empty);
            Assert.That(Messages, Has.Some.Contains(CompiledLog));
            Assert.That(buildGoesOn, Is.True);
        }

        [Test]
        public void BuildRequested_CodeInsideIfUnityEditor_IsLeftOutOfTheProgram()
        {
            // Arrange
            UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions());
            Assert.That(ProgramHeap.Strings(typeof(UdonDouble)), Has.Member(UdonDouble.EditorOnlyMarker),
                "The Editor compile left the marker out.");

            // Act
            RequestBuild();

            // Assert
            Assert.That(ProgramHeap.Strings(typeof(UdonDouble)), Has.No.Member(UdonDouble.EditorOnlyMarker));
        }

        // UdonSharp runs one compile at a time and queues the last one asked for meanwhile. CompileSync
        // finishes the compile under way, asks for its own and finishes the compile under way again,
        // which is the queued one when there was one. So UdonSharpUploadCompile compiles for the
        // Editor first, after which nothing is queued.
        [Test]
        public void CompileSync_CompileUnderWayAndAnotherQueued_ReturnsBeforeItsOwnCompileLogsItsErrors()
        {
            // Arrange
            using var programAsset = new TemporaryProgramAsset(typeof(UploadFailingBehaviour));
            UdonSharpCompilerV1.Compile();
            UdonSharpCompilerV1.Compile();

            // Act
            Record(() => UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions { IsEditorBuild = false }));

            // Assert
            Assert.That(Errors, Is.Empty);
            Record(() => PrivateFieldAccess.InvokeStatic(typeof(UdonSharpCompilerV1), "WaitForCompile"));
            Assert.That(Errors, Has.Some.Contains(UploadFailingBehaviour.UploadError));
        }

        private IEnumerable<string> Messages => _logs.Select(log => log.message);

        private IEnumerable<string> Errors => _logs.Where(log => log.type == LogType.Error).Select(log => log.message);

        // Runs the callback UdonSharp registers with the VRChat SDK for a world build, which UdonSharp
        // keeps internal.
        private bool RequestBuild()
        {
            Type buildCompile = typeof(UdonSharpProgramAsset).Assembly.GetType("UdonSharpEditor.UdonSharpBuildCompile");
            var callback = (IVRCSDKBuildRequestedCallback)Activator.CreateInstance(buildCompile, nonPublic: true);
            bool buildGoesOn = false;
            Record(() => buildGoesOn = callback.OnBuildRequested(VRCSDKRequestedBuildType.Scene, EditorUserBuildSettings.activeBuildTarget));
            return buildGoesOn;
        }

        private void Record(Action compile)
        {
            Application.LogCallback record = (message, stackTrace, type) => _logs.Add((type, message));
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

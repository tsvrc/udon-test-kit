using System.Collections.Generic;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;
using Tsvrc.UdonTestKit.Tests;
using UdonSharp.Compiler;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    public class UdonSharpUploadCompileTests
    {
        [Test]
        public void Run_EveryScriptInAPlayerAssembly_ReturnsNoErrors()
        {
            Assert.That(UdonSharpUploadCompile.Run(), Is.Empty);
        }

        // The errors are logged as well, and only Run expecting them keeps them from failing the test at
        // its end.
        [Test]
        public void Run_ProgramAssetOfATestsOnlyScript_ReturnsTheErrorAndExpectsIt()
        {
            // Arrange
            using var programAsset = new TemporaryProgramAsset(typeof(TestsOnlyBehaviour));

            // Act
            IReadOnlyList<string> errors = UdonSharpUploadCompile.Run();

            // Assert
            Assert.That(errors, Has.Some.Contains("TestsOnlyBehaviour.cs' does not belong to a U# assembly"));
        }

        [Test]
        public void Run_ProgramThatFailsOnlyForUpload_ReturnsItsError()
        {
            // Arrange
            using var programAsset = new TemporaryProgramAsset(typeof(UploadFailingBehaviour));

            // Act
            IReadOnlyList<string> errors = UdonSharpUploadCompile.Run();

            // Assert
            Assert.That(errors, Has.Some.Contains(UploadFailingBehaviour.UploadError));
        }

        [Test]
        public void Run_CompileUnderWayAndAnotherQueued_ReturnsTheErrorsOfItsOwnCompile()
        {
            // Arrange
            using var programAsset = new TemporaryProgramAsset(typeof(UploadFailingBehaviour));
            UdonSharpCompilerV1.Compile();
            UdonSharpCompilerV1.Compile();

            // Act
            IReadOnlyList<string> errors = UdonSharpUploadCompile.Run();

            // Assert
            Assert.That(errors, Has.Some.Contains(UploadFailingBehaviour.UploadError));
        }

        [Test]
        public void Run_ErrorLoggedAfterItReturns_IsLeftOut()
        {
            // Arrange
            IReadOnlyList<string> errors = UdonSharpUploadCompile.Run();
            LogAssert.Expect(LogType.Error, "A later error");

            // Act
            Debug.LogError("A later error");

            // Assert
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void Run_AfterTheUploadCompile_LeavesTheEditorPrograms()
        {
            // Act
            UdonSharpUploadCompile.Run();

            // Assert
            Assert.That(ProgramHeap.Strings(typeof(UdonDouble)), Has.Member(UdonDouble.EditorOnlyMarker));
        }
    }
}

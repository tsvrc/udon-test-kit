using System.Collections.Generic;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;
using Tsvrc.UdonTestKit.Tests;
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

        [Test]
        public void Run_ProgramAssetOfATestsOnlyScript_ReturnsTheError()
        {
            // Arrange
            using var programAsset = new TestsOnlyProgramAsset();
            LogAssert.ignoreFailingMessages = true;

            // Act
            IReadOnlyList<string> errors = UdonSharpUploadCompile.Run();

            // Assert
            Assert.That(errors, Has.Some.Contains("TestsOnlyBehaviour.cs' does not belong to a U# assembly"));
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
            Assert.That(ProgramHeap.Strings(typeof(UdonProbe)), Has.Member(UdonProbe.EditorOnlyMarker));
        }
    }
}

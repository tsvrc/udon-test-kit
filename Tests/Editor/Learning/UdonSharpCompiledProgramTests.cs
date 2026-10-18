using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;
using Tsvrc.UdonTestKit.Tests;
using UdonSharp.Compiler;
using UnityEditor;
using VRC.Udon.ProgramSources;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // UdonSharp keeps the program it compiles for a program asset in Assets/SerializedUdonPrograms,
    // under the GUID of the program asset, and leaves it there when the program asset is deleted. So
    // the test helpers that create program assets delete those programs too, and a test run leaves
    // none behind.
    public class UdonSharpCompiledProgramTests
    {
        private string _program;

        [TearDown]
        public void TearDown()
        {
            if (_program != null)
            {
                AssetDatabase.DeleteAsset(_program);
                _program = null;
            }
            UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions());
        }

        [Test]
        public void CompileSync_ProgramAsset_KeepsItsProgramUnderItsGuid()
        {
            // Arrange
            using var programAsset = new TemporaryProgramAsset(typeof(TestsOnlyBehaviour));

            // Act
            UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions());

            // Assert
            Assert.That(AssetDatabase.LoadAssetAtPath<SerializedUdonProgramAsset>(CompiledProgramOf(programAsset.Path)), Is.Not.Null);
        }

        [Test]
        public void DeleteAsset_CompiledProgramAsset_LeavesItsProgram()
        {
            // Arrange
            using var programAsset = new TemporaryProgramAsset(typeof(TestsOnlyBehaviour));
            UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions());
            _program = CompiledProgramOf(programAsset.Path);

            // Act
            AssetDatabase.DeleteAsset(programAsset.Path);

            // Assert
            Assert.That(AssetDatabase.LoadAssetAtPath<SerializedUdonProgramAsset>(_program), Is.Not.Null);
        }

        private static string CompiledProgramOf(string programAsset) =>
            $"Assets/SerializedUdonPrograms/{AssetDatabase.AssetPathToGUID(programAsset)}.asset";
    }
}

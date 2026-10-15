using NUnit.Framework;
using Tsvrc.UdonTestKit.Tests;
using UdonSharpEditor;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // UdonSharpEditorUtility.GetUdonSharpProgramAsset finds the program asset of an UdonSharp class,
    // and returns null for a class without one. So UdonHost.ProgramAssetOf turns null into an error
    // that says how to make one.
    public class UdonSharpProgramAssetLookupTests
    {
        [Test]
        public void GetUdonSharpProgramAsset_ClassWithAProgramAsset_ReturnsIt()
        {
            Assert.That(UdonSharpEditorUtility.GetUdonSharpProgramAsset(typeof(UdonProbe)).sourceCsScript.GetClass(),
                Is.EqualTo(typeof(UdonProbe)));
        }

        [Test]
        public void GetUdonSharpProgramAsset_ClassWithoutAProgramAsset_ReturnsNull()
        {
            Assert.That(UdonSharpEditorUtility.GetUdonSharpProgramAsset(typeof(TestsOnlyBehaviour)), Is.Null);
        }
    }
}

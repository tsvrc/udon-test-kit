using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;
using Tsvrc.UdonTestKit.Tests;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    public class UdonHostTests
    {
        [Test]
        public void Create_ClassWithAProgramAssetInATestsOnlyAssembly_ThrowsAnErrorThatSaysWhereItGoes()
        {
            // Arrange
            using var programAsset = new TestsOnlyProgramAsset();
            using var udon = new UdonHost();

            // Act and assert
            Assert.That(() => udon.Create<TestsOnlyBehaviour>("Double"),
                Throws.InvalidOperationException.With.Message.Contains("Move it to an assembly in player builds"));
        }
    }
}

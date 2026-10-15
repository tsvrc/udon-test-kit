using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;
using Tsvrc.UdonTestKit.Tests;
using UdonSharp;
using UdonSharp.Localization;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    // The errors name the UdonSharp menu items and field to use, read here from UdonSharp itself.
    public class UdonHostTests
    {
        [Test]
        public void Create_ClassWithoutAProgramAsset_ThrowsAnErrorThatSaysHowToCreateOne()
        {
            // Arrange
            using var udon = new UdonHost();
            string menu = "Assets/Create/" + typeof(UdonSharpProgramAsset).GetCustomAttribute<CreateAssetMenuAttribute>().menuName;

            // Act and assert
            Assert.That(() => udon.Create<TestsOnlyBehaviour>("Double"), Throws.InvalidOperationException.With.Message.EqualTo(
                $"TestsOnlyBehaviour has no UdonSharp program asset. Create one with {MenuText(menu)} and set its {Loc.Get(LocStr.UI_SourceScript)}."));
        }

        [Test]
        public void Create_ClassWithAProgramAssetInATestsOnlyAssembly_ThrowsAnErrorThatSaysWhereItGoes()
        {
            // Arrange
            using var programAsset = new TestsOnlyProgramAsset();
            using var udon = new UdonHost();
            string menu = TypeCache.GetMethodsWithAttribute<MenuItem>()
                .Where(method => method.DeclaringType == typeof(UdonSharpAssemblyDefinition))
                .Select(method => method.GetCustomAttribute<MenuItem>().menuItem)
                .Single();

            // Act and assert
            Assert.That(() => udon.Create<TestsOnlyBehaviour>("Double"), Throws.InvalidOperationException.With.Message.EqualTo(
                "TestsOnlyBehaviour has a program asset, but player builds leave out its assembly, " +
                "Tsvrc.UdonTestKit.Tests.TestsOnlyDoubles, so the compile for a world upload fails. Move it to an " +
                $"assembly in player builds, with a U# assembly definition from {MenuText(menu)}."));
        }

        private static string MenuText(string menuPath) => menuPath.Replace("/", " > ");
    }
}

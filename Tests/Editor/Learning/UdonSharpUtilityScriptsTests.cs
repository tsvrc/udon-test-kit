using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Compilation;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // When UdonSharp first imports into a project, it creates its data asset in Assets/UdonSharp and
    // copies its utility scripts into UtilityScripts next to it (UdonSharpDataLocator). They have no
    // assembly definition, so they compile into Assembly-CSharp in every world project. So
    // TestAssemblyCreator leaves them out when it warns about scripts in Assembly-CSharp.
    public class UdonSharpUtilityScriptsTests
    {
        [Test]
        public void UtilityScripts_NextToTheDataAsset_CompileIntoAssemblyCSharp()
        {
            // Arrange
            string dataAsset = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:UdonSharpDataLocator").Single());

            // Act
            Assembly assemblyCSharp = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                .Single(assembly => assembly.name == "Assembly-CSharp");

            // Assert
            Assert.That(dataAsset, Is.EqualTo("Assets/UdonSharp/UdonSharpDataLocator.asset"));
            Assert.That(assemblyCSharp.sourceFiles, Has.Some.StartsWith("Assets/UdonSharp/UtilityScripts/"));
        }
    }
}

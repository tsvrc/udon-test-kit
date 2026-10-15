using System.IO;
using NUnit.Framework;
using UnityEditor;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // FileUtil.GetUniqueTempPathInProject gives a new path in the Temp folder of the project on each
    // call. So BuiltWorld and the kit tests that write files use one, and a run in another project at
    // the same time can't delete them.
    public class UnityFileUtilTests
    {
        [Test]
        public void GetUniqueTempPathInProject_TwoCalls_GiveTwoPathsInTheTempFolderOfTheProject()
        {
            // Act
            string first = Path.GetFullPath(FileUtil.GetUniqueTempPathInProject());
            string second = Path.GetFullPath(FileUtil.GetUniqueTempPathInProject());

            // Assert
            string projectTemp = Path.GetFullPath("Temp") + Path.DirectorySeparatorChar;
            Assert.That(first, Does.StartWith(projectTemp));
            Assert.That(second, Does.StartWith(projectTemp).And.Not.EqualTo(first));
        }
    }
}

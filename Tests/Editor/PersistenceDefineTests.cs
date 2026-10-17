using NUnit.Framework;
using Tsvrc.UdonTestKit.Editor.Tests.Helpers;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    public class PersistenceDefineTests
    {
        // The first compile of a project with the kit runs without the define.
        [Test]
        public void Compile_KitWithoutThePersistenceDefine_HasNoErrors()
        {
            Assert.That(CompileWithoutDefine.Errors(CompileWithoutDefine.Of("Tsvrc.UdonTestKit", "VRC_ENABLE_PLAYER_PERSISTENCE")), Is.Empty);
        }
    }
}

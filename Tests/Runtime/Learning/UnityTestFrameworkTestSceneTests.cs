using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace Tsvrc.UdonTestKit.Tests.Learning
{
    // Unity Test Framework runs Play Mode tests in a scene named InitTestScene and digits, unless a
    // test loads a scene of its own. So TestSceneSavedData deletes what ClientSim saved for scenes
    // with such names when a run ends.
    public class UnityTestFrameworkTestSceneTests
    {
        [Test]
        public void ActiveScene_PlayModeTest_IsNamedInitTestSceneAndDigits()
        {
            Assert.That(SceneManager.GetActiveScene().name, Does.Match(@"^InitTestScene\d+$"));
        }
    }
}

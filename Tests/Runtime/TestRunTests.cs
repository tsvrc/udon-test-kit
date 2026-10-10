using NUnit.Framework;

namespace Tsvrc.UdonTestKit.Tests
{
    public class TestRunTests
    {
        [Test]
        public void StartedInThisEditorSession_InAPlayModeTest_IsTrue()
        {
            Assert.That(TestRun.StartedInThisEditorSession, Is.True);
        }
    }
}

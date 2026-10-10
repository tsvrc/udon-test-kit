using NUnit.Framework;

namespace UdonTestKit.Tests
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

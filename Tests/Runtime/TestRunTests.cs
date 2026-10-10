using NUnit.Framework;

namespace Tsvrc.TestKit.Tests
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

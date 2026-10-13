using NUnit.Framework;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    public class TestRunTests
    {
        private bool _startedBefore;

        [SetUp]
        public void SetUp() => _startedBefore = TestRun.StartedInThisEditorSession;

        [TearDown]
        public void TearDown() => TestRun.StartedInThisEditorSession = _startedBefore;

        [Test]
        public void StartedInThisEditorSession_DuringARun_IsTrue()
        {
            Assert.That(TestRun.StartedInThisEditorSession, Is.True);
        }

        [Test]
        public void IsActive_DuringARun_IsTrue()
        {
            Assert.That(TestRun.IsActive, Is.True);
        }

        [Test]
        public void RunFinished_AfterRunStarted_ClearsIt()
        {
            // Arrange
            var callbacks = new TestRun.Callbacks();
            callbacks.RunStarted(null);

            // Act
            callbacks.RunFinished(null);

            // Assert
            Assert.That(TestRun.StartedInThisEditorSession, Is.False);
        }

        [Test]
        public void OnError_AfterRunStarted_ClearsIt()
        {
            // Arrange
            var callbacks = new TestRun.Callbacks();
            callbacks.RunStarted(null);

            // Act
            callbacks.OnError("A run that can't finish.");

            // Assert
            Assert.That(TestRun.StartedInThisEditorSession, Is.False);
        }

        [TestCase("Unity.exe -batchmode -runTests -testPlatform EditMode", ExpectedResult = true)]
        [TestCase("Unity.exe -batchmode -projectPath World", ExpectedResult = false)]
        public bool IsTestProcess_CommandLine_IsTrueOnlyWithRunTests(string commandLine)
        {
            return TestRun.IsTestProcess(commandLine.Split(' '));
        }
    }
}

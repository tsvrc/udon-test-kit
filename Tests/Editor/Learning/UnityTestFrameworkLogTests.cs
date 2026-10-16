using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // Unity Test Framework fails a test on any error log it didn't expect, which is how ClientSim's
    // settings window fails tests in batch mode. LogAssert.Expect also marks an error log expected
    // after it came, so UdonSharpUploadCompile.Run expects the errors it returns once the compile
    // has logged them.
    public class UnityTestFrameworkLogTests
    {
        private const string UnexpectedError = "An error log no test expected.";
        private const string LaterExpectedError = "An error log expected after it came.";

        [Test]
        public void Expect_AfterTheErrorLog_MarksItExpected()
        {
            // Arrange
            Debug.LogError(LaterExpectedError);

            // Act
            LogAssert.Expect(LogType.Error, LaterExpectedError);

            // Assert
            Assert.DoesNotThrow(LogAssert.NoUnexpectedReceived);
        }

        [Test]
        public void NoUnexpectedReceived_AfterUnexpectedErrorLog_ThrowsUnhandledLogMessageException()
        {
            // Arrange
            Assert.DoesNotThrow(LogAssert.NoUnexpectedReceived, "Something was already logged unexpectedly.");
            Debug.LogError(UnexpectedError);

            // Act
            Exception failure = Assert.Catch(() => LogAssert.NoUnexpectedReceived());

            // Assert
            Assert.That(failure.GetType().Name, Is.EqualTo("UnhandledLogMessageException"));

            // Marks the log expected after the fact, so this test's own end-of-test check passes.
            LogAssert.Expect(LogType.Error, UnexpectedError);
        }
    }
}

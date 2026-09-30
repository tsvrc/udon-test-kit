using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UdonTestKit.Tests.EditMode.Problems
{
    // Problem: Unity Test Framework treats an error log that no test expected as a failure, raising
    // UnhandledLogMessageException ("Unhandled log message: ... Use LogAssert.Expect").
    // It's why the errors ClientSim's settings window logs in batch mode fail tests
    // (PlayMode/Problems/ClientSimInvalidSettingsProblemTests). Solved by
    // ClientSimProjectSettingsPreflight, tested in Solutions/ClientSimProjectSettingsPreflightTests.
    // A failure here means Unity Test Framework changed, so the preflight may no longer be needed.
    public class UnityTestFrameworkLogProblemTests
    {
        private const string UnexpectedError = "An error log no test expected.";

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

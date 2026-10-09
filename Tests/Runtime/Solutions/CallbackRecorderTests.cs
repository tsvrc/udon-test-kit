using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UdonTestKit.Tests.Solutions
{
    // Solution: CallbackRecorder also works in Play Mode tests. Its full behavior is tested in
    // Editor/Solutions/CallbackRecorderTests.
    public class CallbackRecorderTests
    {
        private GameObject _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
                Object.DestroyImmediate(_host);
        }

        [Test]
        public void SendCustomEvent_InPlayMode_CallsTheCallback()
        {
            // Arrange
            _host = new GameObject(nameof(CallbackRecorder));
            var recorder = _host.AddComponent<CallbackRecorder>();
            recorder.Log = new List<string>();

            // Act
            recorder.SendCustomEvent(nameof(CallbackRecorder.CallbackB));

            // Assert
            Assert.That(recorder.Log, Is.EqualTo(new[] { nameof(CallbackRecorder) + ".CallbackB" }));
        }
    }
}

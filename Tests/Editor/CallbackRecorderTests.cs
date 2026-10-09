using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UdonTestKit.Editor.Tests
{
    public class CallbackRecorderTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject created in _created)
                if (created != null)
                    Object.DestroyImmediate(created);
            _created.Clear();
        }

        [Test]
        public void AddComponent_FromEditModeTest_AddsTheRecorder()
        {
            // Act
            CallbackRecorder recorder = Create();

            // Assert
            Assert.That(recorder != null, "AddComponent didn't add a CallbackRecorder.");
        }

        [Test]
        public void SendCustomEvent_CallbackAName_CallsCallbackA()
        {
            // Arrange
            CallbackRecorder recorder = Create();

            // Act
            recorder.SendCustomEvent(nameof(CallbackRecorder.CallbackA));

            // Assert
            Assert.That(recorder.CallbackACount, Is.EqualTo(1));
        }

        [Test]
        public void CallbackA_Called_CountsCallbackAOnly()
        {
            // Arrange
            CallbackRecorder recorder = Create();

            // Act
            recorder.CallbackA();

            // Assert
            Assert.That(recorder.CallbackACount, Is.EqualTo(1));
            Assert.That(recorder.CallbackBCount, Is.Zero);
        }

        [Test]
        public void CallbackB_Called_CountsCallbackBOnly()
        {
            // Arrange
            CallbackRecorder recorder = Create();

            // Act
            recorder.CallbackB();

            // Assert
            Assert.That(recorder.CallbackACount, Is.Zero);
            Assert.That(recorder.CallbackBCount, Is.EqualTo(1));
        }

        [Test]
        public void Callbacks_WithLog_AppendGameObjectNameAndCallback()
        {
            // Arrange
            CallbackRecorder recorder = Create("MyRecorder");
            recorder.Log = new List<string>();

            // Act
            recorder.CallbackA();
            recorder.CallbackB();

            // Assert
            Assert.That(recorder.Log, Is.EqualTo(new[] { "MyRecorder.CallbackA", "MyRecorder.CallbackB" }));
        }

        [Test]
        public void Callbacks_SharedLogAcrossRecorders_KeepCallOrder()
        {
            // Arrange
            var log = new List<string>();
            CallbackRecorder first = Create("First");
            CallbackRecorder second = Create("Second");
            first.Log = log;
            second.Log = log;

            // Act
            first.CallbackA();
            second.CallbackA();
            first.CallbackB();

            // Assert
            Assert.That(log, Is.EqualTo(new[] { "First.CallbackA", "Second.CallbackA", "First.CallbackB" }));
        }

        private CallbackRecorder Create(string name = nameof(CallbackRecorder))
        {
            var host = new GameObject(name);
            _created.Add(host);
            return host.AddComponent<CallbackRecorder>();
        }
    }
}

using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tsvrc.TestKit.Editor.Tests
{
    public class CallbackSpyTests
    {
        private GameObject _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
            {
                Object.DestroyImmediate(_host);
            }
        }

        [Test]
        public void SendCustomEvent_CallbackAInAnEditModeTest_IsRecorded()
        {
            // Arrange
            using var calls = new CallLog();
            CallbackSpy spy = calls.CreateSpy();

            // Act
            spy.SendCustomEvent(nameof(CallbackSpy.CallbackA));

            // Assert
            Assert.That(calls, Is.EqualTo(new[] { "Spy.CallbackA" }));
        }

        [Test]
        public void Callbacks_SeveralSpies_AreRecordedInTheOrderTheyArrived()
        {
            // Arrange
            using var calls = new CallLog();
            CallbackSpy first = calls.CreateSpy("First");
            CallbackSpy second = calls.CreateSpy("Second");

            // Act
            first.CallbackA();
            second.CallbackA();
            first.CallbackB();

            // Assert
            Assert.That(calls, Is.EqualTo(new[] { "First.CallbackA", "Second.CallbackA", "First.CallbackB" }));
        }

        [Test]
        public void Clear_AfterACall_ForgetsIt()
        {
            // Arrange
            using var calls = new CallLog();
            calls.CreateSpy().CallbackA();

            // Act
            calls.Clear();

            // Assert
            Assert.That(calls, Is.Empty);
        }

        [Test]
        public void Dispose_AfterCreateSpy_DestroysItsGameObject()
        {
            // Arrange
            var calls = new CallLog();
            GameObject host = calls.CreateSpy().gameObject;

            // Act
            calls.Dispose();

            // Assert
            Assert.That(host == null, "The spy's GameObject outlived its CallLog.");
        }

        [Test]
        public void Dispose_SpyAlreadyDestroyed_DoesNotThrow()
        {
            // Arrange
            var calls = new CallLog();
            Object.DestroyImmediate(calls.CreateSpy().gameObject);

            // Act and Assert
            Assert.DoesNotThrow(calls.Dispose);
        }

        [Test]
        public void CallbackA_SpyNotCreatedByACallLog_ThrowsNamingCreateSpy()
        {
            // Arrange
            _host = new GameObject(nameof(CallbackSpyTests));
            var spy = _host.AddComponent<CallbackSpy>();

            // Act
            var failure = Assert.Throws<InvalidOperationException>(spy.CallbackA);

            // Assert
            Assert.That(failure.Message, Does.Contain("CallLog.CreateSpy"));
        }
    }
}

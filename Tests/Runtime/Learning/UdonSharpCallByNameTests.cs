using System.Collections;
using NUnit.Framework;
using UdonTestKit.Tests.Helpers;
using UnityEngine;
using UnityEngine.TestTools;

namespace UdonTestKit.Tests.Learning
{
    // Outside Udon, as in a test, SendCustomEvent calls the public method with that exact name and
    // silently ignores a name it doesn't have, and delayed events never arrive. So CallbackSpy
    // declares each callback as a real method, and a test checks what its CallLog recorded rather
    // than the absence of errors.
    public class UdonSharpCallByNameTests
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
        public void SendCustomEvent_PublicMethodName_CallsIt()
        {
            TargetBehaviour target = CreateTarget();

            target.SendCustomEvent(nameof(TargetBehaviour.Count));

            Assert.That(target.CountCalls, Is.EqualTo(1));
        }

        [Test]
        public void SendCustomEvent_NameItDoesNotHave_IsSilentlyIgnored()
        {
            TargetBehaviour target = CreateTarget();

            Assert.DoesNotThrow(() => target.SendCustomEvent("NoSuchMethod"));
        }

        [UnityTest]
        public IEnumerator SendCustomEventDelayedFrames_OutsideUdon_NeverCallsTheMethod()
        {
            // Arrange
            TargetBehaviour target = CreateTarget();

            // Act
            target.SendCustomEventDelayedFrames(nameof(TargetBehaviour.Count), 0);
            yield return null;
            yield return null;

            // Assert
            Assert.That(target.CountCalls, Is.Zero);
        }

        private TargetBehaviour CreateTarget()
        {
            _host = new GameObject(nameof(UdonSharpCallByNameTests));
            return _host.AddComponent<TargetBehaviour>();
        }
    }
}

using System.Collections;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Tests.Helpers;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tsvrc.UdonTestKit.Tests.Learning
{
    // Outside Udon, as in a test, SendCustomEvent calls only a public method with that exact name and
    // no parameters, silently ignores any other name, and delayed events never arrive. So
    // CallbackSpy declares each callback as a public method without parameters, and a test checks
    // what its CallLog recorded, since a misspelled name raises no error.
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

        [Test]
        public void SendCustomEvent_PrivateMethodName_IsSilentlyIgnored()
        {
            TargetBehaviour target = CreateTarget();

            target.SendCustomEvent(TargetBehaviour.PrivateCountName);

            Assert.That(target.CountCalls, Is.Zero);
        }

        [Test]
        public void SendCustomEvent_MethodWithAParameter_IsSilentlyIgnored()
        {
            TargetBehaviour target = CreateTarget();

            target.SendCustomEvent(nameof(TargetBehaviour.CountBy));

            Assert.That(target.CountCalls, Is.Zero);
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

        [UnityTest]
        public IEnumerator SendCustomEventDelayedSeconds_OutsideUdon_NeverCallsTheMethod()
        {
            // Arrange
            TargetBehaviour target = CreateTarget();

            // Act
            target.SendCustomEventDelayedSeconds(nameof(TargetBehaviour.Count), 0f);
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

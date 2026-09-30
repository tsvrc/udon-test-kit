using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UdonTestKit.Tests.PlayMode.Problems
{
    // Problem: Object.Destroy doesn't destroy right away; the object is gone only from the next frame.
    // It's why Problems/ClientSimPersistenceLeakProblemTests yields a frame after ClientSim's teardown
    // before asserting a leak, so a deferred cleanup isn't mistaken for one.
    // A failure here means Unity changed, so that wait may no longer be needed.
    public class UnityObjectLifetimeProblemTests
    {
        private GameObject _gameObject;

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
                Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void Destroy_SameFrame_ObjectStillExists()
        {
            // Arrange
            _gameObject = new GameObject(nameof(UnityObjectLifetimeProblemTests));

            // Act
            Object.Destroy(_gameObject);

            // Assert
            Assert.That(_gameObject != null, "Destroy took effect in the same frame.");
        }

        [UnityTest]
        public IEnumerator Destroy_NextFrame_ObjectIsGone()
        {
            // Arrange
            _gameObject = new GameObject(nameof(UnityObjectLifetimeProblemTests));

            // Act
            Object.Destroy(_gameObject);
            yield return null;

            // Assert
            Assert.That(_gameObject == null, "The object survived Destroy past the next frame.");
        }
    }
}

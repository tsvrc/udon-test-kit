using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UdonTestKit.Tests.Learning
{
    // Object.Destroy takes effect only from the next frame, so a test yields a frame before
    // checking that something survived.
    public class UnityObjectLifetimeTests
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
            _gameObject = new GameObject(nameof(UnityObjectLifetimeTests));

            // Act
            Object.Destroy(_gameObject);

            // Assert
            Assert.That(_gameObject != null, "Destroy took effect in the same frame.");
        }

        [UnityTest]
        public IEnumerator Destroy_NextFrame_ObjectIsGone()
        {
            // Arrange
            _gameObject = new GameObject(nameof(UnityObjectLifetimeTests));

            // Act
            Object.Destroy(_gameObject);
            yield return null;

            // Assert
            Assert.That(_gameObject == null, "The object survived Destroy past the next frame.");
        }
    }
}

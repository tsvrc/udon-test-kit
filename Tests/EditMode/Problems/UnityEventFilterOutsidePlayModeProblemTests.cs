using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using VRC.Core;

namespace UdonTestKit.Tests.EditMode.Problems
{
    // Problem: the VRChat SDK's UnityEventFilter can't be used outside Play Mode.
    // It's why every UnityEventFilter test (Problems and Solutions) is a PlayMode test.
    // A failure here means the SDK changed, and those tests could move to EditMode.
    public class UnityEventFilterOutsidePlayModeProblemTests
    {
        private GameObject _target;

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_target);

        [Test]
        public void IsTargetPermitted_OutsidePlayMode_Throws()
        {
            // Arrange
            _target = new GameObject(nameof(UnityEventFilterOutsidePlayModeProblemTests));
            MethodInfo isTargetPermitted = typeof(UnityEventFilter).GetMethod("IsTargetPermitted", BindingFlags.NonPublic | BindingFlags.Static);
            Assume.That(isTargetPermitted, Is.Not.Null, "UnityEventFilter.IsTargetPermitted not found. Has the VRChat SDK changed?");

            // Act and Assert
            Assert.Catch(() => isTargetPermitted.Invoke(null, new object[] { _target, "SetActive" }));
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using VRC.Core;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // The VRChat SDK's UnityEventFilter throws outside Play Mode, so its other tests are Play Mode tests.
    public class UnityEventFilterOutsidePlayModeTests
    {
        private GameObject _target;

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_target);

        [Test]
        public void IsTargetPermitted_OutsidePlayMode_Throws()
        {
            // Arrange
            _target = new GameObject(nameof(UnityEventFilterOutsidePlayModeTests));
            MethodInfo isTargetPermitted = typeof(UnityEventFilter).GetMethod("IsTargetPermitted", BindingFlags.NonPublic | BindingFlags.Static);
            Assume.That(isTargetPermitted, Is.Not.Null, "UnityEventFilter.IsTargetPermitted not found. Has the VRChat SDK changed?");

            // Act and Assert
            Assert.Catch(() => isTargetPermitted.Invoke(null, new object[] { _target, "SetActive" }));
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UdonTestKit.Tests.Helpers;

namespace UdonTestKit.Tests.Solutions
{
    // Solution: UnityEventFilterAllowlistFixup makes the VRChat SDK's UnityEventFilter permit Unity
    // Test Framework's four listener types and their four runner methods. The problem is proven in
    // Problems/UnityEventFilterProblemTests. Nothing here applies the fixup: it already ran at domain load.
    public class UnityEventFilterAllowlistFixupTests
    {
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();

        private static IEnumerable<string> ListenerTypeNames => UnityEventFilterAllowlistFixup.ListenerTypeNames;
        private static IEnumerable<string> RunnerMethodNames => UnityEventFilterAllowlistFixup.AllowedMethodNames;

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object created in _created)
                if (created != null)
                    UnityEngine.Object.DestroyImmediate(created);
            _created.Clear();
        }

        [Test]
        public void IsTargetPermitted_AllowlistedListenerType_PermitsEachRunnerMethod(
            [ValueSource(nameof(ListenerTypeNames))] string typeName,
            [ValueSource(nameof(RunnerMethodNames))] string methodName)
        {
            // Arrange
            UnityEngine.Object listener = UnityEventFilterAccess.CreateListener(UnityEventFilterAccess.FindType(typeName), _created);

            // Act
            bool permitted = UnityEventFilterAccess.IsTargetPermitted(listener, methodName);

            // Assert
            Assert.That(permitted, Is.True);
        }
    }
}

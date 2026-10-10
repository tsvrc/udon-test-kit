using System.Collections.Generic;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Tests.Helpers;

namespace Tsvrc.UdonTestKit.Tests
{
    // Nothing here applies the allowlist: it ran when the editor loaded.
    public class UnityEventFilterAllowlistTests
    {
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();

        private static IEnumerable<string> ListenerTypeNames => UnityEventFilterAllowlist.ListenerTypeNames;
        private static IEnumerable<string> RunnerMethodNames => UnityEventFilterAllowlist.AllowedMethodNames;

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object created in _created)
            {
                if (created != null)
                {
                    UnityEngine.Object.DestroyImmediate(created);
                }
            }
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

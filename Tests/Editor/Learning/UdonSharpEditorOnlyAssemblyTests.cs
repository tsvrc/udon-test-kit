using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Tests.Helpers;
using UnityEngine;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // Unity won't add a behaviour from an Editor-only assembly to a GameObject, so the kit's
    // assembly, which holds CallbackSpy, targets every platform.
    public class UdonSharpEditorOnlyAssemblyTests
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
        public void AddComponent_BehaviourFromEditorOnlyAssembly_IsRefused()
        {
            // Arrange
            _host = new GameObject(nameof(UdonSharpEditorOnlyAssemblyTests));
            EditorOnlyBehaviour added = null;

            // Act
            List<(LogType Type, string Message)> logs = Logs.During(() => added = _host.AddComponent<EditorOnlyBehaviour>());

            // Assert
            Assert.That(added == null, "AddComponent added a behaviour from an Editor-only assembly.");
            Assert.That(logs.Select(log => log.Message), Has.Some.Contains("because it is an editor script"));
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
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
            var logs = new List<string>();
            Application.LogCallback record = (message, stackTrace, type) => logs.Add(message);
            Application.logMessageReceived += record;

            // Act
            EditorOnlyBehaviour added;
            try
            {
                added = _host.AddComponent<EditorOnlyBehaviour>();
            }
            finally
            {
                Application.logMessageReceived -= record;
            }

            // Assert
            Assert.That(added == null, "AddComponent added a behaviour from an Editor-only assembly.");
            Assert.That(logs, Has.Some.Contains("because it is an editor script"));
        }
    }
}

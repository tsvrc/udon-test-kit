using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UdonTestKit.Tests.EditMode.Problems
{
    // Problem: Unity refuses to AddComponent a behaviour from an assembly compiled only for the
    // Editor, so a test double kept in an Editor-only test assembly can't be added to a GameObject.
    // Solved by the UdonTestKit.Behaviours assembly targeting every platform, tested in
    // Solutions/CallbackRecorderTests. A failure here means Unity changed, so that may no longer be needed.
    public class UdonSharpEditorOnlyAssemblyProblemTests
    {
        private GameObject _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
                Object.DestroyImmediate(_host);
        }

        [Test]
        public void AddComponent_BehaviourFromEditorOnlyAssembly_IsRefused()
        {
            // Arrange
            _host = new GameObject(nameof(UdonSharpEditorOnlyAssemblyProblemTests));
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

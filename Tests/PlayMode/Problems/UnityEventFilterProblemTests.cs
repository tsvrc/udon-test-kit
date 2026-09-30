#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UdonTestKit.ClientSim;
using UdonTestKit.Tests.PlayMode.Helpers;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using VRC.Core;

namespace UdonTestKit.Tests.PlayMode.Problems
{
    // Problem: when Play Mode starts, the VRChat SDK's UnityEventFilter strips persistent UnityEvent
    // listeners it doesn't permit, and a stripped listener is never called. Unity Test Framework's
    // Play Mode test runner calls its callbacks through listeners of four types the filter denies.
    // Solved by UnityEventFilterAllowlistFixup, tested in Solutions/UnityEventFilterAllowlistFixupTests.
    // A failure here means the SDK or Unity Test Framework changed, so the fixup may no longer be needed.
    public class UnityEventFilterProblemTests
    {
        private const string TestRunnerTypeName = "UnityEngine.TestTools.TestRunner.PlaymodeTestsController";

        // The test runner's event fields that carry its listeners.
        private static readonly string[] TestRunnerEventFieldNames =
        {
            "testStartedEvent", "testFinishedEvent", "runStartedEvent", "runFinishedEvent",
        };

        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();

        private static IEnumerable<string> TestRunnerListenerTypeNames => UnityEventFilterAllowlistFixup.ListenerTypeNames;

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object created in _created)
                if (created != null)
                    UnityEngine.Object.DestroyImmediate(created);
            _created.Clear();
        }

        [Test]
        public void PlayModeStateChanged_InPlayMode_RunsTheFilter()
        {
            // Arrange
            FieldInfo eventField = typeof(EditorApplication).GetField("playModeStateChanged", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo filterOnEntry = typeof(UnityEventFilter).GetMethod("RunFilteringOnPlayModeEntry", BindingFlags.NonPublic | BindingFlags.Static);
            Assume.That(eventField, Is.Not.Null, "EditorApplication.playModeStateChanged's field not found. Has Unity changed?");
            Assume.That(filterOnEntry, Is.Not.Null, "UnityEventFilter.RunFilteringOnPlayModeEntry not found. Has the SDK changed?");

            // Act
            var subscribers = (Delegate)eventField.GetValue(null);

            // Assert
            Assert.That(subscribers?.GetInvocationList().Select(subscriber => subscriber.Method), Has.Member(filterOnEntry));
        }

        [Test]
        public void RunFilteringOnPlayModeEntry_EnteredPlayMode_StripsListenersInLoadedScenes()
        {
            // Arrange
            MethodInfo filterOnEntry = typeof(UnityEventFilter).GetMethod("RunFilteringOnPlayModeEntry", BindingFlags.NonPublic | BindingFlags.Static);
            Assume.That(filterOnEntry, Is.Not.Null, "UnityEventFilter.RunFilteringOnPlayModeEntry not found. Has the SDK changed?");
            EventSource source = CreateEventSource();
            UnityEventTools.AddPersistentListener(source.OnSomething, source.NotOnAllowlist);

            // Act
            filterOnEntry.Invoke(null, new object[] { PlayModeStateChange.EnteredPlayMode });

            // Assert
            Assert.That(source.OnSomething.GetPersistentEventCount(), Is.Zero);
        }

        [Test]
        public void Invoke_AfterFilterStrippedTheListener_DoesNotCallIt()
        {
            // Arrange
            EventSource source = CreateEventSource();
            UnityEventTools.AddPersistentListener(source.OnSomething, source.NotOnAllowlist);
            UnityEventFilter.FilterUnityEvents(source.gameObject);

            // Act
            source.OnSomething.Invoke();

            // Assert
            Assert.That(source.NotOnAllowlistCalls, Is.Zero);
        }

        [Test]
        public void FilterUnityEvents_ListenerTypeNotOnAllowlist_StripsTheListener()
        {
            // Arrange
            EventSource source = CreateEventSource();
            UnityEventTools.AddPersistentListener(source.OnSomething, source.NotOnAllowlist);
            Assume.That(source.OnSomething.GetPersistentEventCount(), Is.EqualTo(1));

            // Act
            UnityEventFilter.FilterUnityEvents(source.gameObject);

            // Assert
            Assert.That(source.OnSomething.GetPersistentEventCount(), Is.Zero);
        }

        [Test]
        public void FilterUnityEvents_ListenerTargetsAllowlistedAudioSourcePlay_KeepsTheListener()
        {
            // Arrange
            EventSource source = CreateEventSource();
            var audioSource = source.gameObject.AddComponent<AudioSource>();
            UnityEventTools.AddPersistentListener(source.OnSomething, audioSource.Play);

            // Act
            UnityEventFilter.FilterUnityEvents(source.gameObject);

            // Assert
            Assert.That(source.OnSomething.GetPersistentEventCount(), Is.EqualTo(1),
                "Filtering is selective: the allowlist decides what survives.");
        }

        [Test]
        public void TestRunner_PersistentListeners_IncludeTheListenerType(
            [ValueSource(nameof(TestRunnerListenerTypeNames))] string typeName)
        {
            // Act
            IReadOnlyCollection<string> listenerTypes = FindTestRunnerListenerTypeNames();

            // Assert
            Assert.That(listenerTypes, Has.Member(typeName),
                $"Unity Test Framework no longer attaches a {typeName} listener to its test runner.");
        }

        [Test]
        public void IsTargetPermitted_ListenerTypeWithoutAllowlistEntry_IsDenied(
            [ValueSource(nameof(TestRunnerListenerTypeNames))] string typeName)
        {
            // Arrange
            Type listenerType = UnityEventFilterAccess.FindType(typeName);
            UnityEngine.Object listener = UnityEventFilterAccess.CreateListener(listenerType, _created);
            IDictionary allowlist = UnityEventFilterAccess.Allowlist();
            object fixupEntry = allowlist[listenerType];
            allowlist.Remove(listenerType);

            try
            {
                // Act
                bool permitted = UnityEventFilterAccess.IsTargetPermitted(listener, UnityEventFilterAccess.RunFinished);

                // Assert
                Assert.That(permitted, Is.False, $"The SDK permits {typeName} without any allowlist entry.");
            }
            finally
            {
                if (fixupEntry != null)
                    allowlist[listenerType] = fixupEntry;
            }
        }

        private EventSource CreateEventSource()
        {
            var host = new GameObject(nameof(UnityEventFilterProblemTests));
            _created.Add(host);
            return host.AddComponent<EventSource>();
        }

        private static IReadOnlyCollection<string> FindTestRunnerListenerTypeNames()
        {
            Type runnerType = UnityEventFilterAccess.FindType(TestRunnerTypeName);
            UnityEngine.Object runner = Resources.FindObjectsOfTypeAll(runnerType).FirstOrDefault();
            Assert.That(runner, Is.Not.Null, "No Unity Test Framework test runner found in Play Mode.");

            var typeNames = new HashSet<string>();
            foreach (string fieldName in TestRunnerEventFieldNames)
            {
                FieldInfo field = runnerType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null, $"{TestRunnerTypeName}.{fieldName} not found. Has Unity Test Framework changed?");

                var unityEvent = (UnityEventBase)field.GetValue(runner);
                for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
                    typeNames.Add(unityEvent.GetPersistentTarget(i).GetType().FullName);
            }

            return typeNames;
        }

        private class EventSource : MonoBehaviour
        {
            public UnityEvent OnSomething = new UnityEvent();
            public int NotOnAllowlistCalls;

            public void NotOnAllowlist()
            {
                NotOnAllowlistCalls++;
            }
        }
    }
}
#endif

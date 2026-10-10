#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Tests.Helpers;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using VRC.Core;

namespace Tsvrc.UdonTestKit.Tests.Learning
{
    // When Play Mode starts, the VRChat SDK strips UnityEvent listeners whose type isn't
    // allowlisted, including the four Unity Test Framework reports results through.
    // UnityEventFilterAllowlist adds those four.
    public class UnityEventFilterTests
    {
        private const string TestRunnerTypeName = "UnityEngine.TestTools.TestRunner.PlaymodeTestsController";
        private const string PlayModeStateChangedField = "m_PlayModeStateChangedEvent";

        // The test runner's event fields that carry its listeners.
        private static readonly string[] TestRunnerEventFieldNames =
        {
            "testStartedEvent", "testFinishedEvent", "runStartedEvent", "runFinishedEvent",
        };

        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();

        private static IEnumerable<string> TestRunnerListenerTypeNames => UnityEventFilterAllowlist.ListenerTypeNames;

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
        public void PlayModeStateChanged_InPlayMode_RunsTheFilter()
        {
            // Arrange
            FieldInfo eventField = typeof(EditorApplication).GetField(PlayModeStateChangedField, BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo filterOnEntry = typeof(UnityEventFilter).GetMethod("RunFilteringOnPlayModeEntry", BindingFlags.NonPublic | BindingFlags.Static);
            Assume.That(eventField, Is.Not.Null, $"EditorApplication.{PlayModeStateChangedField} not found. Has Unity changed?");
            Assume.That(filterOnEntry, Is.Not.Null, "UnityEventFilter.RunFilteringOnPlayModeEntry not found. Has the SDK changed?");

            // Act
            List<MethodInfo> subscribedMethods = EnumerateByReflection(eventField.GetValue(null))
                .OfType<Delegate>()
                .SelectMany(subscriber => subscriber.GetInvocationList())
                .Select(subscriber => subscriber.Method)
                .ToList();

            // Assert
            Assert.That(subscribedMethods, Has.Member(filterOnEntry));
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
            object kitEntry = allowlist[listenerType];
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
                if (kitEntry != null)
                {
                    allowlist[listenerType] = kitEntry;
                }
            }
        }

        private EventSource CreateEventSource()
        {
            var host = new GameObject(nameof(UnityEventFilterTests));
            _created.Add(host);
            return host.AddComponent<EventSource>();
        }

        // The event's store has a GetEnumerator method without implementing IEnumerable, so it's
        // walked the way foreach would, through reflection.
        private static IEnumerable<object> EnumerateByReflection(object store)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo getEnumerator = store.GetType().GetMethod("GetEnumerator", flags);
            Assert.That(getEnumerator, Is.Not.Null, $"{store.GetType().Name}.GetEnumerator not found. Has Unity changed?");

            object enumerator = getEnumerator.Invoke(store, null);
            MethodInfo moveNext = enumerator.GetType().GetMethod("MoveNext", flags);
            PropertyInfo current = enumerator.GetType().GetProperty("Current", flags);
            while ((bool)moveNext.Invoke(enumerator, null))
            {
                yield return current.GetValue(enumerator);
            }
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
                {
                    typeNames.Add(unityEvent.GetPersistentTarget(i).GetType().FullName);
                }
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

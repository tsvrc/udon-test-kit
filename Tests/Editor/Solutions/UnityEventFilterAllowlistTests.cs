using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UdonTestKit.Editor.Tests.Solutions
{
    // Solution: UnityEventFilterAllowlist's own logic: what it adds, and that it warns instead of
    // throwing when the SDK internals it relies on are missing. It runs against stand-in filter types
    // shaped like the SDK's, so every path can be reached. That it works against the real SDK is
    // proven in Runtime/Solutions/UnityEventFilterAllowlistTests, and the problem in
    // Runtime/Problems/UnityEventFilterProblemTests.
    public class UnityEventFilterAllowlistTests
    {
        private static readonly Regex AllowlistWarning = new Regex(@"^\[UdonTestKit\] UnityEventFilterAllowlist: ");

        private static string[] KnownListener => new[] { typeof(KnownListenerType).FullName };

        [SetUp]
        public void SetUp() => WorkingFilter.Entries.Clear();

        [Test]
        public void Apply_NewListenerType_AddsAnEntryPermittingTheFourRunnerMethods()
        {
            // Act
            UnityEventFilterAllowlist.Apply(typeof(WorkingFilter), KnownListener);

            // Assert
            var entry = (WorkingFilter.AllowedMethodFilter)WorkingFilter.Entries[typeof(KnownListenerType)];
            Assert.That(entry.Allowed, Is.EquivalentTo(new[] { "TestStarted", "TestFinished", "RunStarted", "RunFinished" }));
        }

        [Test]
        public void Apply_TypeAlreadyAllowlisted_KeepsTheExistingEntry()
        {
            // Arrange
            var existing = new object();
            WorkingFilter.Entries[typeof(KnownListenerType)] = existing;

            // Act
            UnityEventFilterAllowlist.Apply(typeof(WorkingFilter), KnownListener);

            // Assert
            Assert.That(WorkingFilter.Entries[typeof(KnownListenerType)], Is.SameAs(existing));
        }

        [Test]
        public void Apply_UnknownListenerTypeName_WarnsAndStillAddsTheOthers()
        {
            // Arrange
            LogAssert.Expect(LogType.Warning, AllowlistWarning);

            // Act
            UnityEventFilterAllowlist.Apply(typeof(WorkingFilter), new[] { "No.Such.ListenerType", KnownListener[0] });

            // Assert
            Assert.That(WorkingFilter.Entries.Contains(typeof(KnownListenerType)), Is.True);
        }

        [TestCase(typeof(FilterWithoutAllowlist))]
        [TestCase(typeof(FilterWithNonDictionaryAllowlist))]
        [TestCase(typeof(FilterWithoutEntryType))]
        [TestCase(typeof(FilterThatThrows))]
        public void Apply_SdkInternalsMissing_WarnsInsteadOfThrowing(Type filterType)
        {
            LogAssert.Expect(LogType.Warning, AllowlistWarning);

            Assert.DoesNotThrow(() => UnityEventFilterAllowlist.Apply(filterType, KnownListener));
        }

        // Stand-ins shaped like VRC.Core.UnityEventFilter's internals, each missing one piece.
        private class KnownListenerType
        {
        }

        private static class WorkingFilter
        {
            internal static readonly Hashtable Entries = new Hashtable();
            private static readonly Lazy<IDictionary> _allowedUnityEventTargetTypes = new Lazy<IDictionary>(() => Entries);

            internal class AllowedMethodFilter
            {
                public readonly List<string> Allowed;

                public AllowedMethodFilter(List<string> allowed, List<string> denied)
                {
                    Allowed = allowed;
                }
            }
        }

        private static class FilterWithoutAllowlist
        {
        }

        private static class FilterWithNonDictionaryAllowlist
        {
            private static readonly Lazy<object> _allowedUnityEventTargetTypes = new Lazy<object>(() => new object());
        }

        private static class FilterWithoutEntryType
        {
            private static readonly Lazy<IDictionary> _allowedUnityEventTargetTypes = new Lazy<IDictionary>(() => new Hashtable());
        }

        private static class FilterThatThrows
        {
            private static readonly Lazy<IDictionary> _allowedUnityEventTargetTypes =
                new Lazy<IDictionary>(() => throw new InvalidOperationException("Stand-in failure."));
        }
    }
}

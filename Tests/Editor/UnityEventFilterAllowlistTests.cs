using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Tsvrc.UdonTestKit.Tests.Helpers;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    // Runs against stand-in filter types shaped like the SDK's, so every warning can be reached.
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
        public void Apply_TypeAlreadyAllowlisted_KeepsTheExistingEntryAndWarnsNothing()
        {
            // Arrange
            var existing = new object();
            WorkingFilter.Entries[typeof(KnownListenerType)] = existing;

            // Act
            List<(LogType Type, string Message)> logs = Logs.During(() => UnityEventFilterAllowlist.Apply(typeof(WorkingFilter), KnownListener));

            // Assert: Unity Test Framework doesn't fail a test on a warning, so this collects them.
            Assert.That(WorkingFilter.Entries[typeof(KnownListenerType)], Is.SameAs(existing));
            Assert.That(logs.Messages(LogType.Warning), Is.Empty);
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

        [TestCase(typeof(FilterWithoutAllowlist), "could not find FilterWithoutAllowlist._allowedUnityEventTargetTypes.")]
        [TestCase(typeof(FilterWithNonDictionaryAllowlist), "could not read FilterWithNonDictionaryAllowlist's allowlist dictionary.")]
        [TestCase(typeof(FilterWithoutEntryType), "could not find the FilterWithoutEntryType.AllowedMethodFilter constructor.")]
        public void Apply_SdkInternalsMissing_WarnsWhatIsMissingInsteadOfThrowing(Type filterType, string reason)
        {
            LogAssert.Expect(LogType.Warning, "[UdonTestKit] UnityEventFilterAllowlist: " + reason);

            Assert.DoesNotThrow(() => UnityEventFilterAllowlist.Apply(filterType, KnownListener));
        }

        // The allowlist is read through reflection, which wraps what it throws.
        [Test]
        public void Apply_SdkInternalsThrow_WarnsWithTheExceptionInsteadOfThrowing()
        {
            LogAssert.Expect(LogType.Warning,
                new Regex(@"^\[UdonTestKit\] UnityEventFilterAllowlist: .*InvalidOperationException: Stand-in failure\.", RegexOptions.Singleline));

            Assert.DoesNotThrow(() => UnityEventFilterAllowlist.Apply(typeof(FilterThatThrows), KnownListener));
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UdonTestKit.Editor.Tests.Solutions
{
    // Solution: UnityEventFilterAllowlistFixup's own logic: when it opts in, what it adds, and that
    // it warns instead of throwing when the SDK internals it relies on are missing. It runs against
    // stand-in filter types shaped like the SDK's, so every path can be reached. That it works
    // against the real SDK is proven in Runtime/Solutions/UnityEventFilterAllowlistFixupTests, and
    // the problem in Runtime/Problems/UnityEventFilterProblemTests.
    public class UnityEventFilterAllowlistFixupTests
    {
        private static readonly Regex FixupWarning = new Regex(@"^\[UdonTestKit\] UnityEventFilterAllowlistFixup: ");

        private static Assembly[] OptedIn => new[] { typeof(UnityEventFilterAllowlistFixupTests).Assembly };
        private static string[] KnownListener => new[] { typeof(KnownListenerType).FullName };

        [SetUp]
        public void SetUp() => WorkingFilter.Entries.Clear();

        [Test]
        public void IsOptedIn_AssemblyUsingKitWithoutClientSimTestBaseSubclass_ReturnsTrue()
        {
            // Arrange
            Assembly thisTestAssembly = typeof(UnityEventFilterAllowlistFixupTests).Assembly;
            Assume.That(thisTestAssembly.GetTypes().Where(type => typeof(ClientSimTestBase).IsAssignableFrom(type)), Is.Empty,
                "This test assembly must contain no ClientSimTestBase subclass for the check to mean anything.");

            // Act and Assert
            Assert.That(UnityEventFilterAllowlistFixup.IsOptedIn(new[] { thisTestAssembly }), Is.True);
        }

        [Test]
        public void IsOptedIn_NoAssemblyReferencesKit_ReturnsFalse()
        {
            Assembly[] unrelated = { typeof(Assert).Assembly, typeof(GameObject).Assembly };

            Assert.That(UnityEventFilterAllowlistFixup.IsOptedIn(unrelated), Is.False);
        }

        [Test]
        public void IsOptedIn_OnlyTheKitItself_ReturnsFalse()
        {
            Assembly kit = typeof(UnityEventFilterAllowlistFixup).Assembly;

            Assert.That(UnityEventFilterAllowlistFixup.IsOptedIn(new[] { kit }), Is.False);
        }

        [Test]
        public void Apply_OptedIn_AddsAnEntryPermittingTheFourRunnerMethods()
        {
            // Act
            UnityEventFilterAllowlistFixup.Apply(OptedIn, typeof(WorkingFilter), KnownListener);

            // Assert
            var entry = (WorkingFilter.AllowedMethodFilter)WorkingFilter.Entries[typeof(KnownListenerType)];
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.Allowed, Is.EquivalentTo(new[] { "TestStarted", "TestFinished", "RunStarted", "RunFinished" }));
        }

        [Test]
        public void Apply_NotOptedIn_LeavesTheAllowlistUntouched()
        {
            // Act
            UnityEventFilterAllowlistFixup.Apply(new[] { typeof(GameObject).Assembly }, typeof(WorkingFilter), KnownListener);

            // Assert
            Assert.That(WorkingFilter.Entries, Is.Empty);
        }

        [Test]
        public void Apply_TypeAlreadyAllowlisted_KeepsTheExistingEntry()
        {
            // Arrange
            var existing = new object();
            WorkingFilter.Entries[typeof(KnownListenerType)] = existing;

            // Act
            UnityEventFilterAllowlistFixup.Apply(OptedIn, typeof(WorkingFilter), KnownListener);

            // Assert
            Assert.That(WorkingFilter.Entries[typeof(KnownListenerType)], Is.SameAs(existing));
        }

        [Test]
        public void Apply_UnknownListenerTypeName_WarnsAndStillAddsTheOthers()
        {
            // Arrange
            LogAssert.Expect(LogType.Warning, FixupWarning);

            // Act
            UnityEventFilterAllowlistFixup.Apply(OptedIn, typeof(WorkingFilter), new[] { "No.Such.ListenerType", KnownListener[0] });

            // Assert
            Assert.That(WorkingFilter.Entries.Contains(typeof(KnownListenerType)), Is.True);
        }

        [Test]
        public void Apply_FilterWithoutAllowlistField_WarnsInsteadOfThrowing()
        {
            LogAssert.Expect(LogType.Warning, FixupWarning);

            Assert.DoesNotThrow(() => UnityEventFilterAllowlistFixup.Apply(OptedIn, typeof(FilterWithoutAllowlist), KnownListener));
        }

        [Test]
        public void Apply_AllowlistIsNotADictionary_WarnsInsteadOfThrowing()
        {
            LogAssert.Expect(LogType.Warning, FixupWarning);

            Assert.DoesNotThrow(() => UnityEventFilterAllowlistFixup.Apply(OptedIn, typeof(FilterWithNonDictionaryAllowlist), KnownListener));
        }

        [Test]
        public void Apply_FilterWithoutAllowedMethodFilterType_WarnsInsteadOfThrowing()
        {
            LogAssert.Expect(LogType.Warning, FixupWarning);

            Assert.DoesNotThrow(() => UnityEventFilterAllowlistFixup.Apply(OptedIn, typeof(FilterWithoutEntryType), KnownListener));
        }

        [Test]
        public void Apply_ReadingTheAllowlistThrows_WarnsInsteadOfThrowing()
        {
            LogAssert.Expect(LogType.Warning, FixupWarning);

            Assert.DoesNotThrow(() => UnityEventFilterAllowlistFixup.Apply(OptedIn, typeof(FilterThatThrows), KnownListener));
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

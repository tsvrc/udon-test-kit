using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VRC.Core;

namespace UdonTestKit
{
    // When Play Mode starts, the VRChat SDK's VRC.Core.UnityEventFilter strips every persistent
    // UnityEvent listener it doesn't permit, and a stripped listener is never called. Unity Test
    // Framework's Play Mode runner reports results through listeners of the four types below, so
    // without an allowlist entry for each, a Play Mode run never reports and never exits.
    // The SDK's allowlist is internal; if it can't be reached, this warns instead of throwing.
    [InitializeOnLoad]
    internal static class UnityEventFilterAllowlist
    {
        internal static readonly string[] ListenerTypeNames =
        {
            "UnityEngine.TestTools.TestRunner.Callbacks.PlayModeRunnerCallback",
            "UnityEditor.TestTools.TestRunner.TestRunnerCallback",
            "UnityEditor.TestTools.TestRunner.Api.CallbacksDelegatorListener",
            "UnityEngine.TestRunner.Utils.TestRunCallbackListener",
        };

        internal static readonly string[] AllowedMethodNames =
        {
            "TestStarted", "TestFinished", "RunStarted", "RunFinished",
        };

        static UnityEventFilterAllowlist()
        {
            Apply(typeof(UnityEventFilter), ListenerTypeNames);
        }

        // The filter type and listener names are parameters so the kit's tests can reach every
        // warning against stand-in types.
        internal static void Apply(Type filterType, IEnumerable<string> listenerTypeNames)
        {
            try
            {
                Allowlist(filterType, listenerTypeNames);
            }
            catch (Exception e)
            {
                Warn(e is MissingMemberException ? e.Message : e.ToString());
            }
        }

        private static void Allowlist(Type filterType, IEnumerable<string> listenerTypeNames)
        {
            object lazy = filterType.GetField("_allowedUnityEventTargetTypes", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null)
                ?? throw new MissingMemberException($"could not find {filterType.Name}._allowedUnityEventTargetTypes.");
            var allowlist = lazy.GetType().GetProperty("Value")?.GetValue(lazy) as IDictionary
                ?? throw new MissingMemberException($"could not read {filterType.Name}'s allowlist dictionary.");
            ConstructorInfo entryConstructor = filterType.GetNestedType("AllowedMethodFilter", BindingFlags.NonPublic)
                ?.GetConstructor(new[] { typeof(List<string>), typeof(List<string>) })
                ?? throw new MissingMemberException($"could not find {filterType.Name}.AllowedMethodFilter's constructor.");

            foreach (string typeName in listenerTypeNames)
            {
                Type listenerType = FindType(typeName);
                if (listenerType == null)
                {
                    Warn($"could not find {typeName}, so it was not allowlisted.");
                }
                else if (!allowlist.Contains(listenerType))
                {
                    allowlist.Add(listenerType, entryConstructor.Invoke(new object[] { new List<string>(AllowedMethodNames), new List<string>() }));
                }
            }
        }

        internal static Type FindType(string fullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName))
                .FirstOrDefault(type => type != null);
        }

        private static void Warn(string reason)
        {
            Debug.LogWarning("[UdonTestKit] UnityEventFilterAllowlist: " + reason);
        }
    }
}

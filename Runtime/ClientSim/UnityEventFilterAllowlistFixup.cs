using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VRC.Core;

namespace UdonTestKit
{
    /// <summary>
    /// When Play Mode starts, the VRChat SDK's VRC.Core.UnityEventFilter strips every persistent
    /// UnityEvent listener it doesn't permit, and a stripped listener is never called. Unity Test
    /// Framework's Play Mode test runner calls its own callbacks through listeners of the four
    /// types in <see cref="ListenerTypeNames"/>, which the filter denies without an allowlist entry.
    ///
    /// This fixup adds an entry for each of those types, permitting their TestStarted,
    /// TestFinished, RunStarted and RunFinished methods. It runs from [InitializeOnLoad], only when
    /// some loaded assembly references this one, so a project that doesn't use
    /// UdonTestKit is never touched. If the SDK's internals it relies on can't be found,
    /// it logs a warning instead of throwing.
    /// </summary>
    [InitializeOnLoad]
    public static class UnityEventFilterAllowlistFixup
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

        static UnityEventFilterAllowlistFixup()
        {
            Apply(AppDomain.CurrentDomain.GetAssemblies(), typeof(UnityEventFilter), ListenerTypeNames);
        }

        // The filter type and listener names are parameters so the kit's tests can prove each
        // warning path against stand-in types.
        internal static void Apply(IEnumerable<Assembly> assemblies, Type filterType, IEnumerable<string> listenerTypeNames)
        {
            try
            {
                if (IsOptedIn(assemblies))
                    Allowlist(filterType, listenerTypeNames);
            }
            catch (Exception e)
            {
                Warn("threw an exception while widening the allowlist: " + e);
            }
        }

        // "Opted in" means: some assembly other than this one references it, whether or not it
        // extends ClientSimTestBase.
        internal static bool IsOptedIn(IEnumerable<Assembly> assemblies)
        {
            Assembly self = typeof(UnityEventFilterAllowlistFixup).Assembly;
            string selfName = self.GetName().Name;

            foreach (Assembly assembly in assemblies)
            {
                if (assembly == self || assembly.IsDynamic)
                    continue;

                foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
                    if (reference.Name == selfName)
                        return true;
            }

            return false;
        }

        private static void Allowlist(Type filterType, IEnumerable<string> listenerTypeNames)
        {
            FieldInfo lazyField = filterType.GetField("_allowedUnityEventTargetTypes", BindingFlags.NonPublic | BindingFlags.Static);
            if (lazyField == null)
            {
                Warn($"could not find {filterType.Name}._allowedUnityEventTargetTypes.");
                return;
            }

            object lazy = lazyField.GetValue(null);
            PropertyInfo valueProperty = lazy?.GetType().GetProperty("Value");
            if (!(valueProperty?.GetValue(lazy) is IDictionary allowlist))
            {
                Warn($"could not read {filterType.Name}'s allowlist dictionary.");
                return;
            }

            Type filterEntryType = filterType.GetNestedType("AllowedMethodFilter", BindingFlags.NonPublic);
            ConstructorInfo ctor = filterEntryType?.GetConstructor(new[] { typeof(List<string>), typeof(List<string>) });
            if (ctor == null)
            {
                Warn($"could not find {filterType.Name}.AllowedMethodFilter's constructor.");
                return;
            }

            foreach (string typeName in listenerTypeNames)
            {
                Type listenerType = FindType(typeName);
                if (listenerType == null)
                {
                    Warn($"could not find {typeName}, so it was not allowlisted.");
                    continue;
                }

                if (allowlist.Contains(listenerType))
                    continue;

                object filterEntry = ctor.Invoke(new object[] { new List<string>(AllowedMethodNames), new List<string>() });
                allowlist.Add(listenerType, filterEntry);
            }
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null)
                    return type;
            }

            return null;
        }

        private static void Warn(string reason)
        {
            Debug.LogWarning("[UdonTestKit] UnityEventFilterAllowlistFixup: " + reason);
        }
    }
}

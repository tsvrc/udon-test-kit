using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using VRC.Core;

namespace UdonTestKit.Tests.Helpers
{
    // Reaches the VRChat SDK's UnityEventFilter internals and Unity Test Framework's internal
    // listener types by name. Only works in Play Mode: outside it, UnityEventFilter throws
    // (Editor/Learning/UnityEventFilterOutsidePlayModeTests).
    internal static class UnityEventFilterAccess
    {
        public const string RunFinished = "RunFinished";

        public static bool IsTargetPermitted(UnityEngine.Object target, string methodName)
        {
            MethodInfo method = typeof(UnityEventFilter).GetMethod("IsTargetPermitted", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "UnityEventFilter.IsTargetPermitted not found. Has the VRChat SDK changed?");
            return (bool)method.Invoke(null, new object[] { target, methodName });
        }

        public static IDictionary Allowlist()
        {
            FieldInfo lazyField = typeof(UnityEventFilter).GetField("_allowedUnityEventTargetTypes", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(lazyField, Is.Not.Null, "UnityEventFilter._allowedUnityEventTargetTypes not found. Has the VRChat SDK changed?");
            object lazy = lazyField.GetValue(null);
            return (IDictionary)lazy.GetType().GetProperty("Value").GetValue(lazy);
        }

        public static Type FindType(string fullName)
        {
            Type type = UnityEventFilterAllowlist.FindType(fullName);
            Assert.That(type, Is.Not.Null, $"{fullName} not found. Has Unity Test Framework's internal layout changed?");
            return type;
        }

        // Creates an instance of a listener type, as a component on a new GameObject or as a
        // ScriptableObject, and adds what it created to created for the caller's teardown.
        public static UnityEngine.Object CreateListener(Type type, List<UnityEngine.Object> created)
        {
            if (typeof(Component).IsAssignableFrom(type))
            {
                var host = new GameObject(type.Name);
                created.Add(host);
                return host.AddComponent(type);
            }

            UnityEngine.Object instance = ScriptableObject.CreateInstance(type);
            created.Add(instance);
            return instance;
        }
    }
}

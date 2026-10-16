using System;
using UnityEditor.TestTools.TestRunner.Api;

namespace Tsvrc.UdonTestKit.Editor.Tests.Helpers
{
    // The callbacks registered with Unity Test Framework, which TestRunnerApi keeps in an internal
    // singleton.
    internal static class TestRunnerCallbacks
    {
        public static ICallbacks[] Registered()
        {
            Type holder = typeof(TestRunnerApi).Assembly.GetType("UnityEditor.TestTools.TestRunner.Api.CallbacksHolder", throwOnError: true);
            object instance = PrivateFieldAccess.InvokeStatic(holder, "get_instance");
            return (ICallbacks[])PrivateFieldAccess.InvokeInstance(instance, "GetAll");
        }
    }
}

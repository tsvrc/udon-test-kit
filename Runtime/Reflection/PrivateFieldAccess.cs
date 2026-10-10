using System;
using System.Reflection;
using NUnit.Framework;

namespace UdonTestKit
{
    /// <summary>
    /// Reads and writes fields, and calls methods, by name on any object or type, whatever their
    /// access level. Lookups also search base classes, which Type.GetField and Type.GetMethod don't
    /// do for private members. A name that isn't found fails the test, naming the member and type.
    /// </summary>
    public static class PrivateFieldAccess
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static void SetField(object target, string fieldName, object value) =>
            Find(target.GetType(), type => type.GetField(fieldName, InstanceFlags), "Field", fieldName).SetValue(target, value);

        public static void SetField(Type staticType, string fieldName, object value) =>
            Find(staticType, type => type.GetField(fieldName, StaticFlags), "Static field", fieldName).SetValue(null, value);

        public static T GetField<T>(object target, string fieldName) =>
            (T)Find(target.GetType(), type => type.GetField(fieldName, InstanceFlags), "Field", fieldName).GetValue(target);

        public static T GetField<T>(Type staticType, string fieldName) =>
            (T)Find(staticType, type => type.GetField(fieldName, StaticFlags), "Static field", fieldName).GetValue(null);

        public static object InvokeInstance(object target, string methodName, params object[] args) =>
            Find(target.GetType(), type => type.GetMethod(methodName, InstanceFlags), "Method", methodName).Invoke(target, args);

        public static object InvokeStatic(Type staticType, string methodName, params object[] args) =>
            Find(staticType, type => type.GetMethod(methodName, StaticFlags), "Static method", methodName).Invoke(null, args);

        private static TMember Find<TMember>(Type type, Func<Type, TMember> lookup, string kind, string name) where TMember : MemberInfo
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                TMember member = lookup(current);
                if (member != null)
                {
                    return member;
                }
            }

            Assert.Fail($"{kind} '{name}' not found on {type.Name}.");
            return null;
        }
    }
}

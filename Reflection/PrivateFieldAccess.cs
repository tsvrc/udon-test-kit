using System;
using System.Reflection;
using NUnit.Framework;

namespace UdonTestKit.Reflection
{
    /// <summary>
    /// Reads and writes fields, and calls methods, by name on any object or static type,
    /// whatever their access level. Instance lookups also search base classes, which
    /// Type.GetField and Type.GetMethod don't do for private members. A name that isn't found
    /// fails the test with an assertion naming the member and type.
    /// </summary>
    public static class PrivateFieldAccess
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}. Signature changed?");
            field.SetValue(target, value);
        }

        public static void SetField(Type staticType, string fieldName, object value)
        {
            FieldInfo field = staticType.GetField(fieldName, StaticFlags);
            Assert.IsNotNull(field, $"Static field '{fieldName}' not found on {staticType.Name}. Signature changed?");
            field.SetValue(null, value);
        }

        public static T GetField<T>(object target, string fieldName)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}. Signature changed?");
            return (T)field.GetValue(target);
        }

        public static T GetField<T>(Type staticType, string fieldName)
        {
            FieldInfo field = staticType.GetField(fieldName, StaticFlags);
            Assert.IsNotNull(field, $"Static field '{fieldName}' not found on {staticType.Name}. Signature changed?");
            return (T)field.GetValue(null);
        }

        public static object InvokeStatic(Type type, string methodName, params object[] args)
        {
            MethodInfo method = type.GetMethod(methodName, StaticFlags);
            Assert.IsNotNull(method, $"Method '{methodName}' not found on {type.Name}. Signature changed?");
            return method.Invoke(null, args);
        }

        public static object InvokeInstance(object target, string methodName, params object[] args)
        {
            MethodInfo method = FindMethod(target.GetType(), methodName);
            Assert.IsNotNull(method, $"Method '{methodName}' not found on {target.GetType().Name}. Signature changed?");
            return method.Invoke(target, args);
        }

        private static FieldInfo FindField(Type type, string name)
        {
            for (Type t = type; t != null; t = t.BaseType)
            {
                FieldInfo field = t.GetField(name, InstanceFlags);
                if (field != null) return field;
            }
            return null;
        }

        private static MethodInfo FindMethod(Type type, string name)
        {
            for (Type t = type; t != null; t = t.BaseType)
            {
                MethodInfo method = t.GetMethod(name, InstanceFlags);
                if (method != null) return method;
            }
            return null;
        }
    }
}

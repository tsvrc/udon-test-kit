using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Tsvrc.UdonTestKit
{
    /// <summary>
    /// Reads and writes fields, and calls methods, by name on any object or type, whatever their
    /// access level. Lookups also search base classes, which Type.GetField and Type.GetMethod don't
    /// do for private members. A name that isn't found fails the test, naming the member and type, and
    /// so does a call that no overload takes, or that two take equally well. A null argument array is
    /// one null argument.
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

        /// <summary>Calls the instance method the arguments fit, as C# picks among overloads.</summary>
        public static object InvokeInstance(object target, string methodName, params object[] args)
        {
            object[] arguments = args ?? new object[] { null };
            return FindMethod(target.GetType(), InstanceFlags, "Method", methodName, ref arguments).Invoke(target, arguments);
        }

        /// <summary>Calls the static method the arguments fit, as C# picks among overloads.</summary>
        public static object InvokeStatic(Type staticType, string methodName, params object[] args)
        {
            object[] arguments = args ?? new object[] { null };
            return FindMethod(staticType, StaticFlags, "Static method", methodName, ref arguments).Invoke(null, arguments);
        }

        // The overload the arguments fit, on the type or the nearest base class that has one.
        private static MethodBase FindMethod(Type type, BindingFlags flags, string kind, string name, ref object[] args)
        {
            bool named = false;
            for (Type current = type; current != null; current = current.BaseType)
            {
                MethodInfo[] declared = current.GetMethods(flags | BindingFlags.DeclaredOnly).Where(method => method.Name == name).ToArray();
                named |= declared.Length > 0;
                // A method with type parameters can't be called without type arguments, and the
                // binder picks one for a null argument.
                object[] arguments = args;
                MethodBase[] overloads = declared.Where(method => !method.ContainsGenericParameters && TakesTheNulls(method, arguments)).ToArray<MethodBase>();
                if (overloads.Length == 0)
                {
                    continue;
                }
                try
                {
                    return Type.DefaultBinder.BindToMethod(flags, overloads, ref args, null, null, null, out _);
                }
                catch (MissingMethodException)
                {
                    // An overload on a base class can still take the arguments.
                }
                catch (AmbiguousMatchException)
                {
                    Assert.Fail($"{kind} '{name}' on {current.Name} has overloads that take these arguments equally well.");
                }
            }

            Assert.Fail(named ? $"{kind} '{name}' on {type.Name} takes none of these arguments." : $"{kind} '{name}' not found on {type.Name}.");
            return null;
        }

        // Whether each null argument goes to a parameter that takes null, as C# requires, matching
        // arguments to parameters by position. The default binder takes null for a value type
        // parameter too, which makes such overloads ambiguous.
        private static bool TakesTheNulls(MethodInfo method, object[] args)
        {
            return method.GetParameters().Zip(args, (parameter, argument) =>
                argument != null || TakesNull(parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType() : parameter.ParameterType)).All(takes => takes);
        }

        private static bool TakesNull(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) != null;

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

using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Tsvrc.UdonTestKit.Editor.Tests.Learning
{
    // GetField and GetMethod on a derived type miss a private member of its base class, so
    // PrivateFieldAccess searches base classes.
    //
    // Type.DefaultBinder picks, of the overloads it's given, the one the arguments fit best, as C#
    // does. When no overload takes the arguments it throws MissingMethodException, and when two take
    // them equally well AmbiguousMatchException. So PrivateFieldAccess binds a method call through it,
    // one class of the hierarchy at a time. Unlike C#, it lets null fit a parameter of a value type,
    // so PrivateFieldAccess first leaves out the overloads a null argument can't go to. It also lets
    // null fit a type parameter, and picks a method with type parameters, which can't be called
    // without type arguments, so PrivateFieldAccess leaves those methods out too.
    public class ReflectionTests
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Test]
        public void BindToMethod_StringArgument_PicksTheStringOverloadOverTheObjectOne()
        {
            Assert.That(Bind(nameof(Overloads.StringOrObject), "text").GetParameters()[0].ParameterType, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void BindToMethod_NullArgumentOverloadsOfAReferenceAndAValueType_ThrowsAmbiguousMatchException()
        {
            Assert.That(() => Bind(nameof(Overloads.StringOrInt), new object[] { null }), Throws.TypeOf<AmbiguousMatchException>());
        }

        [Test]
        public void BindToMethod_ArgumentNoOverloadTakes_ThrowsMissingMethodException()
        {
            Assert.That(() => Bind(nameof(Overloads.StringOrInt), 1.5), Throws.TypeOf<MissingMethodException>());
        }

        [Test]
        public void BindToMethod_ArgumentsTwoOverloadsTakeEquallyWell_ThrowsAmbiguousMatchException()
        {
            Assert.That(() => Bind(nameof(Overloads.Tie), "a", "b"), Throws.TypeOf<AmbiguousMatchException>());
        }

        [Test]
        public void BindToMethod_NullArgumentMethodWithTypeParameters_PicksIt()
        {
            Assert.That(Bind(nameof(Overloads.Generic), new object[] { null }).ContainsGenericParameters, Is.True);
        }

        [Test]
        public void GetField_PrivateFieldDeclaredOnBaseType_ReturnsNull()
        {
            // Arrange
            Assume.That(typeof(Base).GetField("_baseField", InstanceFlags), Is.Not.Null);

            // Act
            FieldInfo field = typeof(Derived).GetField("_baseField", InstanceFlags);

            // Assert
            Assert.That(field, Is.Null);
        }

        [Test]
        public void GetMethod_PrivateMethodDeclaredOnBaseType_ReturnsNull()
        {
            // Arrange
            Assume.That(typeof(Base).GetMethod("BasePrivateMethod", InstanceFlags), Is.Not.Null);

            // Act
            MethodInfo method = typeof(Derived).GetMethod("BasePrivateMethod", InstanceFlags);

            // Assert
            Assert.That(method, Is.Null);
        }

        private static MethodBase Bind(string name, params object[] args)
        {
            MethodBase[] overloads = typeof(Overloads).GetMethods(InstanceFlags | BindingFlags.DeclaredOnly)
                .Where(method => method.Name == name).ToArray<MethodBase>();
            return Type.DefaultBinder.BindToMethod(InstanceFlags, overloads, ref args, null, null, null, out _);
        }

        private class Base
        {
#pragma warning disable CS0414, IDE0044, IDE0051 // Reached only through reflection.
            private int _baseField = 1;
            private int BasePrivateMethod() => _baseField;
#pragma warning restore CS0414, IDE0044, IDE0051
        }

        private class Derived : Base
        {
        }

#pragma warning disable IDE0060 // Only their parameter types matter.
        private class Overloads
        {
            public void StringOrObject(string value) { }
            public void StringOrObject(object value) { }
            public void StringOrInt(string value) { }
            public void StringOrInt(int value) { }
            public void Tie(string first, object second) { }
            public void Tie(object first, string second) { }
            public T Generic<T>(T value) => value;
        }
#pragma warning restore IDE0060
    }
}

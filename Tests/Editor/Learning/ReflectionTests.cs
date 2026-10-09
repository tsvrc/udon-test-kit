using System.Reflection;
using NUnit.Framework;

namespace UdonTestKit.Editor.Tests.Learning
{
    // GetField and GetMethod on a derived type miss a private member of its base class, so
    // PrivateFieldAccess searches base classes.
    public class ReflectionTests
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

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
    }
}

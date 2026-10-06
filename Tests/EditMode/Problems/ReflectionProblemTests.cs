using System.Reflection;
using NUnit.Framework;

namespace UdonTestKit.Tests.EditMode.Problems
{
    // Problem: Type.GetField and Type.GetMethod, called on a derived type, don't find a private
    // member declared on its base class, even with BindingFlags.NonPublic.
    // Solved by PrivateFieldAccess searching base classes, tested in Solutions/PrivateFieldAccessTests.
    // A failure here means .NET changed, so that search may no longer be needed.
    public class ReflectionProblemTests
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

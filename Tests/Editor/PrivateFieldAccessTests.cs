using NUnit.Framework;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    public class PrivateFieldAccessTests
    {
        private const int InitialStaticValue = 10;

        [TearDown]
        public void TearDown()
        {
            PrivateFieldAccess.SetField(typeof(Base), "_baseStaticField", InitialStaticValue);
        }

        [Test]
        public void GetField_PrivateFieldOnExactType_ReturnsValue()
        {
            // Arrange
            var target = new Derived();

            // Act
            string value = PrivateFieldAccess.GetField<string>(target, "_derivedField");

            // Assert
            Assert.That(value, Is.EqualTo("hello"));
        }

        [Test]
        public void GetField_PrivateFieldDeclaredOnBaseType_ReturnsValue()
        {
            // Arrange
            var target = new Derived();

            // Act
            int value = PrivateFieldAccess.GetField<int>(target, "_baseField");

            // Assert
            Assert.That(value, Is.EqualTo(1));
        }

        [Test]
        public void SetField_PrivateFieldDeclaredOnBaseType_ChangesTheValue()
        {
            // Arrange
            var target = new Derived();

            // Act
            PrivateFieldAccess.SetField(target, "_baseField", 42);

            // Assert
            Assert.That(PrivateFieldAccess.GetField<int>(target, "_baseField"), Is.EqualTo(42));
        }

        [Test]
        public void GetField_UnknownFieldName_FailsNamingItAndTheType()
        {
            var failure = Assert.Throws<AssertionException>(() => PrivateFieldAccess.GetField<int>(new Derived(), "_doesNotExist"));

            Assert.That(failure.Message, Is.EqualTo("Field '_doesNotExist' not found on Derived."));
        }

        [Test]
        public void SetField_UnknownFieldName_FailsNamingItAndTheType()
        {
            var failure = Assert.Throws<AssertionException>(() => PrivateFieldAccess.SetField(new Derived(), "_doesNotExist", 1));

            Assert.That(failure.Message, Is.EqualTo("Field '_doesNotExist' not found on Derived."));
        }

        [Test]
        public void SetField_PrivateStaticField_ChangesTheValue()
        {
            // Act
            PrivateFieldAccess.SetField(typeof(Base), "_baseStaticField", 99);

            // Assert
            Assert.That(PrivateFieldAccess.GetField<int>(typeof(Base), "_baseStaticField"), Is.EqualTo(99));
        }

        [Test]
        public void GetField_UnknownStaticFieldName_FailsNamingItAndTheType()
        {
            var failure = Assert.Throws<AssertionException>(() => PrivateFieldAccess.GetField<int>(typeof(Base), "_doesNotExist"));

            Assert.That(failure.Message, Is.EqualTo("Static field '_doesNotExist' not found on Base."));
        }

        [Test]
        public void SetField_UnknownStaticFieldName_FailsNamingItAndTheType()
        {
            var failure = Assert.Throws<AssertionException>(() => PrivateFieldAccess.SetField(typeof(Base), "_doesNotExist", 1));

            Assert.That(failure.Message, Is.EqualTo("Static field '_doesNotExist' not found on Base."));
        }

        [Test]
        public void InvokeInstance_PrivateMethodDeclaredOnBaseType_ReturnsResult()
        {
            // Arrange
            var target = new Derived();

            // Act
            object result = PrivateFieldAccess.InvokeInstance(target, "BasePrivateMethod", 5);

            // Assert
            Assert.That(result, Is.EqualTo(6));
        }

        [Test]
        public void InvokeInstance_UnknownMethodName_FailsNamingItAndTheType()
        {
            var failure = Assert.Throws<AssertionException>(() => PrivateFieldAccess.InvokeInstance(new Derived(), "DoesNotExist"));

            Assert.That(failure.Message, Is.EqualTo("Method 'DoesNotExist' not found on Derived."));
        }

        [Test]
        public void InvokeStatic_PrivateStaticMethod_ReturnsResult()
        {
            // Act
            object result = PrivateFieldAccess.InvokeStatic(typeof(Base), "BaseStaticMethod", 5);

            // Assert
            Assert.That(result, Is.EqualTo(10));
        }

        [Test]
        public void InvokeStatic_UnknownMethodName_FailsNamingItAndTheType()
        {
            var failure = Assert.Throws<AssertionException>(() => PrivateFieldAccess.InvokeStatic(typeof(Base), "DoesNotExist"));

            Assert.That(failure.Message, Is.EqualTo("Static method 'DoesNotExist' not found on Base."));
        }

        [Test]
        public void GetField_PublicField_ReturnsValue()
        {
            Assert.That(PrivateFieldAccess.GetField<int>(new Derived(), nameof(Base.PublicField)), Is.EqualTo(3));
        }

        [Test]
        public void InvokeStatic_PublicStaticMethod_ReturnsResult()
        {
            Assert.That(PrivateFieldAccess.InvokeStatic(typeof(Base), nameof(Base.PublicStaticMethod), 5), Is.EqualTo(6));
        }

        private class Base
        {
            public int PublicField = 3;

            public static int PublicStaticMethod(int x) => x + 1;

#pragma warning disable CS0414, IDE0051, IDE0044 // Reached only through reflection.
            private int _baseField = 1;
            private static int _baseStaticField = InitialStaticValue;
            private int BasePrivateMethod(int x) => x + _baseField;
            private static int BaseStaticMethod(int x) => x * 2;
#pragma warning restore CS0414, IDE0051, IDE0044
        }

        private class Derived : Base
        {
#pragma warning disable CS0414, IDE0044, IDE0051, IDE0052 // Reached only through reflection.
            private string _derivedField = "hello";
#pragma warning restore CS0414, IDE0044, IDE0051, IDE0052
        }
    }
}

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

        [TestCase(1, ExpectedResult = "int")]
        [TestCase("a", ExpectedResult = "string")]
        public object InvokeInstance_OverloadsOfTwoParameterTypes_CallsTheOneTheArgumentFits(object argument)
        {
            return PrivateFieldAccess.InvokeInstance(new Derived(), "Pick", argument);
        }

        [Test]
        public void InvokeInstance_NullArgumentArray_PassesOneNullArgument()
        {
            Assert.That(PrivateFieldAccess.InvokeInstance(new Derived(), "Pick", null), Is.EqualTo("string"));
        }

        [TestCase("RefPick", ExpectedResult = "string")]
        [TestCase("NullablePick", ExpectedResult = "int?")]
        public object InvokeInstance_NullArgument_CallsTheOverloadWhoseParameterTakesNull(string method)
        {
            return PrivateFieldAccess.InvokeInstance(new Derived(), method, new object[] { null });
        }

        [Test]
        public void InvokeInstance_NullAndAnotherArgument_CallsTheOverloadWhoseParameterTakesTheNull()
        {
            Assert.That(PrivateFieldAccess.InvokeInstance(new Derived(), "Two", null, "b"), Is.EqualTo("string, string"));
        }

        [Test]
        public void InvokeInstance_MethodWithAParamsArray_PassesTheLastArgumentsInTheArray()
        {
            Assert.That(PrivateFieldAccess.InvokeInstance(new Derived(), "Spread", 2, "a", "b"), Is.EqualTo("2: a, b"));
        }

        [Test]
        public void InvokeInstance_NullForAValueTypeParameterOfAMethodWithAParamsArray_FailsAsTakingNoneOfTheArguments()
        {
            var failure = Assert.Throws<AssertionException>(() => PrivateFieldAccess.InvokeInstance(new Derived(), "Spread", null, "a", "b"));

            Assert.That(failure.Message, Is.EqualTo("Method 'Spread' on Derived takes none of these arguments."));
        }

        [TestCase(1, ExpectedResult = "Base.Over(int)")]
        [TestCase("a", ExpectedResult = "Derived.Over(string)")]
        public object InvokeInstance_OverloadsOnTwoClassesOfTheHierarchy_CallsTheOneTheArgumentFits(object argument)
        {
            return PrivateFieldAccess.InvokeInstance(new Derived(), "Over", argument);
        }

        // C# leaves out the overloads of a base class once one of the type itself takes the arguments.
        [Test]
        public void InvokeInstance_OverloadOfTheTypeTakesTheArgument_CallsItOverAMoreSpecificOneOfABaseClass()
        {
            Assert.That(PrivateFieldAccess.InvokeInstance(new Derived(), "Near", "a"), Is.EqualTo("Derived.Near(object)"));
        }

        [Test]
        public void InvokeInstance_ArgumentNoOverloadTakes_FailsNamingTheMethodAndType()
        {
            var failure = Assert.Throws<AssertionException>(() => PrivateFieldAccess.InvokeInstance(new Derived(), "Pick", 1.5));

            Assert.That(failure.Message, Is.EqualTo("Method 'Pick' on Derived takes none of these arguments."));
        }

        [Test]
        public void InvokeInstance_ArgumentsTwoOverloadsTakeEquallyWell_FailsNamingTheMethodAndType()
        {
            var failure = Assert.Throws<AssertionException>(() => PrivateFieldAccess.InvokeInstance(new Derived(), "Tie", "a", "b"));

            Assert.That(failure.Message, Is.EqualTo("Method 'Tie' on Base has overloads that take these arguments equally well."));
        }

        [Test]
        public void InvokeInstance_MethodWithTypeParameters_FailsAsTakingNoneOfTheArguments()
        {
            var failure = Assert.Throws<AssertionException>(() => PrivateFieldAccess.InvokeInstance(new Derived(), "Generic", 5));

            Assert.That(failure.Message, Is.EqualTo("Method 'Generic' on Derived takes none of these arguments."));
        }

        [TestCase(1, ExpectedResult = "int")]
        [TestCase("a", ExpectedResult = "string")]
        public object InvokeStatic_OverloadsOfTwoParameterTypes_CallsTheOneTheArgumentFits(object argument)
        {
            return PrivateFieldAccess.InvokeStatic(typeof(Base), "StaticPick", argument);
        }

        [Test]
        public void InvokeStatic_NullArgumentArray_PassesOneNullArgument()
        {
            Assert.That(PrivateFieldAccess.InvokeStatic(typeof(Base), "StaticPick", null), Is.EqualTo("string"));
        }

        [Test]
        public void InvokeStatic_ArgumentNoOverloadTakes_FailsNamingTheMethodAndType()
        {
            var failure = Assert.Throws<AssertionException>(() => PrivateFieldAccess.InvokeStatic(typeof(Base), "StaticPick", 1.5));

            Assert.That(failure.Message, Is.EqualTo("Static method 'StaticPick' on Base takes none of these arguments."));
        }

        private class Base
        {
            public int PublicField = 3;

            public static int PublicStaticMethod(int x) => x + 1;

            public string Near(string value) => "Base.Near(string)";

#pragma warning disable CS0414, IDE0051, IDE0044, IDE0060 // Reached only through reflection.
            private int _baseField = 1;
            private static int _baseStaticField = InitialStaticValue;
            private int BasePrivateMethod(int x) => x + _baseField;
            private static int BaseStaticMethod(int x) => x * 2;
            private string Pick(int value) => "int";
            private string Pick(string value) => "string";
            private string Over(int value) => "Base.Over(int)";
            private string Tie(string first, object second) => "first";
            private string Tie(object first, string second) => "second";
            private T Generic<T>(T value) => value;
            private string RefPick(ref int value) => "ref int";
            private string RefPick(string value) => "string";
            private string NullablePick(int value) => "int";
            private string NullablePick(int? value) => "int?";
            private string Two(int first, string second) => "int, string";
            private string Two(string first, string second) => "string, string";
            private string Spread(int first, params string[] rest) => $"{first}: {string.Join(", ", rest)}";
            private static string StaticPick(int value) => "int";
            private static string StaticPick(string value) => "string";
#pragma warning restore CS0414, IDE0051, IDE0044, IDE0060
        }

        private class Derived : Base
        {
#pragma warning disable CS0414, IDE0044, IDE0051, IDE0052, IDE0060 // Reached only through reflection.
            private string _derivedField = "hello";
            private string Over(string value) => "Derived.Over(string)";
            private string Near(object value) => "Derived.Near(object)";
#pragma warning restore CS0414, IDE0044, IDE0051, IDE0052, IDE0060
        }
    }
}

using NUnit.Framework;
using VRC.Udon;

namespace Tsvrc.UdonTestKit.Tests
{
    public class UdonCallLogTests : UdonTestBase
    {
        [Test]
        public void Add_CalledFromUdon_KeepsTheCallsInOrder()
        {
            // Arrange
            UdonBehaviour log = Udon.Create<UdonCallLog>("Log");
            UdonBehaviour first = Udon.Create<UdonDouble>("First");
            UdonBehaviour second = Udon.Create<UdonDouble>("Second");
            first.SetProgramVariable(nameof(UdonDouble.Log), log);
            second.SetProgramVariable(nameof(UdonDouble.Log), log);

            // Act
            second.SendCustomEvent(nameof(UdonDouble._Record));
            first.SendCustomEvent(nameof(UdonDouble._Record));
            second.SendCustomEvent(nameof(UdonDouble._Record));

            // Assert
            Assert.That(UdonHost.Calls(log), Is.EqualTo(new[] { "Second._Record", "First._Record", "Second._Record" }));
        }

        [Test]
        public void Calls_NothingAdded_ReturnsNone()
        {
            // Arrange
            UdonBehaviour log = Udon.Create<UdonCallLog>("Log");

            // Act and assert
            Assert.That(UdonHost.Calls(log), Is.Empty);
        }

        [Test]
        public void Add_CallWithASemicolon_KeepsItWhole()
        {
            // Arrange
            UdonBehaviour log = Udon.Create<UdonCallLog>("Log");
            UdonBehaviour udonDouble = Udon.Create<UdonDouble>("Semi;colon");
            udonDouble.SetProgramVariable(nameof(UdonDouble.Log), log);

            // Act
            udonDouble.SendCustomEvent(nameof(UdonDouble._Record));

            // Assert
            Assert.That(UdonHost.Calls(log), Is.EqualTo(new[] { "Semi;colon._Record" }));
        }

        [Test]
        public void Calls_ReturnedArrayChanged_LeavesTheLog()
        {
            // Arrange
            UdonBehaviour log = Udon.Create<UdonCallLog>("Log");
            UdonBehaviour udonDouble = Udon.Create<UdonDouble>("Double");
            udonDouble.SetProgramVariable(nameof(UdonDouble.Log), log);
            udonDouble.SendCustomEvent(nameof(UdonDouble._Record));

            // Act
            UdonHost.Calls(log)[0] = "Changed";

            // Assert
            Assert.That(UdonHost.Calls(log), Is.EqualTo(new[] { "Double._Record" }));
        }
    }
}

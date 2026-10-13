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
            UdonBehaviour first = Udon.Create<UdonProbe>("First");
            UdonBehaviour second = Udon.Create<UdonProbe>("Second");
            first.SetProgramVariable(nameof(UdonProbe.Log), log);
            second.SetProgramVariable(nameof(UdonProbe.Log), log);

            // Act
            second.SendCustomEvent(nameof(UdonProbe._Record));
            first.SendCustomEvent(nameof(UdonProbe._Record));
            second.SendCustomEvent(nameof(UdonProbe._Record));

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
    }
}

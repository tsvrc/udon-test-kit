using NUnit.Framework;
using Tsvrc.TestKit.Tests.Helpers;

namespace Tsvrc.TestKit.Tests
{
    public class CallbackSpyTests
    {
        [Test]
        public void SendCustomEvent_CallbackBInPlayMode_IsRecorded()
        {
            // Arrange
            using var calls = new CallLog();
            CallbackSpy spy = calls.CreateSpy();

            // Act
            spy.SendCustomEvent(nameof(CallbackSpy.CallbackB));

            // Assert
            Assert.That(calls, Is.EqualTo(new[] { "Spy.CallbackB" }));
        }

        [Test]
        public void SendCustomEvent_SubclassCallbacks_AreRecordedUnderTheirOwnNames()
        {
            // Arrange
            using var calls = new CallLog();
            DoorSpy door = calls.CreateSpy<DoorSpy>("Door");

            // Act
            door.SendCustomEvent(nameof(DoorSpy.Open));
            door.SendCustomEvent(nameof(DoorSpy.Close));

            // Assert
            Assert.That(calls, Is.EqualTo(new[] { "Door.Open", "Door.Close" }));
        }
    }
}

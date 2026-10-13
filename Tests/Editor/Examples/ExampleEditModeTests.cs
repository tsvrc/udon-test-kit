using NUnit.Framework;
using Tsvrc.UdonTestKit;

public class ExampleEditModeTests
{
    [Test]
    public void SendCustomEvent_OnExample_IsRecorded()
    {
        using var calls = new CallLog();
        ExampleSpy spy = calls.CreateSpy<ExampleSpy>("Example");

        spy.SendCustomEvent(nameof(ExampleSpy.OnExample));

        Assert.That(calls, Is.EqualTo(new[] { "Example.OnExample" }));
    }
}

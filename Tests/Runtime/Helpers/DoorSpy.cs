namespace Tsvrc.UdonTestKit.Tests.Helpers
{
    // A spy with callbacks named for what a world's code calls.
    public class DoorSpy : CallbackSpy
    {
        public void Open() => Record();

        public void Close() => Record();
    }
}

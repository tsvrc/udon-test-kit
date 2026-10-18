using UdonSharp;

namespace Tsvrc.UdonTestKit.Tests.Helpers
{
    // An UdonSharpBehaviour standing in for a world's own behaviour, with one method a call by name
    // reaches and two it doesn't.
    public class TargetBehaviour : UdonSharpBehaviour
    {
        public const string PrivateCountName = nameof(CountPrivately);

        public int CountCalls;

        public void Count() => CountCalls++;

        public void CountBy(int amount) => CountCalls += amount;

        private void CountPrivately() => CountCalls++;
    }
}

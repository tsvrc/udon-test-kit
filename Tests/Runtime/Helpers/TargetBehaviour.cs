using UdonSharp;

namespace Tsvrc.TestKit.Tests.Helpers
{
    // An UdonSharpBehaviour with one public method, standing in for a world's own behaviour.
    public class TargetBehaviour : UdonSharpBehaviour
    {
        public int CountCalls;

        public void Count() => CountCalls++;
    }
}

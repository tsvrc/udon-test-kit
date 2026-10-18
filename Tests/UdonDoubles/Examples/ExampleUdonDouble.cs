using Tsvrc.UdonTestKit;
using UdonSharp;

namespace Tsvrc.UdonTestKit.Tests
{
    // A test double that runs as Udon and adds each call it gets to a log. Doubles that UdonHost runs
    // go in this assembly, which player builds include, as the compile for a world upload needs.
    // UdonSharp compiles every U# script of a project together, so no two of them can share a
    // namespace and name.
    public class ExampleUdonDouble : UdonSharpBehaviour
    {
        public UdonCallLog Log;

        public void OnExample()
        {
            Log.Add(name + ".OnExample");
        }
    }
}

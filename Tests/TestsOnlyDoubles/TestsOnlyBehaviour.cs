using UdonSharp;

namespace Tsvrc.UdonTestKit.Tests
{
    // An UdonSharpBehaviour in a tests-only assembly that has a U# assembly definition, where a world
    // might put an Udon test double. The compile for upload leaves that assembly out.
    public class TestsOnlyBehaviour : UdonSharpBehaviour
    {
    }
}

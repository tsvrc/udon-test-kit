namespace UdonTestKit
{
    /// <summary>
    /// A workaround for a known Play Mode testing problem, applied by ClientSimTestBase through
    /// FixupRegistry. Every fixup needs a Problem test proving its problem is real, and a Solution
    /// test proving it fixes it.
    /// </summary>
    public interface IPlayModeEnvironmentFixup
    {
        /// <summary>Called from ClientSimTestBase's [OneTimeSetUp].</summary>
        void OnBeforeAnyTests();

        /// <summary>Called from ClientSimTestBase's [UnityTearDown], after its ClientSim session is torn down.</summary>
        void OnUnityTearDown();
    }
}

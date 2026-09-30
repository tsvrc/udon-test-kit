using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UdonTestKit.ClientSim
{
    /// <summary>
    /// Shared base for real, ClientSim-backed Play Mode tests. Applies every active
    /// IPlayModeEnvironmentFixup at the matching lifecycle point. Extend this for any new Play
    /// Mode test file instead of re-deriving the same ClientSim setup/teardown by hand.
    /// </summary>
    public abstract class ClientSimTestBase
    {
        protected readonly ClientSimPlayerEnvironment Players = new ClientSimPlayerEnvironment();

        [OneTimeSetUp]
        public void ClientSimTestBase_OneTimeSetUp()
        {
            foreach (var fixup in FixupRegistry.ActiveFixups)
                fixup.OnBeforeAnyTests();
        }

        [UnityTearDown]
        public IEnumerator ClientSimTestBase_UnityTearDown()
        {
            Players.Teardown();

            foreach (var fixup in FixupRegistry.ActiveFixups)
                fixup.OnUnityTearDown();

            yield return null;
        }

        protected IEnumerator StartClientSim(bool localPlayerIsMaster = true)
        {
            return Players.Start(localPlayerIsMaster);
        }
    }
}

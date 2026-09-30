using UnityEngine;
using VRC.SDK3.ClientSim.Persistence;

namespace UdonTestKit.ClientSim
{
    /// <summary>
    /// With player persistence on, every ClientSim session creates a ClientSimPlayerObjectStorage
    /// as an unparented root object hidden from the Hierarchy window, and ClientSim's own
    /// teardown never destroys it. The leaked storage keeps rewriting its save file after the
    /// session ends, one more leak piles up with every session, and they all write to the same
    /// file. Destroying them after every test keeps each test starting from zero.
    /// </summary>
    public sealed class ClientSimPersistenceLeakFixup : IPlayModeEnvironmentFixup
    {
        public void OnBeforeAnyTests()
        {
        }

        public void OnUnityTearDown()
        {
            // Resources.FindObjectsOfTypeAll also returns ClientSim's ClientSimPlayerData prefab asset
            // once it's loaded, and that asset's scene isn't valid, so this guard keeps the fixup to
            // scene leaks. DestroyImmediate without allowDestroyingAssets refuses to destroy an asset,
            // so if the guard ever let the prefab through, the prefab would survive.
            foreach (var storage in Resources.FindObjectsOfTypeAll<ClientSimPlayerObjectStorage>())
                if (storage != null && storage.gameObject.scene.IsValid())
                    Object.DestroyImmediate(storage.gameObject);
        }
    }
}

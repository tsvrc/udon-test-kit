using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UdonTestKit
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
        // ClientSim declares the storage only when VRC_ENABLE_PLAYER_PERSISTENCE is defined, and the
        // VRChat SDK adds that define only after a project's first import has compiled. Looking the
        // type up by name keeps that first compile working; without persistence nothing leaks.
        private static readonly Type StorageType =
            Type.GetType("VRC.SDK3.ClientSim.Persistence.ClientSimPlayerObjectStorage, VRC.ClientSim");

        public void OnBeforeAnyTests()
        {
        }

        public void OnUnityTearDown()
        {
            if (StorageType == null)
                return;

            // Resources.FindObjectsOfTypeAll also returns ClientSim's ClientSimPlayerData prefab asset
            // once it's loaded, and that asset's scene isn't valid, so this guard keeps the fixup to
            // scene leaks. DestroyImmediate without allowDestroyingAssets refuses to destroy an asset,
            // so if the guard ever let the prefab through, the prefab would survive.
            foreach (Object found in Resources.FindObjectsOfTypeAll(StorageType))
            {
                var storage = (Component)found;
                if (storage != null && storage.gameObject.scene.IsValid())
                    Object.DestroyImmediate(storage.gameObject);
            }
        }
    }
}

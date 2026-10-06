using System;
using System.Collections;
using UnityEngine;
using VRC.SDK3.ClientSim;
using VRC.SDK3.Components;
using VRC.SDKBase;
using Object = UnityEngine.Object;

namespace UdonTestKit
{
    /// <summary>
    /// A real ClientSim session for Play Mode tests: starts ClientSim with the scene descriptor it
    /// needs, spawns, removes and finds players in its real player list, and tears the session
    /// down. Exposes real VRCPlayerApi instances.
    /// </summary>
    public sealed class ClientSimPlayerEnvironment
    {
        private const float DefaultReadyTimeoutSeconds = 10f;

        private readonly float _readyTimeoutSeconds;
        private readonly float _initializationDelay;
        private GameObject _descriptorObject;

        public ClientSimPlayerEnvironment() : this(DefaultReadyTimeoutSeconds, 0f)
        {
        }

        // Lets the kit's own tests make ClientSim start slower than the timeout, to prove the guard.
        internal ClientSimPlayerEnvironment(float readyTimeoutSeconds, float initializationDelay)
        {
            _readyTimeoutSeconds = readyTimeoutSeconds;
            _initializationDelay = initializationDelay;
        }

        /// <summary>Starts a ClientSim session and returns once ClientSim raises its
        /// ClientSimReadyEvent, by which point the local player is enabled. Throws
        /// TimeoutException if that doesn't happen within the ready timeout.</summary>
        public IEnumerator Start(bool localPlayerIsMaster = true)
        {
            CreateMinimalSceneDescriptor();

            var settings = new ClientSimSettings
            {
                enableClientSim = true,
                spawnPlayer = true,
                localPlayerIsMaster = localPlayerIsMaster,
                initializationDelay = _initializationDelay,
            };

            bool ready = false;
            var eventDispatcher = new ClientSimEventDispatcher();
            eventDispatcher.Subscribe<ClientSimReadyEvent>(_ => ready = true);

            ClientSimRuntimeLoader.BeginUnityTesting(settings, eventDispatcher);
            ClientSimRuntimeLoader.StartClientSim(settings, eventDispatcher);

            float deadline = Time.realtimeSinceStartup + _readyTimeoutSeconds;
            while (!ready)
            {
                if (Time.realtimeSinceStartup > deadline)
                    throw new TimeoutException($"ClientSim didn't report ready within {_readyTimeoutSeconds} seconds.");
                yield return null;
            }
        }

        /// <summary>Spawns a remote player. It's in the player list, and findable with
        /// FindPlayerByName, as soon as this returns.</summary>
        public void SpawnRemotePlayer(string displayName)
        {
            ClientSimMain.SpawnRemotePlayer(displayName);
        }

        /// <summary>Removes a player. It's gone from the player list as soon as this returns.</summary>
        public void RemovePlayer(VRCPlayerApi player)
        {
            ClientSimMain.RemovePlayer(player);
        }

        public static VRCPlayerApi FindPlayerByName(string name)
        {
            VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(players);
            foreach (VRCPlayerApi p in players)
                if (p != null && p.displayName == name) return p;
            return null;
        }

        public void Teardown()
        {
            if (ClientSimMain.HasInstance())
                ClientSimMain.RemoveInstance();

            ClientSimRuntimeLoader.EndUnityTesting();

            if (_descriptorObject != null)
                Object.DestroyImmediate(_descriptorObject);
            _descriptorObject = null;
        }

        private void CreateMinimalSceneDescriptor()
        {
            _descriptorObject = new GameObject("__TestSceneDescriptor");
            VRCSceneDescriptor descriptor = _descriptorObject.AddComponent<VRCSceneDescriptor>();

            GameObject spawnObject = new GameObject("__TestSpawn");
            spawnObject.transform.SetParent(_descriptorObject.transform);

            descriptor.spawns = new[] { spawnObject.transform };
        }
    }
}

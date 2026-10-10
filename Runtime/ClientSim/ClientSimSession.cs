using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.ClientSim;
using VRC.SDK3.ClientSim.Persistence;
using VRC.SDK3.Components;
using VRC.SDKBase;
using Object = UnityEngine.Object;

namespace UdonTestKit
{
    /// <summary>
    /// A ClientSim session for Play Mode tests, with real VRCPlayerApi players. Ending a session
    /// leaves nothing of it running, so the next session in the same scene starts from the data the
    /// last one saved, the way a player who leaves and rejoins does.
    /// </summary>
    public sealed class ClientSimSession
    {
        private const float DefaultReadyTimeoutSeconds = 10f;

        private readonly float _readyTimeoutSeconds;
        private readonly float _initializationDelay;
        private GameObject _addedDescriptor;

        public ClientSimSession() : this(DefaultReadyTimeoutSeconds, 0f)
        {
        }

        // Lets the kit's own tests make ClientSim get ready slower than the timeout.
        internal ClientSimSession(float readyTimeoutSeconds, float initializationDelay)
        {
            _readyTimeoutSeconds = readyTimeoutSeconds;
            _initializationDelay = initializationDelay;
        }

        /// <summary>
        /// Starts ClientSim and returns once the local player has joined and, with player persistence
        /// on, has its saved data restored. Adds a scene descriptor with one spawn point when the
        /// active scene has none. Fails the test if ClientSim's project settings check fails, and
        /// throws TimeoutException if ClientSim isn't ready within 10 seconds.
        /// </summary>
        public IEnumerator Start(bool localPlayerIsMaster = true)
        {
            ClientSimProjectSettings.AssertValid();

            if (Object.FindObjectOfType<VRC_SceneDescriptor>() == null)
                _addedDescriptor = CreateSceneDescriptor(Vector3.zero);

            var settings = new ClientSimSettings
            {
                enableClientSim = true,
                spawnPlayer = true,
                localPlayerIsMaster = localPlayerIsMaster,
                initializationDelay = _initializationDelay,
            };

            var events = new ClientSimEventDispatcher();
            bool ready = false;
            events.Subscribe<ClientSimReadyEvent>(_ => ready = true);
#if VRC_ENABLE_PLAYER_PERSISTENCE
            // ClientSim restores saved data after it reports ready.
            bool restored = false;
            events.Subscribe<ClientSimOnPlayerRestoredEvent>(restore => restored |= restore.player.isLocal);
#else
            const bool restored = true;
#endif

            ClientSimRuntimeLoader.BeginUnityTesting(settings, events);
            ClientSimRuntimeLoader.StartClientSim(settings, events);

            float deadline = Time.realtimeSinceStartup + _readyTimeoutSeconds;
            while (!(ready && restored))
            {
                if (Time.realtimeSinceStartup > deadline)
                    throw new TimeoutException($"ClientSim didn't get ready within {_readyTimeoutSeconds} seconds.");
                yield return null;
            }
        }

        /// <summary>Spawns a remote player and returns it. Give each player its own name.</summary>
        public VRCPlayerApi SpawnRemotePlayer(string displayName)
        {
            ClientSimMain.SpawnRemotePlayer(displayName);
            return FindPlayerByName(displayName);
        }

        /// <summary>Removes a player. It's gone from the player list when this returns.</summary>
        public void RemovePlayer(VRCPlayerApi player)
        {
            ClientSimMain.RemovePlayer(player);
        }

        /// <summary>
        /// Makes <paramref name="player"/> the owner of <paramref name="obj"/>, as Networking.SetOwner
        /// does in VRChat. ClientSim only lets GameObjects with VRCObjectSync or VRCObjectPool change
        /// owner, and the master owns all others. After this, Networking.SetOwner moves
        /// <paramref name="obj"/>'s ownership too, such as when the code under test calls it.
        /// </summary>
        public void SetOwner(VRCPlayerApi player, GameObject obj)
        {
            if (obj.GetComponent<IClientSimSyncable>() == null)
            {
                // Read before adding the component, which has no owner until it's set.
                int currentOwner = Networking.GetOwner(obj).playerId;
                obj.AddComponent<FakeOwnershipSyncable>().SetOwner(currentOwner);
            }
            Networking.SetOwner(player, obj);
        }

        /// <summary>Returns the player with this display name, or null.</summary>
        public static VRCPlayerApi FindPlayerByName(string displayName)
        {
            var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(players);
            return Array.Find(players, player => player != null && player.displayName == displayName);
        }

        /// <summary>
        /// Ends ClientSim, stops its players' saved data from being written, and removes the scene
        /// descriptor Start added, if any. Safe to call when no session is running.
        /// </summary>
        public void End()
        {
            if (ClientSimMain.HasInstance())
                ClientSimMain.RemoveInstance();
            ClientSimRuntimeLoader.EndUnityTesting();
            DestroySavedDataObjects();

            if (_addedDescriptor != null)
                Object.DestroyImmediate(_addedDescriptor);
            _addedDescriptor = null;
        }

        /// <summary>
        /// Deletes the PlayerData and PlayerObjects ClientSim saved for the active scene, for every
        /// player, so the next session there starts with none.
        /// </summary>
        public static void DeleteSavedData()
        {
            DeleteSavedData(Path.Combine(Path.GetDirectoryName(Application.dataPath), "ClientSimStorage"),
                SceneManager.GetActiveScene().name);
        }

        // ClientSim saves to <storageRoot>/PlayerData/PlayerData_<playerId>_<scene>.json and
        // <storageRoot>/PlayerObjects/PlayerObject_<playerId>_<scene>.json.
        internal static void DeleteSavedData(string storageRoot, string sceneName)
        {
            if (!Directory.Exists(storageRoot))
                return;

            var sceneFile = new Regex(@"^(PlayerData|PlayerObject)_\d+_" + Regex.Escape(sceneName) + @"\.json$");
            foreach (string file in Directory.GetFiles(storageRoot, "*.json", SearchOption.AllDirectories))
                if (sceneFile.IsMatch(Path.GetFileName(file)))
                    File.Delete(file);
        }

        // ClientSim keeps each player's saved data on a hidden root object that its own teardown
        // never destroys, and that keeps rewriting the player's save file.
        internal static void DestroySavedDataObjects()
        {
            foreach (ClientSimPlayerObjectStorage storage in SavedDataObjects().ToList())
                Object.DestroyImmediate(storage.gameObject);
        }

        // FindObjectsOfTypeAll also returns ClientSim's prefab asset, whose scene isn't valid.
        internal static IEnumerable<ClientSimPlayerObjectStorage> SavedDataObjects()
        {
            return Resources.FindObjectsOfTypeAll<ClientSimPlayerObjectStorage>()
                .Where(storage => storage != null && storage.gameObject.scene.IsValid());
        }

        internal static GameObject CreateSceneDescriptor(Vector3 spawnPosition)
        {
            var descriptorObject = new GameObject("__TestSceneDescriptor");
            var spawn = new GameObject("__TestSpawn");
            spawn.transform.position = spawnPosition;
            spawn.transform.SetParent(descriptorObject.transform);
            descriptorObject.AddComponent<VRCSceneDescriptor>().spawns = new[] { spawn.transform };
            return descriptorObject;
        }
    }
}

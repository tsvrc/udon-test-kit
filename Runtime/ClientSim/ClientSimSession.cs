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

namespace Tsvrc.UdonTestKit
{
    /// <summary>
    /// A ClientSim session for Play Mode tests, with real VRCPlayerApi players. Ending a session
    /// leaves nothing of it running, so the next session in the same scene starts from the data the
    /// last one saved, the way a player who leaves and rejoins does.
    /// </summary>
    public sealed class ClientSimSession
    {
        private readonly float _initializationDelay;
        private float _readyTimeoutSeconds = 10f;
        private GameObject _addedDescriptor;

        public ClientSimSession()
        {
        }

        // Lets the kit's own tests make ClientSim get ready slower than the timeout.
        internal ClientSimSession(float initializationDelay)
        {
            _initializationDelay = initializationDelay;
        }

        /// <summary>How many seconds <see cref="Start"/> waits for ClientSim to get ready before it
        /// throws TimeoutException: 10 unless set. Throws ArgumentOutOfRangeException for anything but
        /// a finite number above 0.</summary>
        public float ReadyTimeoutSeconds
        {
            get => _readyTimeoutSeconds;
            set
            {
                if (!(value > 0f) || float.IsInfinity(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The timeout needs a finite number of seconds above 0.");
                }
                _readyTimeoutSeconds = value;
            }
        }

        /// <summary>
        /// Starts ClientSim and returns once the local player has joined and, with player persistence
        /// on, has its saved data restored. Adds a scene descriptor with one spawn point, at the
        /// origin, when the active scene has none. Fails the test if the ClientSim project settings
        /// check fails, and throws TimeoutException if ClientSim isn't ready within ReadyTimeoutSeconds.
        /// </summary>
        public IEnumerator Start(bool localPlayerIsMaster = true)
        {
            ClientSimProjectSettings.AssertValid();

            if (Object.FindObjectOfType<VRC_SceneDescriptor>() == null)
            {
                _addedDescriptor = CreateSceneDescriptor(Vector3.zero);
            }

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
            // ClientSim keeps no saved data without the define.
            const bool restored = true;
#endif

            ClientSimRuntimeLoader.BeginUnityTesting(settings, events);
            ClientSimRuntimeLoader.StartClientSim(settings, events);

            float deadline = Time.realtimeSinceStartup + _readyTimeoutSeconds;
            while (!(ready && restored))
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    throw new TimeoutException($"ClientSim didn't get ready within {_readyTimeoutSeconds} seconds.");
                }
                yield return null;
            }
        }

        /// <summary>Spawns a remote player and returns it. Throws ArgumentException when
        /// <paramref name="displayName"/> is empty or a player in the session has it already: the
        /// player is found by its name.</summary>
        public VRCPlayerApi SpawnRemotePlayer(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
            {
                throw new ArgumentException("A remote player needs a display name.", nameof(displayName));
            }
            if (FindPlayerByName(displayName) != null)
            {
                throw new ArgumentException($"A player named {displayName} is in the session already. Give each player its own name.", nameof(displayName));
            }
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
        /// owner, and the master owns all others. After this, Networking.SetOwner moves the ownership
        /// of <paramref name="obj"/> too, such as when the code under test calls it, and when its
        /// owner leaves, the master becomes the owner.
        /// </summary>
        public void SetOwner(VRCPlayerApi player, GameObject obj)
        {
            if (obj.GetComponent<IClientSimSyncable>() == null)
            {
                // Read before adding the component, which has no owner until it's set.
                int currentOwner = Networking.GetOwner(obj).playerId;
                var owner = obj.AddComponent<FakeOwnershipSyncable>();
                owner.SetOwner(currentOwner);
                // ClientSim hands a leaving player's GameObjects to the master only for owners
                // registered with it, as the owners of VRCObjectSync and VRCObjectPool are. Its
                // manager is DontSave, which FindObjectOfType skips, and FindObjectsOfTypeAll also
                // returns the manager's prefab asset, whose scene isn't valid.
                Resources.FindObjectsOfTypeAll<ClientSimSyncedObjectManager>()
                    .First(manager => manager.gameObject.scene.IsValid())
                    .AddSyncedObject(owner);
            }
            Networking.SetOwner(player, obj);
        }

        /// <summary>Returns the player with this display name, or null.</summary>
        public static VRCPlayerApi FindPlayerByName(string displayName)
        {
            var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(players);
            return Array.Find(players, player => player.displayName == displayName);
        }

        /// <summary>
        /// Ends ClientSim, stops the saved data of its players from being written, and removes the
        /// scene descriptor Start added, if any. Safe to call when no session is running.
        /// </summary>
        public void End()
        {
            ClientSimMain.RemoveInstance();
            ClientSimRuntimeLoader.EndUnityTesting();
            DestroySavedDataObjects();

            if (_addedDescriptor != null)
            {
                Object.DestroyImmediate(_addedDescriptor);
            }
        }

        /// <summary>
        /// Deletes the PlayerData and PlayerObjects ClientSim saved for the active scene, for every
        /// player, so the next session there starts with none.
        /// </summary>
        public static void DeleteSavedData()
        {
            DeleteSavedData(StorageRoot, SceneManager.GetActiveScene().name);
        }

        // ClientSim saves to <StorageRoot>/PlayerData/PlayerData_<playerId>_<scene>.json and
        // <StorageRoot>/PlayerObjects/PlayerObject_<playerId>_<scene>.json.
        internal static string StorageRoot => Path.Combine(Path.GetDirectoryName(Application.dataPath), "ClientSimStorage");

        internal static void DeleteSavedData(string storageRoot, string sceneName)
        {
            DeleteSavedDataOfScenes(storageRoot, Regex.Escape(sceneName));
        }

        // The Unity Test Framework names the scene it runs Play Mode tests in InitTestScene and digits.
        internal static void DeleteTestSceneSavedData(string storageRoot)
        {
            DeleteSavedDataOfScenes(storageRoot, @"InitTestScene\d+");
        }

        private static void DeleteSavedDataOfScenes(string storageRoot, string scenePattern)
        {
            if (!Directory.Exists(storageRoot))
            {
                return;
            }

            var sceneFile = new Regex(@"^(PlayerData|PlayerObject)_\d+_" + scenePattern + @"\.json$");
            foreach (string file in Directory.GetFiles(storageRoot, "*.json", SearchOption.AllDirectories))
            {
                if (sceneFile.IsMatch(Path.GetFileName(file)))
                {
                    File.Delete(file);
                }
            }
        }

        // ClientSim keeps each player's saved data on a hidden root object that its own teardown
        // never destroys, and that keeps rewriting the player's save file.
        internal static void DestroySavedDataObjects()
        {
            foreach (ClientSimPlayerObjectStorage storage in SavedDataObjects())
            {
                Object.DestroyImmediate(storage.gameObject);
            }
        }

        // FindObjectsOfTypeAll also returns ClientSim's prefab asset, whose scene isn't valid.
        internal static IEnumerable<ClientSimPlayerObjectStorage> SavedDataObjects()
        {
            return Resources.FindObjectsOfTypeAll<ClientSimPlayerObjectStorage>()
                .Where(storage => storage.gameObject.scene.IsValid());
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

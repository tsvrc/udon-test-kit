using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using VRC.SDK3.ClientSim.Persistence;
using VRC.SDKBase;

namespace UdonTestKit.Tests.Helpers
{
    // Reaches ClientSim's saved-data internals, which have no public API.
    internal static class SavedData
    {
        public const string PersistenceOffMessage =
            "The session created no ClientSimPlayerObjectStorage, so ClientSim's player persistence is off.";

        // Scene instances only, never the prefab asset FindObjectsOfTypeAll also returns
        // (Editor/Learning/UnityObjectTests).
        public static List<ClientSimPlayerObjectStorage> FindStorages()
        {
            return Resources.FindObjectsOfTypeAll<ClientSimPlayerObjectStorage>()
                .Where(storage => storage.gameObject.scene.IsValid())
                .ToList();
        }

        // A destroyed object is null only through Unity's == (Editor/Learning/UnityObjectTests).
        public static int CountAlive(IEnumerable<ClientSimPlayerObjectStorage> storages)
        {
            return storages.Count(storage => storage != null);
        }

        // ClientSim's own internal PlayerDataFilePath, for the player the storage was initialized with.
        public static string PlayerObjectFileOf(ClientSimPlayerObjectStorage storage)
        {
            FieldInfo playerField = typeof(ClientSimPlayerObjectStorage).GetField("_player", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(playerField, Is.Not.Null, "ClientSimPlayerObjectStorage._player not found. Has ClientSim changed?");
            return FilePath(typeof(ClientSimPlayerObjectStorage), (VRCPlayerApi)playerField.GetValue(storage));
        }

        public static string PlayerDataFileOf(VRCPlayerApi player)
        {
            return FilePath(typeof(ClientSimPlayerDataStorage), player);
        }

        private static string FilePath(Type storageType, VRCPlayerApi player)
        {
            MethodInfo pathMethod = storageType.GetMethod("PlayerDataFilePath", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(pathMethod, Is.Not.Null, $"{storageType.Name}.PlayerDataFilePath not found. Has ClientSim changed?");
            return (string)pathMethod.Invoke(null, new object[] { player });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using VRC.SDK3.ClientSim.Persistence;
using VRC.SDKBase;

namespace UdonTestKit.Tests.Helpers
{
    // Shared by the saved-data tests, and a way into ClientSim's saved-data internals, which have
    // no public API.
    internal static class SavedData
    {
        public const string PersistenceOffMessage =
            "The session created no ClientSimPlayerObjectStorage, so ClientSim's player persistence is off.";
        public const string Key = "UdonTestKit.Key";
        public const string NewerSave = "{\"1\":[]}";
        // Long enough for a live storage to save again.
        public const float WriteWindowSeconds = 1f;
        // A pause before reading a save file, so a save started earlier isn't counted after it.
        public const float SettleSeconds = 0.5f;

        // A destroyed object equals null only through Unity's == and !=, not through `is null`.
        public static int CountAlive(IEnumerable<ClientSimPlayerObjectStorage> storages)
        {
            return storages.Count(storage => storage != null);
        }

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

        // ClientSim's own internal PlayerDataFilePath.
        private static string FilePath(Type storageType, VRCPlayerApi player)
        {
            MethodInfo pathMethod = storageType.GetMethod("PlayerDataFilePath", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(pathMethod, Is.Not.Null, $"{storageType.Name}.PlayerDataFilePath not found. Has ClientSim changed?");
            return (string)pathMethod.Invoke(null, new object[] { player });
        }
    }
}

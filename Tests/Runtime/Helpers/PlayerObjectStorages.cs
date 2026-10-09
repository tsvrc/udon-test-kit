using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using VRC.SDK3.ClientSim.Persistence;

namespace UdonTestKit.Tests.Helpers
{
    internal static class PlayerObjectStorages
    {
        public const string PersistenceOffMessage =
            "The session created no ClientSimPlayerObjectStorage, so ClientSim's player persistence is off.";

        // Same filter as ClientSimPersistenceLeakFixup: scene instances only, never the prefab
        // asset FindObjectsOfTypeAll also returns (Editor/Problems/UnityObjectProblemTests).
        public static List<ClientSimPlayerObjectStorage> FindInScene()
        {
            return Resources.FindObjectsOfTypeAll<ClientSimPlayerObjectStorage>()
                .Where(storage => storage.gameObject.scene.IsValid())
                .ToList();
        }

        public static int CountAlive(IEnumerable<ClientSimPlayerObjectStorage> storages)
        {
            // A destroyed object is null only through Unity's == (Editor/Problems/UnityObjectProblemTests).
            return storages.Count(storage => storage != null);
        }

        // The file a storage saves to, computed by ClientSim's own internal
        // PlayerDataFilePath(player) from the player the storage was initialized with.
        public static string FilePathOf(ClientSimPlayerObjectStorage storage)
        {
            FieldInfo playerField = typeof(ClientSimPlayerObjectStorage).GetField("_player", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo pathMethod = typeof(ClientSimPlayerObjectStorage).GetMethod("PlayerDataFilePath", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(playerField, Is.Not.Null, "ClientSimPlayerObjectStorage._player not found. Has ClientSim changed?");
            Assert.That(pathMethod, Is.Not.Null, "ClientSimPlayerObjectStorage.PlayerDataFilePath not found. Has ClientSim changed?");

            return (string)pathMethod.Invoke(null, new[] { playerField.GetValue(storage) });
        }
    }
}

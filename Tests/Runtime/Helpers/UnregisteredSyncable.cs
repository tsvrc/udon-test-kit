using UnityEngine;
using VRC.SDK3.ClientSim;

namespace Tsvrc.UdonTestKit.Tests.Helpers
{
    // An owner ClientSim reads and writes, never registered with ClientSim the way the owners of
    // VRCObjectSync and VRCObjectPool are.
    public class UnregisteredSyncable : MonoBehaviour, IClientSimSyncable
    {
        private int _ownerId;

        public int GetOwner() => _ownerId;

        public void SetOwner(int ownerID) => _ownerId = ownerID;
    }
}

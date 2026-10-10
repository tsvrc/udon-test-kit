using UnityEngine;
using VRC.SDK3.ClientSim;

namespace Tsvrc.UdonTestKit
{
    // ClientSim reads and writes a GameObject's owner only through an IClientSimSyncable on it.
    // Added by ClientSimSession.SetOwner.
    internal sealed class FakeOwnershipSyncable : MonoBehaviour, IClientSimSyncable
    {
        private int _ownerId;

        public int GetOwner() => _ownerId;

        public void SetOwner(int ownerID) => _ownerId = ownerID;
    }
}

using UnityEngine;
using VRC.SDK3.ClientSim;

namespace UdonTestKit.Behaviours
{
    /// <summary>
    /// Gives a GameObject an owner of its own in ClientSim. Without one, ClientSim reports the
    /// master as the owner of every GameObject, and Networking.SetOwner can't change it. Add this,
    /// then call SetOwner, or Networking.SetOwner, to make any player the owner. Until then the
    /// GameObject has no owner.
    /// </summary>
    public class FakeOwnershipSyncable : MonoBehaviour, IClientSimSyncable
    {
        private int _ownerId;

        public int GetOwner()
        {
            return _ownerId;
        }

        public void SetOwner(int ownerID)
        {
            _ownerId = ownerID;
        }
    }
}

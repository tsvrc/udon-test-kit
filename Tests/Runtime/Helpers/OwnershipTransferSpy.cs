using UnityEngine;
using VRC.SDK3.ClientSim;

namespace Tsvrc.UdonTestKit.Tests.Helpers
{
    // Counts the ownership transfers ClientSim reports for its GameObject.
    public class OwnershipTransferSpy : MonoBehaviour, IClientSimSyncableHandler
    {
        public int Transfers;

        public void OnOwnershipTransferred(int ownerID) => Transfers++;
    }
}

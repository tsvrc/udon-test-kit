using UdonSharp;

namespace Tsvrc.UdonTestKit
{
    // Records the calls of the doubles that run as Udon, as "GameObjectName.Method;" in the order they
    // arrived, for UdonHost.Calls to read.
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UdonCallLog : UdonSharpBehaviour
    {
        public string Calls = "";

        public void Add(string call)
        {
            Calls += call + ";";
        }
    }
}

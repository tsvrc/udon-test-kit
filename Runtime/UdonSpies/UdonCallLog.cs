using UdonSharp;

namespace Tsvrc.UdonTestKit
{
    /// <summary>
    /// A spy that runs as Udon. Test doubles that run as Udon call <see cref="Add"/> for each call they
    /// get, as "GameObjectName.Method", and a test reads the calls in order with UdonHost.Calls.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UdonCallLog : UdonSharpBehaviour
    {
        /// <summary>The recorded calls, each followed by a semicolon.</summary>
        public string Calls = "";

        public void Add(string call)
        {
            Calls += call + ";";
        }
    }
}

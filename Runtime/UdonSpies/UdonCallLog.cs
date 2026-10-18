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
        /// <summary>The recorded calls, in the order they arrived.</summary>
        public string[] Calls = new string[0];

        public void Add(string call)
        {
            var calls = new string[Calls.Length + 1];
            System.Array.Copy(Calls, calls, Calls.Length);
            calls[Calls.Length] = call;
            Calls = calls;
        }
    }
}

using System.Collections.Generic;
using UdonSharp;

namespace UdonTestKit
{
    /// <summary>
    /// A listener double for code that calls UdonSharpBehaviours by method name, such as
    /// SendCustomEvent: counts calls to CallbackA and CallbackB, and appends
    /// "GameObjectName.CallbackX" to Log when one is set. Share one Log between recorders to
    /// see the order calls arrived in across them.
    /// </summary>
    public class CallbackRecorder : UdonSharpBehaviour
    {
        public List<string> Log;
        public int CallbackACount;
        public int CallbackBCount;

        public void CallbackA()
        {
            CallbackACount++;
            Log?.Add($"{name}.CallbackA");
        }

        public void CallbackB()
        {
            CallbackBCount++;
            Log?.Add($"{name}.CallbackB");
        }
    }
}

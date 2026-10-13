using UdonSharp;

namespace Tsvrc.UdonTestKit.Tests
{
    // Runs as Udon in the kit tests. _Run counts its calls in Runs, _Record adds the call to Log, and
    // _Throw throws.
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UdonProbe : UdonSharpBehaviour
    {
        public UdonCallLog Log;
        public int Runs;

        public void _Run()
        {
            Runs++;
        }

        public void _Record()
        {
            Log.Add(name + "._Record");
        }

        public void _Throw()
        {
            UdonSharpBehaviour missing = null;
            missing.SendCustomEvent(nameof(_Run));
        }
    }
}

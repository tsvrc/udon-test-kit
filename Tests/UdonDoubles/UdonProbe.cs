using UdonSharp;
using UnityEngine;

namespace Tsvrc.UdonTestKit.Tests
{
    // Runs as Udon in the kit tests. _Run counts its calls in Runs, _RunAfterAFrame and
    // _RunAfterHalfASecond delay a call to _Run, _Record adds the call to Log, and _Throw throws.
    // EditorOnlyMarker is in its program only when it was compiled for the Editor.
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UdonProbe : UdonSharpBehaviour
    {
        public const string EditorOnlyMarker = "UdonProbe.EditorOnlyMarker";

        public UdonCallLog Log;
        public int Runs;

        public void _Run()
        {
            Runs++;
        }

        public void _RunAfterAFrame()
        {
            SendCustomEventDelayedFrames(nameof(_Run), 1);
        }

        public void _RunAfterHalfASecond()
        {
            SendCustomEventDelayedSeconds(nameof(_Run), 0.5f);
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

        public void _LogEditorOnlyMarker()
        {
#if UNITY_EDITOR
            Debug.Log(EditorOnlyMarker);
#endif
        }
    }
}

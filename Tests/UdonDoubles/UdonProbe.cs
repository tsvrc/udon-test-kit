using UdonSharp;
using VRC.SDKBase;

namespace Tsvrc.UdonTestKit.Tests.Learning
{
    // Runs as Udon in the learning tests about calls between behaviours. A test sets its fields and
    // calls its parameterless methods by name; what it saw goes to Calls or to its public fields.
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UdonProbe : UdonSharpBehaviour
    {
        public UdonCallLog Calls;
        public UdonSharpBehaviour Target;
        public UdonSharpBehaviour[] Targets;
        public bool TargetIsValid;
        public bool TargetIsNull;
        public bool CallReturned;
        public string MethodName;

        private bool _started;
        private bool _calledBack;

        private void Start()
        {
            _started = true;
            if (Calls != null)
            {
                Calls.Add(name + ".Start");
            }
        }

        public void _Record()
        {
            Calls.Add(name + (_started ? "._Record" : "._RecordBeforeStart"));
        }

        public void _CallTarget()
        {
            Target.SendCustomEvent(nameof(_Throw));
            CallReturned = true;
        }

        // The two methods below read the first item of Targets into a local, as a loop over an array of
        // behaviours does.
        public void _CheckFirstTarget()
        {
            UdonSharpBehaviour target = Targets[0];
            TargetIsValid = Utilities.IsValid(target);
            TargetIsNull = (object)target == null;
        }

        public void _CallFirstTarget()
        {
            UdonSharpBehaviour target = Targets[0];
            target.SendCustomEvent(nameof(_Record));
            CallReturned = true;
        }

        public void _CallTargetByName()
        {
            Target.SendCustomEvent(MethodName);
            CallReturned = true;
        }

        public void _Throw()
        {
            UdonSharpBehaviour missing = null;
            missing.SendCustomEvent(nameof(_Record));
        }

        // Records 0, 1 and 2. On the first pass, Target calls _Loop again before this call returns.
        public void _Loop()
        {
            for (int i = 0; i < 3; i++)
            {
                Calls.Add(name + "." + i);
                if (!_calledBack)
                {
                    _calledBack = true;
                    Target.SendCustomEvent(nameof(_CallBack));
                }
            }
        }

        public void _CallBack()
        {
            Target.SendCustomEvent(nameof(_Loop));
        }
    }
}

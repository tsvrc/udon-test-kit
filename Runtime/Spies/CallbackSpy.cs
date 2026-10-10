using System;
using System.Runtime.CompilerServices;
using UdonSharp;

namespace Tsvrc.TestKit
{
    /// <summary>
    /// A test spy for code that calls an UdonSharpBehaviour's methods by name, such as
    /// SendCustomEvent: each call to one of its callbacks is added to the CallLog that created it,
    /// as "GameObjectName.CallbackName". Create spies with <see cref="CallLog.CreateSpy"/>.
    /// For callbacks with names of your own, subclass it in a non-Editor assembly and call
    /// <see cref="Record"/> from each method.
    /// </summary>
    public class CallbackSpy : UdonSharpBehaviour
    {
        internal CallLog Calls;

        public void CallbackA() => Record();

        public void CallbackB() => Record();

        protected void Record([CallerMemberName] string callback = null)
        {
            if (Calls == null)
            {
                throw new InvalidOperationException($"{name} wasn't created by a CallLog, so it has nowhere to record {callback}. Use CallLog.CreateSpy.");
            }
            Calls.Add($"{name}.{callback}");
        }
    }
}

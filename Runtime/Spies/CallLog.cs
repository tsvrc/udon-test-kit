using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UdonTestKit
{
    /// <summary>
    /// The calls a test's spies received, in the order they arrived, as "GameObjectName.CallbackName".
    /// Disposing it destroys the spies it created, so a test with <c>using var calls = new CallLog();</c>
    /// needs no teardown.
    /// </summary>
    public sealed class CallLog : IReadOnlyList<string>, IDisposable
    {
        private readonly List<string> _calls = new List<string>();
        private readonly List<CallbackSpy> _spies = new List<CallbackSpy>();

        /// <summary>Creates a spy on a new GameObject with this name.</summary>
        public CallbackSpy CreateSpy(string name = "Spy") => CreateSpy<CallbackSpy>(name);

        /// <summary>Creates a spy of your own subclass on a new GameObject with this name.</summary>
        public TSpy CreateSpy<TSpy>(string name) where TSpy : CallbackSpy
        {
            var spy = new GameObject(name).AddComponent<TSpy>();
            spy.Calls = this;
            _spies.Add(spy);
            return spy;
        }

        /// <summary>Forgets the calls so far, such as the ones a test's setup caused.</summary>
        public void Clear() => _calls.Clear();

        public void Dispose()
        {
            foreach (CallbackSpy spy in _spies)
            {
                if (spy != null)
                {
                    Object.DestroyImmediate(spy.gameObject);
                }
            }
            _spies.Clear();
        }

        public int Count => _calls.Count;

        public string this[int index] => _calls[index];

        public IEnumerator<string> GetEnumerator() => _calls.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal void Add(string call) => _calls.Add(call);
    }
}

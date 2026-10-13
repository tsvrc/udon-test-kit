using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UdonSharp;
using UdonSharpEditor;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.Udon;
using VRC.Udon.Common;
using Object = UnityEngine.Object;

namespace Tsvrc.UdonTestKit
{
    // Runs UdonSharp behaviours as their compiled Udon programs instead of as their C# classes, so a
    // test sees what Udon does. An UdonBehaviour added at runtime runs nothing until its compiled
    // program is assigned and UdonManager registers it, so Create does both. Disposing it destroys
    // what it created.
    public sealed class UdonHost : IDisposable
    {
        private readonly List<GameObject> _hosts = new List<GameObject>();

        // Creates a GameObject with this name, running the program of T.
        public UdonBehaviour Create<T>(string name, bool active = true) where T : UdonSharpBehaviour
        {
            UdonSharpProgramAsset program = ProgramAssetOf(typeof(T));
            var host = new GameObject(name);
            host.SetActive(false);
            _hosts.Add(host);
            var behaviour = host.AddComponent<UdonBehaviour>();
            behaviour.programSource = program;
            behaviour.AssignProgramAndVariables(program.SerializedProgramAsset, new UdonVariableTable());
            host.SetActive(active);
            UdonManager.Instance.RegisterUdonBehaviour(behaviour);
            return behaviour;
        }

        public void Dispose()
        {
            foreach (GameObject host in _hosts)
            {
                if (host != null)
                {
                    Object.DestroyImmediate(host);
                }
            }
            _hosts.Clear();
        }

        internal static UdonSharpProgramAsset ProgramAssetOf(Type udonSharpClass)
        {
            UdonSharpProgramAsset program = UdonSharpEditorUtility.GetUdonSharpProgramAsset(udonSharpClass);
            if (program == null)
            {
                throw new InvalidOperationException($"{udonSharpClass.Name} has no UdonSharp program asset. Create one with Assets > Create > VRChat > Udon > Udon C# Program Asset and set its Source Script.");
            }
            return program;
        }

        // The calls an UdonCallLog recorded, in order.
        public static string[] Calls(UdonBehaviour log)
        {
            var calls = (string)log.GetProgramVariable(nameof(UdonCallLog.Calls));
            return calls.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        // The error Udon logs when it halts a behaviour, and the start of the copy UdonSharp logs later.
        internal const string HaltError = "An exception occurred during Udon execution, this UdonBehaviour will be halted";
        internal const string UdonSharpCopy = "Udon runtime exception detected!";

        // Runs act, which makes an Udon behaviour throw, and waits for the two errors that follow:
        // the one that halts the behaviour, and the copy UdonSharp logs on a later frame. Waiting keeps
        // that copy out of the next test.
        public static IEnumerator ExpectUdonException(Action act)
        {
            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape(HaltError)));
            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape(UdonSharpCopy)));

            bool copied = false;
            void OnLog(string message, string stackTrace, LogType type)
            {
                if (message.Contains(UdonSharpCopy))
                {
                    copied = true;
                }
            }

            Application.logMessageReceived += OnLog;
            try
            {
                act();
                for (int frame = 0; frame < 10 && !copied; frame++)
                {
                    yield return null;
                }
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
            }
        }
    }
}

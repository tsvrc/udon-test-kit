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
    /// <summary>
    /// Runs UdonSharp behaviours as their compiled Udon programs, not as their C# classes, so a test sees
    /// what Udon does. Disposing it destroys the GameObjects it created.
    /// </summary>
    public sealed class UdonHost : IDisposable
    {
        private readonly List<GameObject> _hosts = new List<GameObject>();

        /// <summary>Creates a GameObject named <paramref name="name"/> with an UdonBehaviour running the
        /// compiled program of <typeparamref name="T"/>, from its UdonSharp program asset. When
        /// <paramref name="active"/> is false, the GameObject is inactive and the behaviour starts once
        /// it's activated.</summary>
        public UdonBehaviour Create<T>(string name, bool active = true) where T : UdonSharpBehaviour
        {
            UdonSharpProgramAsset program = ProgramAssetOf(typeof(T));
            // An UdonBehaviour added at runtime runs nothing until it has its compiled program and
            // UdonManager registers it.
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

        /// <summary>The calls an <see cref="UdonCallLog"/> recorded, in the order they arrived.</summary>
        public static string[] Calls(UdonBehaviour log)
        {
            var calls = (string)log.GetProgramVariable(nameof(UdonCallLog.Calls));
            return calls.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        // The error Udon logs when it halts a behaviour, and the start of the copy UdonSharp logs later.
        internal const string HaltError = "An exception occurred during Udon execution, this UdonBehaviour will be halted";
        internal const string UdonSharpCopy = "Udon runtime exception detected!";

        /// <summary>Runs <paramref name="act"/>, which makes an Udon behaviour throw, and expects the error
        /// Udon logs as it halts the behaviour. Yield it from a <c>[UnityTest]</c>: it then waits a few
        /// frames for the copy of that error UdonSharp logs on a later frame, and expects the copy too,
        /// which keeps it out of the next test.</summary>
        public static IEnumerator ExpectUdonException(Action act)
        {
            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape(HaltError)));

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

            // UdonSharp doesn't log the copy when it watches the VRChat client's log and finds no
            // VRChat installation, so the copy is expected only once it came.
            if (copied)
            {
                LogAssert.Expect(LogType.Error, new Regex(Regex.Escape(UdonSharpCopy)));
            }
        }
    }
}

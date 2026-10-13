using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VRC.Udon;

namespace Tsvrc.UdonTestKit.Tests.Learning
{
    // An UdonBehaviour added to a GameObject at runtime loads its program from the private
    // serializedProgramAsset field, which only the inspector fills in, and runs it once
    // UdonManager.RegisterUdonBehaviour is called.
    // Relied on by: UdonHost (Create does both).
    public class UdonRuntimeBehaviourTests
    {
        private readonly List<GameObject> _hosts = new List<GameObject>();

        [TearDown]
        public void TearDown()
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

        [Test]
        public void RegisterUdonBehaviour_ProgramSourceOnly_RunsNothing()
        {
            // Arrange
            UdonBehaviour behaviour = Create(setSerializedProgram: false);

            // Act
            UdonManager.Instance.RegisterUdonBehaviour(behaviour);

            // Assert
            Assert.That(Runs(behaviour), Is.False);
        }

        [Test]
        public void SerializedProgramSet_NotRegistered_RunsNothing()
        {
            // Arrange
            UdonBehaviour behaviour = Create(setSerializedProgram: true);

            // Act and assert
            Assert.That(Runs(behaviour), Is.False);
        }

        [Test]
        public void RegisterUdonBehaviour_SerializedProgramSet_RunsIt()
        {
            // Arrange
            UdonBehaviour behaviour = Create(setSerializedProgram: true);

            // Act
            UdonManager.Instance.RegisterUdonBehaviour(behaviour);

            // Assert
            Assert.That(Runs(behaviour), Is.True);
        }

        private UdonBehaviour Create(bool setSerializedProgram)
        {
            AbstractUdonProgramSource program = UdonHost.FindProgram(typeof(UdonArrayProbe));
            var host = new GameObject(nameof(UdonRuntimeBehaviourTests));
            host.SetActive(false);
            _hosts.Add(host);
            var behaviour = host.AddComponent<UdonBehaviour>();
            behaviour.programSource = program;
            if (setSerializedProgram)
            {
                UdonHost.SerializedProgramField.SetValue(behaviour, program.SerializedProgramAsset);
            }
            host.SetActive(true);
            return behaviour;
        }

        // Whether _FindName ran and wrote its result.
        private static bool Runs(UdonBehaviour behaviour)
        {
            behaviour.SetProgramVariable(nameof(UdonArrayProbe.Names), new[] { "Ev" });
            behaviour.SetProgramVariable(nameof(UdonArrayProbe.SearchName), "Missing");
            behaviour.SetProgramVariable(nameof(UdonArrayProbe.SearchStart), 0);
            behaviour.SendCustomEvent(nameof(UdonArrayProbe._FindName));
            return behaviour.GetProgramVariable(nameof(UdonArrayProbe.Found)) is int found && found == -1;
        }
    }
}

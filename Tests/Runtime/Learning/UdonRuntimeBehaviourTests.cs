using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VRC.Udon;

namespace Tsvrc.UdonTestKit.Tests.Learning
{
    // An UdonBehaviour added to a GameObject at runtime loads its program from the private
    // serializedProgramAsset field, which only the inspector fills in, and runs it once
    // UdonManager.RegisterUdonBehaviour is called. So UdonHost.Create does both.
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
            AbstractUdonProgramSource program = UdonHost.FindProgram(typeof(UdonProbe));
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

        // Whether _Run ran and counted the call.
        private static bool Runs(UdonBehaviour behaviour)
        {
            behaviour.SendCustomEvent(nameof(UdonProbe._Run));
            return behaviour.GetProgramVariable(nameof(UdonProbe.Runs)) is int runs && runs == 1;
        }
    }
}

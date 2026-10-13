using System.Collections.Generic;
using NUnit.Framework;
using UdonSharp;
using UdonSharpEditor;
using UnityEngine;
using VRC.Udon;
using VRC.Udon.Common;

namespace Tsvrc.UdonTestKit.Tests.Learning
{
    // An UdonBehaviour added to a GameObject at runtime doesn't load the compiled program of its
    // programSource. It runs a program given to AssignProgramAndVariables, once
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
            UdonBehaviour behaviour = Create(assignProgram: false);

            // Act
            UdonManager.Instance.RegisterUdonBehaviour(behaviour);

            // Assert
            Assert.That(Runs(behaviour), Is.False);
        }

        [Test]
        public void AssignProgramAndVariables_NotRegistered_RunsNothing()
        {
            // Arrange
            UdonBehaviour behaviour = Create(assignProgram: true);

            // Act and assert
            Assert.That(Runs(behaviour), Is.False);
        }

        [Test]
        public void RegisterUdonBehaviour_ProgramAssigned_RunsIt()
        {
            // Arrange
            UdonBehaviour behaviour = Create(assignProgram: true);

            // Act
            UdonManager.Instance.RegisterUdonBehaviour(behaviour);

            // Assert
            Assert.That(Runs(behaviour), Is.True);
        }

        private UdonBehaviour Create(bool assignProgram)
        {
            UdonSharpProgramAsset program = UdonSharpEditorUtility.GetUdonSharpProgramAsset(typeof(UdonProbe));
            var host = new GameObject(nameof(UdonRuntimeBehaviourTests));
            host.SetActive(false);
            _hosts.Add(host);
            var behaviour = host.AddComponent<UdonBehaviour>();
            behaviour.programSource = program;
            if (assignProgram)
            {
                behaviour.AssignProgramAndVariables(program.SerializedProgramAsset, new UdonVariableTable());
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

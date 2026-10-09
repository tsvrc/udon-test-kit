using System;
using System.Collections;
using UnityEngine;
using VRC.SDK3.ClientSim;
using VRC.SDK3.Components;
using Object = UnityEngine.Object;

namespace UdonTestKit.Tests.Helpers
{
    // ClientSim started and ended directly, without ClientSimSession, so a Problem test sees what
    // ClientSim does on its own.
    internal sealed class RawClientSim
    {
        private GameObject _descriptor;

        // Starts ClientSim and returns in the same frame. Pass events to watch its events.
        public void Begin(bool withDescriptor = true, ClientSimEventDispatcher events = null)
        {
            if (withDescriptor)
                _descriptor = CreateDescriptor();

            var settings = new ClientSimSettings
            {
                enableClientSim = true,
                spawnPlayer = true,
                localPlayerIsMaster = true,
                initializationDelay = 0f,
            };
            events = events ?? new ClientSimEventDispatcher();
            ClientSimRuntimeLoader.BeginUnityTesting(settings, events);
            ClientSimRuntimeLoader.StartClientSim(settings, events);
        }

        // Starts ClientSim and returns in the frame ClientSim reports ready.
        public IEnumerator Start(ClientSimEventDispatcher events = null)
        {
            bool ready = false;
            events = events ?? new ClientSimEventDispatcher();
            events.Subscribe<ClientSimReadyEvent>(_ => ready = true);
            Begin(events: events);

            float deadline = Time.realtimeSinceStartup + 10f;
            while (!ready)
            {
                if (Time.realtimeSinceStartup > deadline)
                    throw new TimeoutException("ClientSim didn't get ready within 10 seconds.");
                yield return null;
            }
        }

        public void End()
        {
            if (ClientSimMain.HasInstance())
                ClientSimMain.RemoveInstance();
            ClientSimRuntimeLoader.EndUnityTesting();

            if (_descriptor != null)
                Object.DestroyImmediate(_descriptor);
            _descriptor = null;
        }

        private static GameObject CreateDescriptor()
        {
            var descriptorObject = new GameObject("__ProblemTestSceneDescriptor");
            var spawn = new GameObject("__ProblemTestSpawn");
            spawn.transform.SetParent(descriptorObject.transform);
            descriptorObject.AddComponent<VRCSceneDescriptor>().spawns = new[] { spawn.transform };
            return descriptorObject;
        }
    }
}

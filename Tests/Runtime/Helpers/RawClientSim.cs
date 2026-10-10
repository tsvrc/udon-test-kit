using System.Collections;
using UnityEngine;
using VRC.SDK3.ClientSim;
using Object = UnityEngine.Object;

namespace Tsvrc.TestKit.Tests.Helpers
{
    // ClientSim started and ended without ClientSimSession, so a learning test sees what ClientSim
    // does on its own.
    internal sealed class RawClientSim
    {
        private GameObject _descriptor;

        // Starts ClientSim and returns in the same frame. Pass events to watch its events.
        public void Begin(bool withDescriptor = true, ClientSimEventDispatcher events = null)
        {
            if (withDescriptor)
            {
                _descriptor = ClientSimSession.CreateSceneDescriptor(Vector3.zero);
            }

            var settings = new ClientSimSettings
            {
                enableClientSim = true,
                spawnPlayer = true,
                localPlayerIsMaster = true,
                initializationDelay = 0f,
            };
            events ??= new ClientSimEventDispatcher();
            ClientSimRuntimeLoader.BeginUnityTesting(settings, events);
            ClientSimRuntimeLoader.StartClientSim(settings, events);
        }

        // Starts ClientSim and returns in the frame ClientSim reports ready.
        public IEnumerator Start()
        {
            bool ready = false;
            var events = new ClientSimEventDispatcher();
            events.Subscribe<ClientSimReadyEvent>(_ => ready = true);
            Begin(events: events);
            yield return new WaitUntil(() => ready);
        }

        public void End()
        {
            if (ClientSimMain.HasInstance())
            {
                ClientSimMain.RemoveInstance();
            }
            ClientSimRuntimeLoader.EndUnityTesting();

            if (_descriptor != null)
            {
                Object.DestroyImmediate(_descriptor);
            }
            _descriptor = null;
        }
    }
}

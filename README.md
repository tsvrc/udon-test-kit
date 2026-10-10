# Udon Test Kit

Test a VRChat world with Unity Test Framework. Play Mode tests run a real ClientSim session with
real `VRCPlayerApi` players, and the kit works around the VRChat SDK and ClientSim problems that
otherwise break those tests.

The learning tests in `Tests/*/Learning` check the VRChat SDK, ClientSim and Unity behavior the
kit relies on. When an update changes that behavior, they fail and name the kit code to revisit.

Requires Unity 2022.3 and VRChat Worlds SDK 3.8.2 or later. Earlier SDKs stop Play Mode when a
scene has no scene descriptor, and Unity Test Framework's test scene has none.

## Set up

1. Add the package to your world. The VRChat Creator Companion installs it in `Packages`.
2. Select a folder in the Project window and choose **Assets > Create > Udon Test Kit > Test
   Assemblies**. You get a `Tests` folder with an Edit Mode and a Play Mode test assembly, each
   with an example test that passes. They reference Unity Test Framework, the kit, UdonSharp, the
   VRChat SDK, ClientSim and every assembly definition of yours under `Assets`.
3. Run the tests from Unity's Test Runner window, or from the command line.

Scripts without an assembly definition compile into `Assembly-CSharp`, which an assembly
definition can't reference. Edit Mode tests for them work from any `Editor` folder, and the kit is
available there too. Play Mode tests need those scripts moved into an assembly definition. The
menu item warns you when this applies.

If a Play Mode test fails before ClientSim starts, naming project settings, run **Tools > Udon
Test Kit > Fix ClientSim Project Settings** once. It changes each setting ClientSim's own check
rejects and logs what it changed.

## Play Mode tests

Extend `ClientSimTestBase` and start its `Session` in a test. The session ends after every test.

```csharp
using System.Collections;
using NUnit.Framework;
using UdonTestKit;
using UnityEngine.TestTools;
using VRC.SDKBase;

public class PlayerCountTests : ClientSimTestBase
{
    [UnityTest]
    public IEnumerator SpawnRemotePlayer_InClientSim_JoinsThePlayerList()
    {
        yield return Session.Start();

        VRCPlayerApi remote = Session.SpawnRemotePlayer("Remote");

        Assert.That(VRCPlayerApi.GetPlayerCount(), Is.EqualTo(2));
        Assert.That(remote.isLocal, Is.False);
    }
}
```

`Session.Start` returns once the local player has joined and its saved data is restored. Pass
`localPlayerIsMaster: false` to make another player the master. `SpawnRemotePlayer` and
`RemovePlayer` take effect before they return, and `ClientSimSession.FindPlayerByName` finds a
player by display name.

To skip the base class, create a `ClientSimSession` yourself and call its `End` in your teardown.
One session can also serve every test in a class: start it in a `[UnitySetUp]` when it isn't
running yet, and end it in `[OneTimeTearDown]`. Tests that share a session also share whatever
the earlier ones changed, so keep that for tests that only read.

### Choosing the scene

By default every test runs in Unity Test Framework's own test scene, which the whole run shares. To
run tests in a scene of their own, pass a `TestScene` to the base class:

```csharp
public class LobbyTests : ClientSimTestBase
{
    public LobbyTests() : base(TestScene.FromAsset("Assets/Scenes/MyWorld.unity"), TestSceneLifetime.PerFixture)
    {
    }
}
```

- `TestScene.Empty("Lobby")` is a new empty scene. `TestScene.FromAsset(path)` is a scene asset,
  such as your world's own scene; it doesn't need to be in the build settings.
- `TestSceneLifetime.PerTest`, the default, loads a fresh copy for every test and unloads it after,
  so each test starts from the same scene. `TestSceneLifetime.PerFixture` loads it once for every
  test in the class.
- The scene loads before your own setup and becomes the active scene, so ClientSim and the objects
  your test creates go into it. It unloads after ClientSim has ended, destroying everything in it.

The session adds a scene descriptor only when the active scene has none. When the active scene has
its own, such as your world's scene, the local player spawns at that descriptor's spawn point.

`TestScene` also works without the base class: `yield return scene.Load()` in a `[UnitySetUp]`, and
`scene.Unload()` once ClientSim has ended, such as in a `[UnityTearDown]`.

## Saved data

ClientSim saves each player's PlayerData and PlayerObjects in your project's `ClientSimStorage`
folder, one file per player and scene. Ending a session stops it from saving, so the next session
in the same scene starts from what the last one saved, like a player who leaves and rejoins. That
lets one test check that data survives a rejoin:

```csharp
yield return Session.Start();
PlayerData.SetString("coins", "10");
yield return null; // ClientSim has saved it by the next frame
Session.End();

yield return Session.Start();

Assert.That(PlayerData.TryGetString(Networking.LocalPlayer, "coins", out string coins), Is.True);
Assert.That(coins, Is.EqualTo("10"));
```

Saved data stays on disk after a test, so a later test in the same scene starts from it too. Call
`ClientSimSession.DeleteSavedData()` to delete what ClientSim saved for the active scene, such as
at the start of a test that needs none.

Unity Test Framework gives its test scene a new name on every run, so each run leaves a few files
in `ClientSimStorage` that no later run reads. Delete them whenever you like.

## Test helpers

`CallbackRecorder` is an `UdonSharpBehaviour` that counts calls to `CallbackA` and `CallbackB`,
and adds `"GameObjectName.CallbackX"` to its `Log` list when you set one. Use it as the target of
code that calls behaviours by method name, such as `SendCustomEvent`.

`FakeOwnershipSyncable` gives a GameObject an owner of its own. In ClientSim the master owns every
plain GameObject, and `Networking.SetOwner` can't change that. Add this component, then call its
`SetOwner(playerId)` or `Networking.SetOwner`. Until then nobody owns the GameObject.

`PrivateFieldAccess` reads and writes fields and calls methods by name, whatever their access
level, including private members of a base class. A name that isn't found fails the test.

## What the kit fixes on its own

When Play Mode starts, the VRChat SDK strips the listeners Unity Test Framework reports Play Mode
results through, so a Play Mode run never finishes. While the kit is installed, the SDK keeps them.

ClientSim opens its settings window when its project settings check fails, and in batch mode that
window logs errors that fail the test without naming the cause. `Session.Start` fails first and
names the settings.

ClientSim never destroys the hidden object that saves a player's data, and that object keeps
saving after the session ends, over whatever a later session saves. `Session.End` destroys it.

The kit compiles only when `UNITY_INCLUDE_TESTS` is defined, so it's never part of a world build.

## Running tests from the command line

`Scripts~/Invoke-UnityTests.ps1` runs a Unity project's tests from PowerShell on Windows. It's a
single file that doesn't use the kit or anything else beyond Unity Test Framework, so you can copy
it into any Unity project, such as its root folder. It finds the project by looking up from its
own folder. Unity doesn't import folders ending in `~`, so a `.unitypackage` of the kit doesn't
include the script; take it from the kit's repository.

Close the Unity Editor first: batch mode can't open a project another Editor has open. Then:

```powershell
.\Invoke-UnityTests.ps1
```

It runs your project's tests: those in every assembly definition under `Assets`, and in scripts
without one, such as tests in `Editor` folders. Tests that come with packages, such as the VRChat
SDK's, don't run. Each test mode starts Unity once and prints failed, inconclusive and skipped
tests with their messages, then counts the rest. The script exits with 1 when a test failed or was
inconclusive, or when a run didn't finish. An inconclusive test is an `Assume` that didn't hold,
so it proved nothing.

`Get-Help .\Invoke-UnityTests.ps1 -Detailed` shows its options:

| Option           | Default                                                                       |
| ---------------- | ----------------------------------------------------------------------------- |
| `-TestMode`      | `All` (or `EditMode`, `PlayMode`)                                             |
| `-AssemblyNames` | your project's assemblies; separate several with `;`                          |
| `-ProjectPath`   | the nearest project above the script's folder, then above the current folder  |
| `-UnityPath`     | the version in `ProjectVersion.txt`, in Unity Hub's default folder            |
| `-ResultsPath`   | a new folder under the temp folder, for each mode's results XML and Unity log |

If PowerShell refuses to run the script because of its execution policy, allow local scripts once
with `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`.

## Developing the kit

The kit's own tests compile only when `UDON_TEST_KIT_TESTS` is defined, so they never show up in a
world that uses the kit. To run them, add that symbol to **Project Settings > Player > Scripting
Define Symbols** in the project you develop the kit in.

Keep the kit under that project's `Assets` folder, where the script's default finds its tests, and
run `Scripts~/Invoke-UnityTests.ps1` from where it is. Never have two copies of the kit in one
project, such as a checkout and a VCC install: two assemblies named `UdonTestKit` stop the project
compiling.

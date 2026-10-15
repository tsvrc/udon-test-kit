# Contributing to Udon Test Kit

Bug reports, fixes and new test helpers are welcome. Before you build a new helper or change a
public API, open an issue to agree on its shape, so the code you write is the code that gets
merged. Questions go in [GitHub Discussions](https://github.com/orgs/tsvrc/discussions).

## Reporting a bug

Use the bug report form. The kit relies on internals of the VRChat SDK, which ClientSim ships in, so
the SDK version matters most, and so does how you ran the test: the Test Runner window and batch
mode fail in different ways. A failing test that shows the problem is the most useful thing you can
attach.

## Setup

The kit tests need a VRChat world project to run in.

1. Create a world project with Unity 2022.3 and VRChat Worlds SDK 3.8.2 or later, through the
   [VRChat Creator Companion](https://vcc.docs.vrchat.com/).
2. Clone this repository into the `Assets` folder of that project, for example as
   `Assets/udon-test-kit`. Don't install the kit through the Creator Companion as well: two copies
   stop the project compiling.
3. Add `TSVRC_UDON_TEST_KIT_TESTS` to **Project Settings > Player > Scripting Define Symbols**. The
   kit tests compile only when it's defined, so they never show up in a world that uses the kit.

## Running tests

Run both modes before opening a pull request. In the Editor, use the EditMode and PlayMode tabs of
**Window > General > Test Runner**. From PowerShell, with the Editor closed:

```powershell
.\Scripts~\Invoke-UnityTests.ps1
```

On Linux, from Bash, with the Windows build target. In batch mode, Unity opens a project it hasn't
imported yet on the default target, which is Linux on Linux.

```bash
./Scripts~/run_unity_tests.sh -- -buildTarget win64
```

Both scripts run the tests of every assembly definition under `Assets`, which is why the kit lives
there while you develop it. The options of the PowerShell script are in the
[Invoke-UnityTests.ps1 reference](https://tsvrc.com/docs/udon-test-kit/reference/invoke-unity-tests),
and `run_unity_tests.sh --help` lists the same ones for Bash.

The scripts have tests of their own in `Tests/Scripts~`, which run without Unity:
`Test-InvokeUnityTests.ps1` from Windows PowerShell and `test_run_unity_tests.sh` from Bash. A
pull request that changes a script runs both.

## Learning tests

The tests in `Tests/Editor/Learning` and `Tests/Runtime/Learning` don't test the kit. They pin down
the VRChat SDK, ClientSim, UdonSharp and Unity behavior the kit relies on, such as which owners
ClientSim allows or which listeners the SDK strips when Play Mode starts. When an update changes
that behavior, a learning test fails and its comment names the kit code to revisit.

Add one whenever kit code depends on behavior you found by trying it rather than in documentation.
When one fails after an update, change the kit code it names, then update the test to the new
behavior.

## Coding conventions

Match the style of the surrounding file. A few things aren't obvious from reading one file:

- Kit code in `Runtime/` calls `UnityEditor` directly, without `#if UNITY_EDITOR`. The `Tsvrc.UdonTestKit`
  assembly compiles only when `UNITY_INCLUDE_TESTS` is defined, which keeps it out of player
  builds, and `PlayerBuildTests` fails if that stops being true.
- Code that runs as Udon is the exception. UdonSharp compiles only player assemblies for a world
  upload, so `UdonCallLog` is in `Tsvrc.UdonTestKit.UdonSpies`, and the test doubles that run as
  Udon are in `Tests/UdonDoubles/`. Both are player assemblies with a U# assembly definition, and
  each behaviour in them needs a program asset: **Create > U# Script** makes the script and its
  program asset together.
- Test names read `Method_Condition_Result`, such as `Fix_AllSettingsValid_AppliesNothing`, and the
  body is split with `// Arrange`, `// Act` and `// Assert` comments.
- A comment says why the code does something when that isn't obvious, usually the SDK or ClientSim
  behavior it works around. It states that reason itself instead of pointing to another file, a
  test or a notes document, so it still makes sense when read alone.

A change to what the kit does needs a test that fails without it.

## Documentation

The kit documentation is on [tsvrc.com](https://tsvrc.com/docs/udon-test-kit/intro), built from
`docs/udon-test-kit/` in the [tsvrc/docs](https://github.com/tsvrc/docs) repository. A change to
what users call or see needs a matching pull request there.

## Submitting a pull request

1. Sign off every commit (`git commit -s`) per the
   [Developer Certificate of Origin](https://developercertificate.org/). It certifies you wrote the
   change, or otherwise have the right to submit it under the MIT license, so only you can add it,
   never a tool. A check fails the pull request if any of its commits isn't signed off.
2. If AI tools helped, say which parts and how in the pull request description. You're the author
   either way: read, understand and test all of it before you submit it, and be ready to explain it
   in review. A pull request that's mostly unreviewed AI output, and would take longer to review
   than it saves, will be closed.
3. Open the pull request, describing what it changes and why. Community conduct is covered by the
   [org-wide Code of Conduct](https://github.com/tsvrc/.github/blob/main/CODE_OF_CONDUCT.md).

## Reporting a security vulnerability

Don't open a public issue for this. See [SECURITY.md](SECURITY.md).

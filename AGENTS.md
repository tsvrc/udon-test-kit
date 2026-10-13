# AGENTS.md

Udon Test Kit is a set of Unity Test Framework helpers for VRChat worlds (Unity 2022.3, VPM package
`com.tsvrc.udon-test-kit`). [CONTRIBUTING.md](CONTRIBUTING.md) covers setup, learning tests, coding
conventions and pull requests; follow it. This file only adds what it leaves out.

## Layout

- `Runtime/` is the kit itself: the `Tsvrc.UdonTestKit` assembly, and in `Runtime/UdonSpies/` the
  `Tsvrc.UdonTestKit.UdonSpies` assembly, for the spies that run as Udon.
- `Tests/Editor/` and `Tests/Runtime/` hold the kit tests, with learning tests in `Learning/`
  under each. `Tests/UdonDoubles/` holds the test doubles that run as Udon, and
  `Tests/TestsOnlyDoubles/` an UdonSharp behaviour in a tests-only assembly, for the learning tests
  about the upload compile.
- `Scripts~/Invoke-UnityTests.ps1` is the command-line test runner, and
  `Scripts~/run_unity_tests.sh` its Linux counterpart.
- User documentation isn't in this repo: it's `docs/udon-test-kit/` in `tsvrc/docs`.

## Running tests

Close the Unity Editor, then run `Scripts~/Invoke-UnityTests.ps1`, or
`Scripts~/run_unity_tests.sh -- -buildTarget win64` on Linux. Add `-TestMode EditMode` or
`-TestMode PlayMode` (`--test-mode` in Bash) to run one mode.

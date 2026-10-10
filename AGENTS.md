# AGENTS.md

Test Kit is a set of Unity Test Framework helpers for VRChat worlds (Unity 2022.3, VPM package
`com.tsvrc.test-kit`). [CONTRIBUTING.md](CONTRIBUTING.md) covers setup, learning tests, coding
conventions and pull requests; follow it. This file only adds what it leaves out.

## Layout

- `Runtime/` is the kit itself, one assembly named `Tsvrc.TestKit`.
- `Tests/Editor/` and `Tests/Runtime/` hold the kit's own tests, with learning tests in `Learning/`
  under each.
- `Scripts~/Invoke-UnityTests.ps1` is the command-line test runner.
- User documentation isn't in this repo: it's `docs/test-kit/` in `tsvrc/docs`.

## Running tests

Close the Unity Editor, then run `Scripts~/Invoke-UnityTests.ps1`. Add `-TestMode EditMode` or
`-TestMode PlayMode` to run one mode.

# Udon Test Kit

[![Release](https://img.shields.io/github/v/release/tsvrc/udon-test-kit?style=flat&label=release&color=58a6ff&labelColor=161b22)](https://github.com/tsvrc/udon-test-kit/releases)
[![Last commit](https://img.shields.io/github/last-commit/tsvrc/udon-test-kit?style=flat&label=last%20commit&color=58a6ff&labelColor=161b22)](https://github.com/tsvrc/udon-test-kit/commits/main)
[![License: MIT](https://img.shields.io/github/license/tsvrc/udon-test-kit?style=flat&color=58a6ff&labelColor=161b22)](LICENSE.md)

Unity Test Framework tests for VRChat worlds, with your UdonSharp behaviours running in a real
ClientSim session.

> [!NOTE]
> I use AI coding tools to draft code and tests, dig through the VRChat SDK, ClientSim and
> UdonSharp source, and rerun checks that would take hours by hand. I design the kit and review each
> AI-drafted change before it's merged, and every behavior of the kit has a test you can
> [run yourself](CONTRIBUTING.md#running-tests).

*Udon Test Kit is a [TsVRC](https://tsvrc.com) project by
[Antony Seas](https://github.com/ToniSeas). It isn't affiliated with, endorsed by, or sponsored by
VRChat Inc.*

## What's in the kit

- ClientSim sessions with the local player, remote players you spawn, ownership and saved data.
- `UdonHost`, which runs behaviours as their compiled Udon programs, since Udon doesn't always do
  what the C# does.
- Spies that record calls made by name, such as `SendCustomEvent`.
- Scenes for tests to run in, either a new empty one or your world scene.
- `UdonSharpUploadCompile` and `BuiltWorld`, to check that your world compiles for upload and what
  its build holds.
- `PrivateFieldAccess` for private members, and `TestRun` to keep editor tooling out of test runs.
- Menu items that create the test assemblies of your world, add the assembly definitions your
  world gets later to them, and fix the project settings ClientSim checks before it starts.
- [`Invoke-UnityTests.ps1`](Scripts~/Invoke-UnityTests.ps1) and
  [`run_unity_tests.sh`](Scripts~/run_unity_tests.sh), which run the tests of any Unity project
  from PowerShell or Bash and report the ones that didn't pass.

## Install

Requires Unity 2022.3 and the VRChat Worlds SDK 3.10.5 or a later 3.x version, in the Editor
on Windows or on Linux with the Windows build target, batch mode included.

Install **Udon Test Kit** (`com.tsvrc.udon-test-kit`) as
[Install a TsVRC package](https://tsvrc.com/docs/install-a-package) describes. The `.unitypackage`
is on the [releases page](https://github.com/tsvrc/udon-test-kit/releases). Then create your test
assemblies with **Assets > Create > TsVRC > Udon Test Kit > Test Assemblies**, as
[Add Udon Test Kit to your project](https://tsvrc.com/docs/udon-test-kit/add-to-your-project)
describes.

## Documentation

The guides and the reference are at
[tsvrc.com/docs/udon-test-kit](https://tsvrc.com/docs/udon-test-kit/intro).

## Contributing

Bug reports, fixes and new helpers are welcome. [CONTRIBUTING.md](CONTRIBUTING.md) covers the setup,
running the tests and opening a pull request. Ask questions in
[GitHub Discussions](https://github.com/orgs/tsvrc/discussions), and report a security issue
privately, as [SECURITY.md](SECURITY.md) describes. The
[Code of Conduct](https://github.com/tsvrc/.github/blob/main/CODE_OF_CONDUCT.md) applies to everyone
taking part.

## License

MIT, see [LICENSE.md](LICENSE.md). The TsVRC name and logo are covered separately, in
[TRADEMARK.md](https://github.com/tsvrc/.github/blob/main/TRADEMARK.md).

# Udon Test Kit

[![Release](https://img.shields.io/github/v/release/tsvrc/udon-test-kit?style=flat&label=release&color=58a6ff&labelColor=161b22)](https://github.com/tsvrc/udon-test-kit/releases)
[![Last commit](https://img.shields.io/github/last-commit/tsvrc/udon-test-kit?style=flat&label=last%20commit&color=58a6ff&labelColor=161b22)](https://github.com/tsvrc/udon-test-kit/commits/main)
[![License: MIT](https://img.shields.io/github/license/tsvrc/udon-test-kit?style=flat&color=58a6ff&labelColor=161b22)](LICENSE.md)

Automated tests for VRChat worlds with Unity Test Framework. Play Mode tests run your UdonSharp
behaviours in a real ClientSim session, with real players.

Docs: [tsvrc.com/docs/udon-test-kit](https://tsvrc.com/docs/udon-test-kit/intro).

*Udon Test Kit is a [TsVRC](https://tsvrc.com) project. It isn't affiliated with, endorsed by, or
sponsored by VRChat Inc.*

## Quick look

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

Full walkthrough: [Write your first test](https://tsvrc.com/docs/udon-test-kit/first-test).

## Install

Requires Unity 2022.3 and the VRChat Worlds SDK 3.8.2 or later.

Install **Udon Test Kit** (`com.tsvrc.udon-test-kit`) as described in
[Install a TsVRC package](https://tsvrc.com/docs/install-a-package); the `.unitypackage` is on the
[releases page](https://github.com/tsvrc/udon-test-kit/releases). Then create your test assemblies:
[Add Udon Test Kit to your project](https://tsvrc.com/docs/udon-test-kit/add-to-your-project).

## Command-line test runner

[`Scripts~/Invoke-UnityTests.ps1`](Scripts~/Invoke-UnityTests.ps1) runs a Unity project's tests
from PowerShell and prints only what failed. It's standalone: copy it into any Unity project, with
or without the kit. See
[Run tests from the command line](https://tsvrc.com/docs/udon-test-kit/how-to/running-tests-from-the-command-line).

## Contributing

Bug reports, fixes and new helpers are welcome. [CONTRIBUTING.md](CONTRIBUTING.md) covers setting
up a project to develop the kit in, running its tests, and opening a pull request. Ask questions in
[GitHub Discussions](https://github.com/orgs/tsvrc/discussions) rather than in an issue.

Found a security issue? Follow [SECURITY.md](SECURITY.md) instead of opening a public issue.
Community conduct is covered by the
[org-wide Code of Conduct](https://github.com/tsvrc/.github/blob/main/CODE_OF_CONDUCT.md).

## License

MIT, see [LICENSE.md](LICENSE.md). The TsVRC name and logo are governed separately, see
[TRADEMARK.md](https://github.com/tsvrc/.github/blob/main/TRADEMARK.md).

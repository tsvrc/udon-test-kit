# Security policy

## Reporting a vulnerability

Don't open a public issue. Use
[GitHub's private vulnerability reporting](https://github.com/tsvrc/udon-test-kit/security/advisories/new)
for this repository instead, so the report isn't visible before a fix is available.

If that's not accessible to you for some reason, contact
[@ToniSeas](https://github.com/ToniSeas) directly.

## Scope

This covers the kit's own code: `Runtime/` and `Scripts~/Invoke-UnityTests.ps1`. The kit runs only
in the Unity Editor and in test runs, never in a world build, but it does write project settings,
delete ClientSim's saved data files and start Unity from the command line.

It doesn't cover the VRChat platform, the VRChat Worlds SDK, ClientSim, UdonSharp or Unity Test
Framework. Report those to [VRChat's support channels](https://hello.vrchat.com/) or to Unity.

## Supported versions

Only the latest release is supported. There's no backport policy for older versions.

## Response

This project is maintained by one person in their spare time. There's no guaranteed response time,
but security reports get priority over other issues.

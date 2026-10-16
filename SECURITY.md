# Security policy

## Reporting a vulnerability

Don't open a public issue. Report it through
[GitHub private vulnerability reporting](https://github.com/tsvrc/udon-test-kit/security/advisories/new),
which keeps the report private until a fix is out. If you can't use it, email
[contact@tsvrc.com](mailto:contact@tsvrc.com).

Say what the vulnerability is, how to reproduce it, which versions it affects, and how to fix or
work around it if you know.

## What happens next

You'll get a reply within 7 days, and updates while I work on it. Once the report is confirmed, I
prepare the fix in a private security advisory, then publish the advisory with the release that
fixes it, crediting you unless you'd rather not be named. The advisory comes out within 90 days of
the report, with a fix if there is one, unless we agree on a later date.

## Scope

This covers the kit code: `Runtime/` and the test runners in `Scripts~/`. The kit runs only in the
Unity Editor and in test runs, never in a world build. It writes test assemblies and project
settings into your project, deletes the ClientSim saved data of the scene under test, builds the
world into a temporary folder and runs the Unity tools that read the build, and the runners start
Unity from the command line.

It doesn't cover the VRChat platform, the VRChat Worlds SDK, ClientSim, UdonSharp or Unity Test
Framework. Report those to [VRChat support](https://hello.vrchat.com/) or to Unity.

## Supported versions

Only the latest release gets security fixes.

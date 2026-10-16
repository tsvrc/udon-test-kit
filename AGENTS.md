# AGENTS.md

Udon Test Kit is a set of Unity Test Framework helpers for VRChat worlds (Unity 2022.3, VPM package
`com.tsvrc.udon-test-kit`). Follow [CONTRIBUTING.md](CONTRIBUTING.md), the guide for every
contributor: its rules apply to you as written. This file adds only what's specific to an agent.

- Run the tests from the command line, as [Running tests](CONTRIBUTING.md#running-tests) shows.
  Unity can't open a project another instance has open, so if the Unity Editor has it open, ask the
  person you work for to close it instead of stopping Unity yourself.
- A change is done when both test modes pass, and for a change to a script in `Scripts~`, its
  script tests too. Say what you ran and give the summary line of each run.

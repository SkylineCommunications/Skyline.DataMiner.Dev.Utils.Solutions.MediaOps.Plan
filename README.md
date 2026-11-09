# Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan

This repository contains the public API of the `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan` NuGet package (C# namespace `Skyline.DataMiner.Solutions.MediaOps.Plan`), used when developing custom solutions based on the MediaOps.PLAN solution.

## Installation

Add the NuGet package to your solution:

```bash
dotnet add package Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan
```

Depending on your project type, one of the following additional packages is also required:

- Automation scripts: `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.Automation`
- Protocols: `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.Protocol`
- GQI Ad-hoc data sources and custom operators: `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.GQI`

> [!NOTE]
> This library targets `.NET Framework 4.8`.

## Documentation

| Document | Description |
| -------- | ----------- |
| [Getting Started](Documentation/Getting%20Started.md) | Installation, prerequisites, and the core concepts (resources, jobs, workflows, recurring jobs, node graphs, properties, relationships) |
| [Quick Reference](Documentation/Quick%20Reference.md) | Copyable snippets for every repository: CRUD, job/recurring job lifecycle, node graphs, properties, relationships, querying, and synchronization |
| [Advanced Topics](Documentation/Advanced%20Topics.md) | State machines, job timing rules, configuration state, data references, eligible resources, SRM synchronization, logging, and installation checks |

External resources:

- [DataMiner Docs](https://docs.dataminer.services/) - Official DataMiner documentation

## Regression testing

Tests use C# and MSTest. The same test assembly supports in-memory simulation for unit tests and a real DataMiner System with MediaOps Plan deployed for integration tests. Simulation is the default.

Testing uses the following projects:

| Project | Purpose |
| --- | --- |
| [DevPack.Tests](./DevPack.Tests) | API and regression tests for properties, resources and resource pools, jobs, workflows, recurring jobs, relationships, querying, and orchestration. |
| [DevPack.UnitTesting](./DevPack.UnitTesting) | In-memory DataMiner simulation and test helpers. This is a support library, not a separate test suite. |
| [QAOpsPackage](./QAOpsPackage/README.md) | Builds the `.dmtest` containing the test assembly, source-built libraries, runtime dependencies, and QAOps pipeline scripts. |

### CI execution

The [Plan CICD](./.github/workflows/Plan-CICD.yml) workflow runs validation and the simulated suite before triggering QAOps integration testing for:

- Approval of the current head commit of a non-draft PR from this repository targeting `main` or an RC branch matching `*.*.X`.
- Manual workflow runs.
- Release tag pushes whose names do not contain `-alpha`.

Pushes and merges to `main` or RC branches run validation and simulated tests but do not automatically trigger QAOps testing.

| Case | Validate | Unit Tests | QAOps Integration Tests | Skyline CICD |
| --- | --- | --- | --- | --- |
| Open or update PR | Yes | Yes | No | No |
| Current-head approval to main | Yes | Yes | Yes | No |
| Current-head approval to RC (`*.*.X`) | Yes | Yes | Yes | No |
| Current-head approval to other branches | Yes | Yes | No | No |
| Manual run | Yes | Yes | Yes | No |
| Push or merge to main or RC | Yes | Yes | No | Yes |
| Tag containing `-alpha` | Yes | Yes | No | Yes |
| Other prerelease tag | Yes | Yes | Yes | Yes |
| Stable release tag | Yes | Yes | Yes | Yes |

Each stage requires the applicable preceding checks to pass. Stable release tags must be contained in the matching `<major>.<minor>.X` branch. NuGet publication runs only on tag pushes: alpha tags publish after simulated tests, while other tags also require successful QAOps tests. Prerelease packages go to GitHub Packages; stable packages go to nuget.org.

The **Unit Tests** job builds [MediaOps Plan.UnitTests.slnf](./MediaOps%20Plan.UnitTests.slnf) with MSBuild and runs the complete test assembly with VSTest and `MEDIAOPS_PLAN_USE_REAL_DMA=false`. Test output is harvested into the runner's temporary directory to avoid Windows path-length limits.

The **Integration Tests** job (`integration_tests`):

1. Builds [QAOpsPackage](./QAOpsPackage/README.md) from the selected commit with a tag-derived or run-specific package version.
2. Revalidates the current-head approval and target branch immediately before submitting an approval-triggered run.
3. Submits the exact built `.dmtest` with `dataminer-qaops test-run-and-wait` to the configured QAOps target.
4. Waits for QAOps to execute the harvested MSTest suite against the real DataMiner System.
5. Propagates the QAOps exit status to CI and uploads the results JSON as the `qaops-results` artifact when available.

The package does **not** deploy MediaOps Plan or its prerequisites. `PerformPackageInstallation` is `false`; the target must already have the required solution and dependencies installed.

By default, CI and local execution use eight method-level MSTest workers. QAOps runs targeting the same GitHub environment are queued to prevent overlapping test execution.

### QAOps configuration

Configure the QAOps target through GitHub variables and secrets. The selected GitHub environment can override repository/organization values.

| Configuration | Name | Purpose |
| --- | --- | --- |
| Variable | `QAOPS_CONFIGURATION_ID` | Selects the QAOps target. |
| Variable | `QAOPS_TEST_SUITE_ID` | Selects the QAOps test suite. |
| Secret | `QAOPS_TOKEN` | Authorizes the QAOps run. |

`GITHUB_TOKEN` supplies access to the GitHub NuGet feed. The QAOps job uses a Windows runner and .NET 10.

Eligible PR approvals targeting `main` use `MediaOps RT`. Eligible approvals targeting RC branches (`*.*.X`) and non-alpha tag pushes use `dma-rc`. Manual runs use the selected `target_env`, which defaults to `MediaOps RT`. Integration runs are serialized per GitHub environment, so RC approvals and release tags share the same queue.

## Local test setup

### Prerequisites

- Windows with PowerShell 7+.
- A .NET SDK and .NET Framework 4.8 targeting support.
- Visual Studio or Visual Studio Build Tools with MSBuild support for `.slnx` solutions and `vstest.console.exe`. The runner finds these tools on `PATH` or through `vswhere`.
- Access to the repository's NuGet feeds.

Simulation does not require a running DataMiner System. Real-Agent runs require a reachable test System with MediaOps Plan and its prerequisites deployed, plus credentials with permission to create, modify, and remove test data. Use an isolated test environment, not production.

### Connection configuration

Tests load user secrets associated with `DevPack.Tests`, followed by environment variables. Environment variables override user secrets. Connection settings are used for real-Agent runs.

Run these commands from the repository root, replacing the placeholders:

```powershell
dotnet user-secrets set "CRED_PREFIX" "DATAMINER" --project .\DevPack.Tests
dotnet user-secrets set "DATAMINER_HOST" "<HOSTNAME>" --project .\DevPack.Tests
dotnet user-secrets set "DATAMINER_USERNAME" "<USERNAME>" --project .\DevPack.Tests
dotnet user-secrets set "DATAMINER_PASSWORD" "<PASSWORD>" --project .\DevPack.Tests
```

`DATAMINER_DOMAIN` is optional and defaults to an empty string. Without `CRED_PREFIX`, tests use the current Windows credentials. `DATAMINER_HOST` defaults to `localhost`.

A detected QAOps Bridge takes precedence over local settings, forces real-Agent mode, and connects to `localhost` using encrypted QAOps credentials.

## Running tests

Run the following commands from the repository root in PowerShell 7+.

### Simulated unit tests

```powershell
.\DevPack.Tests\Run-Tests.ps1
```

### Real-Agent integration tests

Configure the connection settings first, then run:

```powershell
.\DevPack.Tests\Run-Tests.ps1 -UseRealDma
```

The runner explicitly sets `MEDIAOPS_PLAN_USE_REAL_DMA` for the selected mode. When running outside the runner, for example in Test Explorer, set this user secret or environment variable to `true` to use a real Agent; otherwise simulation is the default. QAOps Bridge detection still takes precedence.

### Runner options

The [test runner](./DevPack.Tests/Run-Tests.ps1) supports:

| Option | Purpose |
| --- | --- |
| `-UseRealDma` | Run against a real DataMiner Agent instead of simulation. |
| `-Executions 5` | Run five iterations. The default is one. `-MaxIterations` is an alias. |
| `-TestFilter "FullyQualifiedName~Workflow"` | Select tests with a native VSTest filter expression. This is not folder or wildcard matching. |
| `-Name "workflow"` | Label the session and append the name to its results folder. |
| `-Hours 8` | Repeat for the specified duration. |
| `-Until (Get-Date).AddDays(1)` | Repeat until the specified local time. |
| `-NoBuild` | Skip restore/build and use existing binaries for the selected configuration. |
| `-Configuration Debug` | Select `Debug` instead of the default `Release`. |
| `-ResultsRoot ".\TestResults"` | Override the results directory; the default is `TestResults` at the repository root. |
| `-SettingsPath ".\DevPack.Tests.runsettings"` | Select a runsettings file; this repository-level file is the default. |
| `-Diagnostics` | Add VSTest runner diagnostics, not DataMiner API-call tracing. |

`-Hours` and `-Until` are mutually exclusive. A time limit repeats tests until the deadline unless an explicit `-Executions` limit is reached first. An active iteration finishes normally when the time limit is reached.

The runner builds once unless `-NoBuild` is supplied. Test failures are recorded without stopping subsequent iterations; the final exit code is nonzero if any iteration fails.

## Test results

[View QAOps results](https://qaops-skyline.on.dataminer.services/app/8f36715b-d50d-4463-9d2d-c38170929ee4/Configurations) for the configuration selected by `QAOPS_CONFIGURATION_ID`. CI also uploads the `qaops-results` JSON artifact when available, with a retention period of 14 days.

Local results are stored at the repository root unless `-ResultsRoot` overrides it:

```text
TestResults\<yyyyMMdd-HHmmss-fff>-<pid>[-<name>]\
```

Completed sessions contain `run.json`, a copy of the runsettings file, `iterations.csv`, `test-results.csv`, `summary.csv`, and `class-summary.csv`. A build also produces `build.log`. Each `iteration-0001`-style subfolder contains `console.log` and `results.trx`; `-Diagnostics` adds `vstest.log`.

Summary durations are summed or averaged test durations, not parallel wall-clock time. Use `run.json` and `iterations.csv` for elapsed session and iteration times.

## Adding or maintaining tests

Use the [QAOps package reference](./QAOpsPackage/README.md) for package contents, CI execution, and cleanup expectations.

- Reuse existing test classes and the shared [test context](./DevPack.Tests/RegressionTests/TestContextManager.cs) and [object creator](./DevPack.Tests/RegressionTests/TestObjectCreator.cs).
- Keep test data isolated because eight method-level workers can run concurrently.
- Dispose the object creator through the existing cleanup patterns so tracked objects are removed. Cleanup is best-effort: failed delete or deprecate operations can leave test data behind.
- Cover behavior in simulation where supported, and use QAOps for validation that requires a real DataMiner Agent.
- When adding or removing tests, verify discovery in the QAOps package.

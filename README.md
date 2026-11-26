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

Adding the NuGet package or creating an API client does not install the DOM schema. Deployment code calls the `InstallDomModules()` extension from `Skyline.DataMiner.Solutions.MediaOps.Plan.API` on a client created by the Plan DevPack. It reconciles only Properties, Relationships, Resource Studio, and Workflow.

When deploying MediaOps Plan, install the People & Organizations schema through its own DevPack first. See [Installing the DOM schema](Documentation/Advanced%20Topics.md#installing-the-dom-schema) for both steps.

## Documentation

| Document | Description |
| -------- | ----------- |
| [Getting Started](Documentation/Getting%20Started.md) | Installation, prerequisites, and the core concepts (resources, jobs, workflows, recurring jobs, node graphs, properties, relationships) |
| [Quick Reference](Documentation/Quick%20Reference.md) | Copyable snippets for every repository: CRUD, job/recurring job lifecycle, node graphs, properties, relationships, querying, and synchronization |
| [Advanced Topics](Documentation/Advanced%20Topics.md) | State machines, job timing rules, configuration state, data references, eligible resources, SRM synchronization, logging, and installation checks |

External resources:

- [DataMiner Docs](https://docs.dataminer.services/) - Official DataMiner documentation

## Regression testing

Tests use C# and MSTest. The same test assembly supports in-memory simulation for unit tests and a real DataMiner System for integration tests. Simulation is the default.

Testing uses the following projects:

| Project | Purpose |
| --- | --- |
| [DevPack.Tests](./DevPack.Tests) | API and regression tests for properties, resources and resource pools, jobs, workflows, recurring jobs, relationships, querying, and orchestration. |
| [DevPack.UnitTesting](./DevPack.UnitTesting) | In-memory DataMiner simulation and test helpers. This is a support library, not a separate test suite. |
| [QAOpsPackage](./QAOpsPackage/README.md) | Builds the QAOps `.dmtest` and provides the installer and Catalog references for the fallback `.dmapp`. |

### CI execution

The [Plan CICD](./.github/workflows/Plan-CICD.yml) workflow uses the following rules. Each stage requires the applicable preceding checks to pass. An RC branch is a branch matching `*.*.X` with no slash in its name.

| Event | Validation and simulated unit tests | Integration environment | Skyline CICD |
| --- | --- | --- | --- |
| PR opened, updated, reopened, or marked ready for review | Yes | None | No |
| Current-head approval of a non-draft same-repository PR to `main` | Yes | `MediaOps RT` | No |
| Current-head approval of a non-draft same-repository PR to an RC branch | Yes | `MediaOps RC` | No |
| Other current-head approval of a non-draft PR, including from a fork | Yes | None | No |
| Non-approval, stale approval, or a review on a draft PR | No | None | No |
| Manual run | Yes | Selected `target_env`; defaults to `MediaOps RT` | No |
| Push or merge to `main` or an RC branch | Yes | None | Build only |
| Push of a prerelease tag containing `-alpha` | Yes | None | Publish to GitHub Packages |
| Push of another prerelease tag | Yes | `MediaOps RC` | Publish to GitHub Packages after integration passes |
| Push of a stable release tag | Yes | `MediaOps RC` | Publish to nuget.org after integration passes |

Plan CICD handles numeric `major.minor.patch` tags and their prereleases. Other tag pushes and pushes to branches outside `main` and the RC branches do not trigger Plan CICD. A stable tag must be contained in the matching `<major>.<minor>.X` branch. Publishing uses `NUGETAPIKEY_GITHUB` for prereleases and `NUGETAPIKEY` for stable releases. The [Internal DataMiner CICD NuGet Solution workflow](./.github/workflows/private-dataminer-cicd-internalnugetsolution.yml) runs separately for branch pushes, PRs, and manual runs and does not use this integration gate.

The **Unit Tests** job builds [MediaOps Plan.UnitTests.slnf](./MediaOps%20Plan.UnitTests.slnf) with MSBuild and runs the complete test assembly with VSTest and `MEDIAOPS_PLAN_USE_REAL_DMA=false`. Test output is harvested into the runner's temporary directory to avoid Windows path-length limits.

### Integration execution

For eligible runs, the repository variable **`USE_QAOPS`** selects the integration path once per run:

- **`true`:** The **QAOps Integration Tests** job builds a `.dmtest` and submits it to the configured QAOps target with `dataminer-qaops test-run-and-wait`. QAOps runs the harvested test assembly with eight method-level MSTest workers. The tests use the credentials provided by the QAOps Bridge.
- **`false`:** [Plan Regression](./.github/workflows/Plan-Regression.yml) builds `QAOpsPackage.csproj` as a `.dmapp`, uploads it as a private volatile artifact, and deploys it to the selected DataMiner System. After deployment succeeds, VSTest runs the full `DevPack.Tests` assembly over SLNet with two method-level workers.

Use the exact lowercase values `true` and `false`. A missing or invalid value fails the **Integration Tests** check without starting either path. Runs that do not require integration are unaffected. A failed or canceled run does not switch paths automatically. Start a new run after changing the variable. A release tag must point to a revision with the updated workflow to use the switch. If QAOps timed out waiting for its initial response, check for work still running on the target before using the fallback.

Both paths use Windows and .NET 10 to build from the approved PR head or the commit that triggered the run. The package version comes from the tag for a tag push and is `0.1.<run number>` otherwise. Approval-triggered runs recheck the live PR head, target, and review before submission or deployment. Both paths share the queue for the selected GitHub environment through the end of testing. The **Integration Tests** check reports the selected path, commit, version, environment, and results and passes only when that path succeeds.

Both paths require DataMiner 10.6.11 or later and use the same [installer](./QAOpsPackage/README.md#execution-and-package-contents). It installs the Catalog packages for Standard Data Model Registration, Categories, and MediaOps Plan, then reconciles the four Plan-owned DOM modules using the DevPack built from the selected commit and creates any missing shared test fixtures. On a persistent fallback target, reconciliation can soft-delete fields or section links removed from the source and does not roll back automatically.

If the `Generic Camera` connector is absent, the affected tests report inconclusive on QAOps and on fallback runs targeting `MediaOps RC`. On other fallback targets, including `MediaOps RT`, a missing connector fails those tests. If the connector is present, its Production version and any required active function must be available.

### Integration variables and secrets

Set `USE_QAOPS` at repository scope. Configure target-specific settings on each GitHub environment used for integration, such as `MediaOps RT` and `MediaOps RC`. A fallback target must be cloud connected for deployment and reachable over SLNet for testing.

| Path | Type | Name | Purpose |
| --- | --- | --- | --- |
| Both | Repository variable | `USE_QAOPS` | Selects QAOps or the fallback. |
| Both | Secret | `SKYLINE__SDK__DATAMINERTOKEN` | Downloads the Catalog prerequisites when building either package. |
| Both | Automatic token | `GITHUB_TOKEN` | Provides GitHub Packages read access for restore and PR read access for approval revalidation. |
| QAOps | Environment variables | `QAOPS_CONFIGURATION_ID`, `QAOPS_TEST_SUITE_ID` | Select the QAOps target and test suite. |
| QAOps | Environment secret | `QAOPS_TOKEN` | Authorizes QAOps submission and result retrieval. |
| Fallback | Environment variable | `DATAMINER_HOST` | SLNet host reachable from the Windows runner. |
| Fallback | Optional environment variable | `DATAMINER_DOMAIN` | Authentication domain for the direct test account; defaults to empty. |
| Fallback | Environment secrets | `DATAMINER_USERNAME`, `DATAMINER_PASSWORD` | Account with permission to run the suite and manage its test data. |
| Fallback | Environment secret | `DATAMINER_TOKEN` | DataMiner System key for volatile upload and deployment. The key must target the same System as `DATAMINER_HOST`. |

The fallback workflow sets `CRED_PREFIX=DATAMINER` and `MEDIAOPS_PLAN_USE_REAL_DMA=true` for the direct test run. It sets `MEDIAOPS_PLAN_LEGACY_RC_RUN=true` only when the selected environment is `MediaOps RC`.

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

[View QAOps results](https://qaops-skyline.on.dataminer.services/app/8f36715b-d50d-4463-9d2d-c38170929ee4/Configurations) for the configuration selected by `QAOPS_CONFIGURATION_ID`. CI uploads `qaops-results` (JSON) for QAOps runs and `fallback-results` (TRX) for fallback runs when results are available. Both artifacts are retained for 14 days.

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

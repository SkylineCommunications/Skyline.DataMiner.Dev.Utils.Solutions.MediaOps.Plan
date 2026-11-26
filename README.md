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

## CI/CD pipeline

### Triggers and job order

[Plan CICD](.github/workflows/Plan-CICD.yml) starts with **Validate NuGets** and **Validate Tag Matches Branch**. Afterwards, **Unit Tests** runs in simulation on Windows against `MediaOps Plan.UnitTests.slnf`.

| Trigger | Live regression | Skyline CICD and NuGet publishing |
| --- | --- | --- |
| PR targeting any branch | None | None |
| Current-head PR approval targeting `main` | `MediaOps RT` | None |
| Current-head PR approval targeting a `*.*.X` branch | `MediaOps RC` | None |
| Other current-head approvals | None | None |
| Manual run | The `target_env` input | None |
| Push to `main` or `*.*.X`, including a merge | None | Skyline CICD build only |
| Push of a prerelease tag containing `-alpha` | None | Skyline CICD, followed by GitHub Packages publish |
| Push of another prerelease tag | `MediaOps RC` | Skyline CICD builds and publishes the release to the GitHub Packages after regression passes |
| Push of a stable release tag | `MediaOps RC` | Skyline CICD builds and publishes the release to the nuget.org after regression passes |

The [Internal DataMiner CICD NuGet Solution workflow](.github/workflows/private-dataminer-cicd-internalnugetsolution.yml) runs separately for branch pushes, PRs, and manual runs.

## Regression testing

Regression tests use C# and MSTest. The same test suite runs in simulation for unit tests and against a real DataMiner System with MediaOps Plan deployed for integration tests.

The repository contains two test projects:

| Project | Purpose |
| --- | --- |
| [DevPack.Tests](./DevPack.Tests) | API and integration tests for properties, resources and resource pools, jobs, workflows, recurring jobs, relationships, querying, and orchestration. |
| [DevPack.UnitTesting](./DevPack.UnitTesting) | In-memory DataMiner simulation and test helpers. |

## Test results

[View QAOps results](https://qaops-skyline.on.dataminer.services/app/8f36715b-d50d-4463-9d2d-c38170929ee4/Configurations) for the configuration selected by `QAOPS_CONFIGURATION_ID`, or check the `qaops-results` JSON artifact when available.

For legacy CI runs, inspect the **Legacy Integration Tests** job logs and the `legacy-results` TRX artifact when available. The required **Integration Tests** summary identifies the selected backend, target, source SHA, package version, and backend results.

`ElementResourceImport`, `FunctionResourceWithElementLinkImport`, and `HappyPathCrud_ElementLink` report **Inconclusive** when the `Generic Camera` connector is absent on QAOps or in a legacy `MediaOps RC` run. A missing connector fails these tests on legacy `MediaOps RT` and other legacy targets. If the connector is present, the tests run normally.

### CI execution

On runs that [require live regression](#triggers-and-job-order), the repository variable **`USE_QAOPS`** selects the integration backend once per run:

- `true`: **QAOps Integration Tests** (`qaops_integration_tests`).
- `false`: **Legacy Integration Tests** (`legacy_integration_tests`), using [Plan Regression](./.github/workflows/Plan-Regression.yml).

Only the exact values `true` and `false` are accepted. A missing or invalid value fails the integration gate without starting either backend. Unit/build-only runs are unaffected. There is no automatic fallback if the selected backend fails. Both backends use the automatic `GITHUB_TOKEN` to read GitHub Packages and recheck PR approval.

### QAOps configuration

Configure the **QAOps Integration Tests** job as follows:

| Configuration | Name | Purpose |
| --- | --- | --- |
| Environment variable | `QAOPS_CONFIGURATION_ID` | Selects the QAOps target. |
| Environment variable | `QAOPS_TEST_SUITE_ID` | Selects the QAOps test suite. |
| Environment secret | `QAOPS_TOKEN` | Authorizes the QAOps run. |
| Repository/organization secret | `SKYLINE__SDK__DATAMINERTOKEN` | Downloads Catalog dependencies while building the package. |

The QAOps job uses a Windows runner and .NET 10. Tests on the target get their DataMiner credentials from the [QAOps Bridge](./QAOpsPackage/README.md#what-this-package-tests).

### Legacy configuration

Configure the deploy and test jobs in **Plan Regression** as follows:

| Configuration | Name | Purpose |
| --- | --- | --- |
| Environment secret | `DATAMINER_TOKEN` | Deploy job: private volatile upload and deployment. The key must target the same System as `DATAMINER_HOST`. |
| Environment secrets | `DATAMINER_USERNAME`, `DATAMINER_PASSWORD` | Test job: DataMiner test authentication. |
| Environment variable | `DATAMINER_HOST` | Test job: SLNet host reachable from the Windows runner. |
| Environment variable (optional) | `DATAMINER_DOMAIN` | Test job: authentication domain. Defaults to empty. |
| Repository/organization secret | `SKYLINE__SDK__DATAMINERTOKEN` | Deploy job: Catalog dependency downloads during the package build. |

The test job sets `CRED_PREFIX=DATAMINER` and `MEDIAOPS_PLAN_USE_REAL_DMA=true`. It sets `MEDIAOPS_PLAN_LEGACY_RC_RUN=true` only for `MediaOps RC`.

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

`-Hours` and `-Until` are mutually exclusive. An active iteration finishes normally when the time limit is reached.

Local results are stored at the repository root unless `-ResultsRoot` overrides it:

```text
TestResults\<yyyyMMdd-HHmmss-fff>-<pid>[-<name>]\
```

## Adding or maintaining tests

Use the [QAOps package reference](./QAOpsPackage/README.md) for package contents, CI execution, and cleanup expectations.

- Reuse existing test classes and the shared [test context](./DevPack.Tests/RegressionTests/TestContextManager.cs) and [object creator](./DevPack.Tests/RegressionTests/TestObjectCreator.cs).
- Keep test data isolated because eight method-level workers can run concurrently.
- Dispose the object creator through the existing cleanup patterns so tracked objects are removed. Cleanup is best-effort: failed delete or deprecate operations can leave test data behind.
- Cover behavior in simulation where supported, and use QAOps for validation that requires a real DataMiner Agent.
- When adding or removing tests, verify discovery in the QAOps package.

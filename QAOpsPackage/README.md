# QAOpsPackage

This project builds a **DataMiner QAOps Test Package** (`.dmtest`) for `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan`.

## What this package tests

When this package runs in QAOps, it executes the complete harvested `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.Tests` MSTest assembly. CI uses the same suite for simulated **Unit Tests** and real-Agent **Integration Tests**.

The integration tests cover the MediaOps Plan API against a real DataMiner System, including:

- **Properties**: property definitions and values.
- **Resource Studio / RST**: resources, resource pools, capabilities, capacities, configurations, resource properties, and filtering behavior.
- **Workflow**: jobs, workflows, recurring jobs, scheduling properties, and job settings.

The QAOps credentials branch in `DevPack.Tests/RegressionTests/Config.cs` enables real-Agent mode and connects to `localhost` with the encrypted QAOps credentials.

## Execution and package contents

`qaops.config.xml` sets `PerformPackageInstallation` to `false`. QAOps extracts the test content and runs the pipeline against the configured target, where MediaOps Plan and its prerequisites are already installed.

This package currently contains:

- the harvested build output of `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.Tests`, including the library built from the checked-out source
- the QAOps pipeline scripts in `TestPackagePipeline`

## CI execution

[`Plan-CICD.yml`](../.github/workflows/Plan-CICD.yml) runs validation and the simulated suite first. Manual runs, eligible current-head approvals of non-draft same-repository PRs targeting `main`, and tag pushes without `-alpha` then run the full suite through `dataminer-qaops test-run-and-wait`.

Branch pushes and tags containing `-alpha` publish internally after simulated tests pass. Other prerelease tags require successful QAOps tests before internal publication. Stable tags also require matching release-branch ancestry and publish to nuget.org. Publication runs only on pushes.

QAOps uses repository-level `QAOPS_CONFIGURATION_ID` and `QAOPS_TEST_SUITE_ID` variables and the `QAOPS_TOKEN` secret by default. Tags whose commits are outside `main` use the `dma-rc` environment's settings unless `DMA_RC_ENABLED=false`. Integration runs are serialized per target.

The workflow restores the test project before building `QAOpsPackage.csproj` with a tag-derived or run-specific version, submits that exact `.dmtest`, and uploads the QAOps results JSON.

## What gets cleaned up after the run

The integration tests create and track test data, and the test cleanup code attempts to remove the objects it created, including:

- jobs and workflows
- properties and property setting collections
- resources and resource pools
- capabilities, capacities, configurations, and resource properties
- categories
- elements and services
- core resources and core resource pools

However, this cleanup is **best-effort**, not a hard guarantee. The cleanup code intentionally ignores cleanup failures so the package can continue disposing the remaining tracked objects. Because of that, this README should **not** claim that absolutely everything is always removed.

So the correct expectation is:

- **Usually cleaned up:** the temporary test objects created by the integration tests.
- **Not guaranteed to be fully clean in every failure scenario:** if one of the delete/deprecate operations fails, some test data can remain in the QAOps test environment until that environment is discarded.

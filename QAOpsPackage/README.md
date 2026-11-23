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

The QAOps target must run DataMiner 10.6.11 or later. For a local build, store an organization key that can download the Catalog prerequisites in the project's `skyline:sdk:dataminertoken` user secret. CI reads the key from the `SKYLINE__SDK__DATAMINERTOKEN` GitHub secret.

`TestPackageContent/qaops.config.xml` sets `PerformPackageInstallation` to `true`. QAOps runs [`QAOpsPackage.cs`](QAOpsPackage.cs) before starting the test pipeline. `InstallDefaultContent()` installs the embedded Catalog packages in filename order: Standard Data Model Registration, Categories, then MediaOps Plan. These packages do not need to be preinstalled on the target.

The installer then calls `InstallDomModules()` on the Plan DevPack built from this checkout to reconcile Properties, Relationships, Resource Studio, and Workflow. This call does not install or reconcile the People & Organizations schema. The installer creates the `Job Types` and `Resource Pools` category scopes and the reserved `Job` relationship object type only if they are missing. The reserved object type is seeded directly through DOM because the public repository does not allow creating reserved names.

Test initialization and reconnects do not run this setup.

This package currently contains:

- the Standard Data Model Registration, Categories, and MediaOps Plan packages declared in [`PackageContent/CatalogReferences.xml`](PackageContent/CatalogReferences.xml)
- `Scripts/Install.xml` and its dependencies in `Scripts/InstallDependencies`, including the Plan DevPack built from this checkout
- the harvested build output of `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.Tests`, including the library built from the checked-out source
- the QAOps pipeline scripts in `TestPackagePipeline`

Visual Studio references the Plan DevPack project directly, and the solution builds the test project before the package. The install script compiles before it can install the embedded prerequisites. Because the SDK normally omits solution-library NuGet DLLs from `Scripts/InstallDependencies`, the package build stages the required DLLs from the active DevPack build and bundles them as direct assembly references.

## CI execution

[`Plan-CICD.yml`](../.github/workflows/Plan-CICD.yml) runs validation, builds `MediaOps Plan.UnitTests.slnf` with MSBuild, and runs the complete simulated suite with VSTest. This unit build harvests test output into the runner's temporary directory to avoid Windows path-length limits.

Manual runs, eligible current-head approvals of non-draft same-repository PRs targeting `main` or RC branches matching `*.*.X`, and pushes of release tags that do not contain `-alpha` then run the full suite through `dataminer-qaops test-run-and-wait`.

Branch pushes run Skyline CICD after validation and simulated tests pass. Tags containing `-alpha` skip the QAOps publishing gate; all other release tags require QAOps to pass. Prerelease tags publish to GitHub Packages. Stable tags also require matching release-branch ancestry and publish to nuget.org. NuGet publication runs only on tag pushes.

Tag-triggered QAOps runs and eligible approvals of PRs targeting RC branches (`*.*.X`) use the `MediaOps RC` environment after the applicable validation and simulated tests pass. Eligible approvals of PRs targeting `main` use `MediaOps RT`. Manual runs select an environment through `target_env`, including `MediaOps RC`. The selected environment can override the repository-level `QAOPS_CONFIGURATION_ID` and `QAOPS_TEST_SUITE_ID` variables and `QAOPS_TOKEN` secret. Integration runs are serialized per target, so RC approvals and tag runs share the same queue.

The workflow restores the test project before building `QAOpsPackage.csproj` with a tag-derived or run-specific version, revalidates the current PR head and target for approval-triggered runs, submits that exact `.dmtest`, and uploads the QAOps results JSON.

## What gets cleaned up after the run

The integration tests create and track test data, and the test cleanup code attempts to remove the objects it created, including:

- jobs and workflows
- properties and property setting collections
- resources and resource pools
- capabilities, capacities, configurations, and resource properties
- categories
- elements and services
- core resources and core resource pools

Cleanup is **best-effort**. If a delete or deprecate operation fails, cleanup continues with the remaining tracked objects. Some temporary data may remain until the QAOps test environment is discarded.

Test cleanup does not remove the installed Catalog packages, reconciled DOM schemas, or shared test fixtures.

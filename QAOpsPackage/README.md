# QAOpsPackage

This project builds a **DataMiner QAOps Test Package** (`.dmtest`) for `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan`. The fallback workflow builds the same project with `-p:DataMinerType=Package` to deploy the installer and Catalog prerequisites as a `.dmapp` before running the tests directly.

## What this package tests

When this package runs in QAOps, it executes the complete harvested `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.Tests` MSTest assembly. CI uses the same suite for simulated **Unit Tests** and real-Agent **Integration Tests**.

The integration tests cover the MediaOps Plan API against a real DataMiner System, including:

- **Properties**: property definitions and values.
- **Resource Studio / RST**: resources, resource pools, capabilities, capacities, configurations, resource properties, and filtering behavior.
- **Workflow**: jobs, workflows, recurring jobs, scheduling properties, and job settings.

The QAOps credentials branch in `DevPack.Tests/RegressionTests/Config.cs` enables real-Agent mode and connects to `localhost` with the encrypted QAOps credentials.

## Execution and package contents

The QAOps target must run DataMiner 10.6.11 or later. For a local build, store an organization key that can download the Catalog prerequisites in the project's `skyline:sdk:dataminertoken` user secret. CI reads the key from the `SKYLINE__SDK__DATAMINERTOKEN` GitHub secret.

`TestPackageContent/qaops.config.xml` sets `PerformPackageInstallation` to `true`. QAOps runs [`QAOpsPackage.cs`](QAOpsPackage.cs) before starting the test pipeline. The current setup order is:

1. `InstallDefaultContent()` installs the embedded Catalog packages in filename order: Standard Data Model Registration, Categories, then the **full MediaOps Plan solution**.

   The solution release selected by `Range 2` runs its own installer and supplies the initial Plan and People & Organizations DOM schemas. Release `2.0.0`, for example, imports the shipped JSON schemas and seeds the same shared fixtures. The range does not pin that release or its installer behavior.

2. The `InstallDomModules()` extension from the Plan DevPack built from this checkout reconciles Properties, Relationships, Resource Studio, and Workflow on top of the deployed solution. It does not reconcile the People & Organizations schema.
3. The installer creates the `Job Types` and `Resource Pools` category scopes and the reserved `Job` relationship object type only if they are missing. It creates the reserved object type directly through DOM because the public API does not allow creating reserved names.

**Intentional overlap for now:** QAOps reconciles the Plan schemas after the full solution has already installed them. The fixture checks are fallbacks and skip entries that the solution already seeded. Keep this redundancy until provisioning ownership is clarified. These tests exercise the source-built DevPack on top of a full solution deployment, not a fresh DevPack-only installation.

Use a target and an embedded solution release compatible with the `2.0.X` schema. The DevPack under test does not include Flow Rules. Its installer can soft-delete fields from a newer schema, including one supplied by the embedded solution.

Test initialization and reconnects do not run this setup. The shared scopes and relationship object type remain on the target after a run. Real-Agent quarantine tests still require the `MediaOps_SRM_QuarantineHandling` callback to be configured on the target. Check what the selected solution release deploys and what the environment provides separately.

This package currently contains:

- the Standard Data Model Registration, Categories, and full MediaOps Plan solution packages declared in [`PackageContent/CatalogReferences.xml`](PackageContent/CatalogReferences.xml)
- `Scripts/Install.xml` and its dependencies in `Scripts/InstallDependencies`, including the Plan DevPack built from this checkout
- the harvested build output of `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.Tests`, including the library built from the checked-out source
- the QAOps pipeline scripts in `TestPackagePipeline`

Visual Studio references the Plan DevPack project directly, and the solution builds the test project before the package. The install script compiles before it can install the embedded prerequisites. Because the SDK normally omits solution-library NuGet DLLs from `Scripts/InstallDependencies`, the package build stages the required DLLs from the active DevPack build and bundles them as direct assembly references.

## CI execution

[`Plan-CICD.yml`](../.github/workflows/Plan-CICD.yml) runs validation, builds `MediaOps Plan.UnitTests.slnf` with MSBuild, and runs the complete simulated suite with VSTest. This unit build harvests test output into the runner's temporary directory to avoid Windows path-length limits.

When `USE_QAOPS=true` and [integration is required](../README.md#ci-execution), the workflow restores the test project before building `QAOpsPackage.csproj` with a tag-derived or run-specific version. It revalidates the current PR head and target for approval-triggered runs, submits that exact `.dmtest` with `dataminer-qaops test-run-and-wait`, and uploads the QAOps results JSON. See the [integration variables and secrets](../README.md#integration-variables-and-secrets) for setup of both paths.

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

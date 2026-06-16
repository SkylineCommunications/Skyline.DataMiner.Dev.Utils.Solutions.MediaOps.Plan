# QAOpsPackage

This project builds a **DataMiner QAOps Test Package** (`.dmtest`) for `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan`.

## What this package tests

When this package runs in QAOps, it executes the harvested `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.Tests` MSTest assembly and only runs tests with:

```txt
TestCategory=IntegrationTest
```

The integration tests cover the MediaOps Plan API against a real DataMiner system, including:

- **Properties**: property definitions and values.
- **Resource Studio / RST**: resources, resource pools, capabilities, capacities, configurations, resource properties, and filtering behavior.
- **Workflow**: jobs, workflows, recurring jobs, scheduling properties, and job settings.

In other words, this package validates the **integration behavior of the MediaOps Plan .NET API** against a live QAOps-provisioned DataMiner environment. It is not a unit-test package.

## What gets installed when the package runs

`qaops.config.xml` sets `PerformPackageInstallation` to `true`, so QAOps installs the **test package itself** before executing the pipeline.

This package currently contains:

- the harvested build output of `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.Tests`
- the QAOps pipeline scripts in `TestPackagePipeline`

This package currently does **not** add extra repository-committed test assets under `TestPackageContent\Tests`, does **not** add extra static dependencies under `TestPackageContent\Dependencies`, and does **not** install XML automation tests from `xmlautomationtests.generated`.

The setup and finalize pipeline steps are currently placeholders, so they do not perform extra provisioning or extra teardown beyond the integration test execution itself.

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

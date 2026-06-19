# QAOpsPackage

> [!IMPORTANT]
> This package is intended to run in **DataMiner QAOps**. It is a **test package**, not a production package.

## About

When this package is executed, QAOps installs the package on the test system and runs automated **integration tests** for the MediaOps Plan solution.

These tests validate high-level MediaOps Plan behavior such as:

- property management
- resource and resource pool behavior
- capabilities, capacities, and configurations
- workflow and job-related behavior

In short, the package checks whether the MediaOps Plan solution behaves correctly on a real DataMiner environment.

## Which components get installed

Running this package will install the **test package itself** on the QAOps test environment so the automated validation can run.

It is meant for temporary QAOps test environments and not for functional deployment as part of a production setup.

## Which items remain on the system after the package is run

During the test run, temporary test data can be created in the QAOps environment. The package then attempts to clean up the objects it created.

Cleanup is **best-effort**:

- Under normal circumstances, the temporary test data is removed again.
- If part of the cleanup fails, some test data can remain in the QAOps test environment.

This package is designed to leave the environment clean after execution, but it does **not** guarantee complete cleanup in every failure scenario.

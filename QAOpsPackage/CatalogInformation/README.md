# MediaOps Plan API Tests

> [!IMPORTANT]
> This package is intended to run in **DataMiner QAOps**. It is a **test package**, not a production package.

## About

QAOps runs automated integration tests for the MediaOps Plan API on a real DataMiner System, so you can verify behavior that an in-memory simulation cannot cover.

## Key Features

- Verify property definitions and values.
- Test resource and resource pool behavior.
- Validate capabilities, capacities, and configurations.
- Test workflow and job behavior.

## Prerequisites

- DataMiner 10.6.11 or later.
- An isolated DataMiner QAOps test environment.

## Which components get installed

Before the tests start, QAOps installs the Catalog packages for Standard Data Model Registration, Categories, and MediaOps Plan in that order. The test package then uses its bundled DevPack to reconcile the four Plan-owned DOM modules. It creates the `Job Types` and `Resource Pools` category scopes and the reserved `Job` relationship object type only if they are missing.

## Which items remain in the system after the package is run

Test cleanup does not remove the installed Catalog packages, reconciled DOM schemas, or shared test fixtures. Existing shared fixtures are preserved.

The tests attempt to remove the temporary objects they create and track. Cleanup is best-effort: some temporary data may remain if a cleanup operation fails.

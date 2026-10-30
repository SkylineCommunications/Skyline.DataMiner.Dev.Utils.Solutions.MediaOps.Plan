# Testing review gate

Read this for every review. **Every new or changed nontrivial/business behavior must have meaningful automated coverage**, whether unit, simulation-backed API, integration, or a relevant external consumer test. Apply this to the semantic PR scope, not unrelated historical coverage debt. A test already in the repository is sufficient only if it genuinely exercises and asserts the changed rule.

## Build the evidence chain

For each changed behavior, identify:

| Evidence | What to establish |
| --- | --- |
| Rule | The decision, transformation, validation, state transition, persistence effect, query result, or consumer-visible outcome being changed |
| Reachable cases | Normal use, meaningful branches, and supported failure/boundary conditions affected by the change |
| Test | Exact test file and method/data case exercising that behavior, directly or through a real caller; for external tests, the relevant release's producing commit |
| Assertion | The observable result that would become wrong if the rule regressed |
| Enablement | The test is compiled, discoverable, not disabled, and included in the relevant runner/filter/package |
| Execution | Results for the applicable revision/environment when available; otherwise explicitly unverified |

Include nontrivial error handling, partial-success processing, filtering, mapping, serialization, duplicate/remap behavior, timing, cleanup, and concurrency rules when they are part of the change. Do not reduce business coverage to line counts or one successful happy-path call. Equally, do not demand a separate test for every trivial accessor or delegation already covered through its caller.

Choose the cheapest layer that proves the contract. Pure tests establish rules/transformations. Simulation-backed tests can execute the actual Plan API and dependency code against an in-memory connection. Real-DMA integration establishes platform behavior not implemented by that simulation. External application tests establish affected UI/callback/deployment boundaries. Do not demand every layer for every rule; do require the relevant boundary when lower-layer tests cannot establish it.

## Every bug fix must close its escape gap

A bug fix needs a causal account of **why the existing suite did not detect this particular bug**, not just a new test with a similar name.

1. Reconstruct the realistic triggering scenario and expected outcome from the report/PR and production flow.
2. Find the closest existing coverage, including shared tests and the owning dependency's tests when accessible. Use [GitHub source retrieval](github-source.md) to read external tests at the relevant producing commit. Read fixtures, mocks, assertions, discovery and CI filters.
3. Identify the escape gap: missing case or state transition, missing/weak assertion, a mock that bypassed the defective logic, unrealistic data/setup, an untested integration boundary, or a sufficient test that was not discovered/executed.
4. Verify the PR closes that exact gap with a regression assertion and, where needed, corrected test/CI wiring. An unrelated test or a coverage percentage increase does not count. Existing adequate tests that did not run need execution repair, not duplicate tests.
5. Establish that the regression check fails for the original defect and passes with the fix. The failure must be the intended behavior/assertion, not a build error, missing deployment, missing credentials, or unrelated fixture failure. If the relevant tests cannot be executed here, distinguish source-based evidence from demonstrated fail-before/pass-after results and mark execution pending.

The review should identify the production cause, escape gap, test/wiring change closing it, and verification status. Do not accept an unexplained production-only fix. A blocked test environment does not waive test code or discovery requirements, and it is not evidence that the test itself is missing or broken.

## What does not prove coverage

- A test never reaches the changed branch or only asserts that a call returned/non-null.
- A mock returns the expected business outcome instead of executing the behavior under test.
- Expected results use the same production algorithm whose correctness is in question, without independent contract assertions.
- A regression test would also pass on the faulty implementation.
- The relevant attribute is commented out, the test is ignored/skipped, or a runner/filter/package omits it.
- A simulation supplies a platform behavior that the actual Agent does not guarantee, or explicitly bypasses the broken boundary.
- Retries, broad exception catches, or larger timeouts mask a real failure without addressing its cause.
- A green workflow does not run the relevant suite, or the results belong to another revision/deployment.

Before reporting missing coverage, search existing tests by behavior, caller, and assertion, not just production class name. When external tests cannot be inspected, identify the verification gap instead of asserting they do not exist.

## Local suite and search routes

The executable test project is [DevPack.Tests](../../../../DevPack.Tests/Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.Tests.csproj): `MSTest.Sdk`, `net48`, VSTest. [DevPack.UnitTesting](../../../../DevPack.UnitTesting/) is supporting simulation code, not another test assembly. Do not import the Plan solution's unit-test filter or Playwright runner into this repository.

| Test area | Search for |
| --- | --- |
| [RST](../../../../DevPack.Tests/RST/) | Resource/pool lifecycle, assignments, capabilities/capacities/configurations, eligibility, synchronization and category membership |
| [Workflow](../../../../DevPack.Tests/Workflow/) | Jobs, recurrence, workflows, node graphs, connections, timing, state/configuration rules, filtering and querying |
| [Generic](../../../../DevPack.Tests/Generic/) | References/resolution/conversion, settings, bulk-result and exception behavior |
| [Orchestration](../../../../DevPack.Tests/Orchestration/) | Orchestration reference validation and shared context |
| [Properties](../../../../DevPack.Tests/Properties/), [Relationships](../../../../DevPack.Tests/Relationships/) | Definitions, settings/values, attachments, links, queries and deletion contracts |
| [Storage](../../../../DevPack.Tests/Storage/), [Querying](../../../../DevPack.Tests/Querying/) | Caches, batching, filter/query semantics and persistence-facing behavior |
| [Automation](../../../../DevPack.Tests/Automation/), [Simulation](../../../../DevPack.Tests/Simulation/) | Script-facing behavior and simulation/store isolation; follow the actual test body |

Useful focused entry points include `Workflow/Jobs/LiveOrchestrationSimulationTests.cs`, `RunningJobNodeEditSimulationTests.cs`, `PostRollNodeGraphFreezeTests.cs`, `CoreReservationNodeIdTests.cs`, `Generic/DataReferences/`, and `Properties/Values/FilePropertySimulationTests.cs`. These are search routes, not a complete or permanently current test inventory.

## Simulation versus a real DataMiner Agent

Read [Config.cs](../../../../DevPack.Tests/RegressionTests/Config.cs), [IntegrationTestContext.cs](../../../../DevPack.Tests/RegressionTests/IntegrationTestContext.cs) and [TestContextManager.cs](../../../../DevPack.Tests/RegressionTests/TestContextManager.cs) before interpreting a run:

- The shared regression context normally uses `MediaOpsPlanSimulation.Create()` and an in-memory connection. `MEDIAOPS_PLAN_USE_REAL_DMA=true` or `1` enables a real connection outside QAOps.
- A nonempty encrypted `BridgeId` is checked first and enables QAOps credentials/real-DMA mode regardless of `MEDIAOPS_PLAN_USE_REAL_DMA=false`. A false flag alone is not a safety guarantee on a QAOps bridge.
- Some tests create their own simulation rather than use the shared context. A test category named `IntegrationTest` does not by itself establish which mode executed.
- Some platform-dependent tests skip when the simulation cannot implement the required behavior. A green simulated run is not evidence that those cases ran against a real Agent.
- The simulation has no real SRM engine or physical device routing. Inspect explicit state manipulation and simulated script/scheduler behavior before claiming platform/end-to-end proof.

Reuse [TestObjectCreator](../../../../DevPack.Tests/RegressionTests/TestObjectCreator.cs), context helpers, settings snapshots and cleanup. Isolate mutable data with unique names/IDs; restore shared/global settings in `finally` and inspect cleanup on assertion failure. The suite enables method-level parallelism in [MSTestSettings.cs](../../../../DevPack.Tests/MSTestSettings.cs). Honor justified `DoNotParallelize` constraints; process-local locks do not isolate another process or Agent user.

For test-support changes, follow other users of [connection handling](../../../../DevPack.UnitTesting/Connection/), [simulation](../../../../DevPack.UnitTesting/Simulation/), stores and query translation. Do not make the simulation return success merely to make production tests green. Assert the real observable contract and keep platform limitations explicit.

## Discovery, CI and execution evidence

- [Plan-CICD.yml](../../../../.github/workflows/Plan-CICD.yml) builds/runs `DevPack.Tests` on Windows with .NET 10 tooling, `CI=true` and `MEDIAOPS_PLAN_USE_REAL_DMA=false` in its Unit Tests job. The projects still target .NET Framework 4.8. [Runsettings](../../../../DevPack.Tests.runsettings) supplies runner configuration.
- The QAOps Integration Tests job is conditional: manual dispatch, eligible non-alpha tags, or an approved current-head, non-draft, same-repository PR targeting `main`. It rechecks approval/head agreement before submission. A normal green PR build does not prove QAOps ran.
- QAOps access is serialized by the workflow's selected-environment concurrency group. Test fixtures must still tolerate their configured within-run parallelism and shared state.
- The test project's post-build target harvests its output into `QAOpsPackage/TestPackageContent/TestHarvesting/postbuild.generated/`. [TestDiscovery.ps1](../../../../QAOpsPackage/TestPackageContent/TestHarvesting/TestDiscovery.ps1) copies that output into `tests.generated`; despite its name, it does not demonstrate nonempty VSTest discovery.
- [The QAOps execution script](../../../../QAOpsPackage/TestPackageContent/TestPackagePipeline/2.TestPackageExecution.ps1) executes the harvested test DLL through `Invoke-DotNetTestAndPublishResults`. Check fresh binaries/dependencies, actual discovered cases and `mediaops-plan-integration-tests.trx`, not just the presence of the DLL or a pipeline summary.
- The QAOps setup script is a placeholder, not a full Plan/Live application deployment. Verify the selected environment's installed solution/schema/scripts when a test relies on them.
- External Plan solution integration/Playwright tests and dependency suites are available through the [ownership map](solution-map.md), when the changed boundary requires them. They are not local test projects or automatically part of this CI run.

### Real-environment execution boundary

During hosted review, do not deploy, invoke QAOps submissions, run `DevPack.Injector`, or execute tests against a real DataMiner environment. Inspect source/discovery/configuration and supplied or existing CI results. Simulation-only checks are permissible only with supported Windows/.NET Framework tooling, package restore access, no QAOps bridge/live credentials, and explicitly selected simulated mode. If those conditions cannot be established, leave execution unverified rather than attempting a connection.

Never claim an unexecuted test passed, that source inspection demonstrated fail-before/pass-after, or that unavailable infrastructure is a production bug. Keep failure, skip, no discovery and no execution distinct while enforcing meaningful coverage and enablement.

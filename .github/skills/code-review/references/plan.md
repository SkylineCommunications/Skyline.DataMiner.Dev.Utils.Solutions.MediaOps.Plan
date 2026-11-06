# Plan domain map

Load for the Plan API, context adapters, Resource Studio, workflows, scheduling, configuration/references, properties/relationships, querying and persistence. Paths below are local unless a repository is named. Follow only the sections reached by the changed behavior.

## API entry points and adapters

- [IMediaOpsPlanApi](../../../../DevPack/API/IMediaOpsPlanApi.cs) and [MediaOpsPlanApi](../../../../DevPack/API/MediaOpsPlanApi.cs) expose repositories, global settings, logging and installation checks.
- [IConnectionExtensions](../../../../DevPack/API/Extensions/IConnectionExtensions.cs) obtains the shared API from a connection. Trace connection/API ownership, lifetime and caching when affected.
- Automation [EngineExtensions](../../../../DevPack.Automation/Extensions/EngineExtensions.cs) and [IEngineExtensions](../../../../DevPack.Automation/Extensions/IEngineExtensions.cs), protocol [SLProtocolExtensions](../../../../DevPack.Protocol/Extensions/SLProtocolExtensions.cs), and GQI [GQIDMSExtensions](../../../../DevPack.GQI/Extensions/GQIDMSExtensions.cs) adapt their runtime context to the shared API.

These are reusable libraries, not standalone automation scripts, QActions or GQI data sources. Inspect the actual runtime caller when an adapter contract changes. Do not transfer a platform API from one execution context into another or infer that UI validation protects direct callers.

For public contract changes, check source/binary compatibility, overload/default behavior, interface implementers, exception/result semantics and serialized data where affected. Follow relevant external consumers through [solution-map](solution-map.md); do not demand unrelated modernization or support for every hypothetical caller.

## Resource Studio

### Search route

| Layer | Entry points |
| --- | --- |
| Objects/repositories/handlers | `DevPack/API/{Objects,Repositories,Handlers}/ResourceStudio/` |
| Validation | Domain handlers, `API/Handlers/ApiObjectValidator.cs`, `API/Validators/SlcResourceStudio*UsageValidator.cs`, setting validators |
| Persistence/core | `DevPack/Storage/DOM/` and `Storage/Core/`, reached through the resource/pool/parameter handlers |
| Tests | [DevPack.Tests/RST](../../../../DevPack.Tests/RST/), domain query/filter tests and relevant simulation tests |
| External application | Plan solution `RST*`, `ResourceManagement_*`, `ResourceStudio_*`, shared application handlers and read models |

### Contracts to investigate when affected

- Resources/pools have lifecycle and usage rules. Check workflow/job references and supported delete/deprecate options before alleging that an operation should succeed.
- A resource's availability is shared across pools containing it. Concurrency, quantitative capacity and qualitative capability constraints serve different purposes.
- Time-dependent capabilities constrain overlapping bookings. Validate the API's actual overlap/time-window behavior rather than infer a contract from a dropdown list.
- Configurations supply runtime values; properties supply metadata. Neither automatically establishes an eligibility requirement.
- Linked pools can create related nodes manually or automatically. Inspect the selection policy and no-eligible-resource behavior before assuming every linked pool must resolve immediately.
- Externally managed resources/pools can belong to PNO. For ownership/edit restrictions, load [PNO](people-and-organizations.md).
- DOM-to-core synchronization can distinguish differences, blockers and applicable actions. Trace report generation separately from mutation, including partial-success results for bulk operations.

## Workflows and node graphs

Start in `DevPack/API/Objects/Workflow/`, corresponding `Repositories/Workflow/` and `Handlers/Workflow/`, [NodeGraphValidator](../../../../DevPack/API/Validators/NodeGraphValidator.cs) and [NodeSwapValidator](../../../../DevPack/API/Validators/NodeSwapValidator.cs). Tests live in `DevPack.Tests/Workflow/{Nodes,Connections,Workflows}/`.

- Workflows are reusable untimed templates; jobs add timing, assignment, reservations and execution lifecycle. Trace creation from templates instead of treating them as interchangeable persisted objects.
- Directed connections, parent/child links and graph groups have different semantics. Follow the relevant graph operation and invariants.
- Duplicate/create-from-template/swap/remove operations must preserve or deliberately remap node/connection/group identities, settings, references and orchestration data. Read cloning/remapping helpers and consumers.
- Trace graph validation, DOM conversion, core reservation updates and Live effects together when a mutation reaches them. A correct in-memory graph alone does not establish persisted or executed correctness.
- Application edit locks/guards live partly in the Plan solution. Do not claim a direct API invariant is enforced solely by an IAS presenter, or demand a duplicate local guard when the owning validator already enforces it.

## Scheduling

### Search route

| Behavior | Entry points |
| --- | --- |
| Public job operations | [JobsRepository](../../../../DevPack/API/Repositories/Workflow/Jobs/JobsRepository.cs), job interfaces and objects |
| Persistence and core reservations | [Job handlers](../../../../DevPack/API/Handlers/Workflow/Jobs/): `DomJobHandler`, `CoreJobHandler`, timing/window helpers and trace-data translation |
| Live synchronization | `LiveJobConfigHandler`, orchestration/reference validators; load [Live](live.md) |
| Validation | `JobValidator`, `JobReferenceValidator`, `JobReferenceResolution`, `JobNodeResourceRequirementsValidator`, `JobNodeGraphPostRollFreezeValidator` |
| Recurrence | Recurring-job repositories/objects/handlers and occurrence generation; Plan solution generator script when reached |
| Tests | `DevPack.Tests/Workflow/{Jobs,RecurringJobs,JobSettings,SchedulingProperties}/` |
| External callbacks | Plan solution `MediaOps_SRM_Scheduling Actions/`, `MediaOps_SRM_QuarantineHandling/`, `SCH-AS-RecurringJobs_Generator/` |

### Contracts to investigate when affected

- Plan job lifecycle, core reservation state and orchestration outcome are distinct. An orchestration failure need not prevent lifecycle completion; inspect typed status/error handling and tests.
- Trace `Start`/`Stop` timing operations and `TransitionToRunning`/`TransitionToCompleted` callbacks end to end before reporting a missing state assignment. Simulation-driven transitions are not proof that real scheduling callbacks ran.
- Pre/post-roll and node-specific timing affect reservation/execution windows. Running/completed/canceled operations have different edit constraints; inspect timing resolvers, freeze checks and actual validators.
- Draft persistence, tentative validation/reservation, confirmation and automatic assignment are separate steps. A handled availability failure can intentionally leave a saved Draft job; read the typed exception and save sequence.
- Bulk operations can partially succeed. Inspect successful/unsuccessful IDs and typed trace data instead of assuming all-or-nothing behavior or parsing error strings.
- Recurrence includes occurrence generation, repeat invocation/idempotency, series relationships and future-job cancellation. Separate API behavior from the external generator script's responsibility.
- Job information/nodes/timing can update DOM, core reservations, lightweight query data and Live configuration. Inspect all representations reached by the changed path, not every representation for every change.

## Configuration and shared contracts

Resource pools, workflows, recurring jobs and jobs share settings and orchestration contracts. Follow [configuration-state calculation](../../../../DevPack/API/ConfigurationStateCalculator/), [linking/reference resolution](../../../../DevPack/API/Linking/), [validators](../../../../DevPack/API/Validators/), [orchestration handlers](../../../../DevPack/API/Handlers/Orchestration/) and the affected objects/storage conversion.

Trace mandatory values, literal values versus references, node/job scope, unresolved or circular chains, type conversion and destination input types. Dynamic inputs have reevaluation/link-support contracts. A stored reference, resolved value and actual script input are different stages; load [Live](live.md) when that execution boundary changes.

Generic properties, property setting collections, scheduling properties and relationships live under the corresponding `API/{Objects,Repositories,Handlers}/Properties/`, `Relationships/` and workflow folders. Trace definition/value identity, usage/deletion checks, file attachment handling and storage round-trips. Preserve established bulk result/exception contracts through [DevPack/Exceptions](../../../../DevPack/Exceptions/).

For shared rules, use the matching `DevPack.Tests/Generic/`, `Properties/`, `Relationships/`, orchestration and workflow tests. For a cross-boundary defect, serialization-only coverage does not prove that the configured value reaches its consumer.

## Queries and persistence

Start in [API/Querying](../../../../DevPack/API/Querying/) and [Storage](../../../../DevPack/Storage/), then follow the repository and domain handler using the translator/cache/model. The matching query/filter tests are under `DevPack.Tests/{RST,Workflow,Properties,Relationships}/`, with shared query tests under `DevPack.Tests/Querying/`.

- Trace filtering, paging, sorting, null/value conversion and API-to-DOM/core field mappings through the actual query path. Check supported combinations and page boundaries reached by the change.
- Lightweight/persisted projections are not complete API objects. Follow their writes and readers before treating an omitted field as data loss.
- Cache invalidation, subscriptions and connection-scoped state must be understood in their real ownership/lifetime. Follow readers/writers and disposal rather than proposing a new cache or polling layer without need.
- [DOM models](../../../../DevPack/Storage/DOM/Model/) include generated schema-facing code. Stable IDs and mappings must agree with the installed solution schema, owned by the external Plan application package. A fresh simulation alone does not prove upgrade compatibility.
- Check the simulation's query/message/storage support when interpreting a test, but do not change simulation behavior to conceal a production mismatch.

## Further reading

Use the local [getting started](../../../../Documentation/Getting%20Started.md), [quick reference](../../../../Documentation/Quick%20Reference.md) and [advanced topics](../../../../Documentation/Advanced%20Topics.md) together with actual PR source. External application documentation and dependency navigation are in [solution-map](solution-map.md); retrieve release-matched evidence through [GitHub source retrieval](github-source.md).

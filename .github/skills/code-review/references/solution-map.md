# Solution map

Read this for every review. Unqualified paths refer to `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan`, not the Plan application solution. This is a search map, not a frozen API specification.

## Goal and execution model

MediaOps connects operational planning with execution against media infrastructure. Plan describes resources, reusable workflows, timed jobs and recurring series. Live can execute the resulting automation and connectivity. People & Organizations (PNO) and Categories supply related models used across the applications.

This repository owns the reusable Plan API. Direct consumers do not have to pass through the application's UI:

```text
Automation / connector / GQI / IConnection consumer
  -> context adapter / connection extension -> IMediaOpsPlanApi
  -> repositories -> handlers + validation + reference resolution
  -> DOM + core resources/reservations + optional Live configuration
```

DOM is the DataMiner object model used for persisted solution data. Core resource/reservation management, API objects, persisted DOM objects, query projections and Live events are related representations, not interchangeable objects. Trace the conversions and ownership at the changed boundary.

Plan and Live are independently usable. A referenced or deployed Live DLL does not establish that the Live solution is installed. Verify installation checks before reviewing optional functionality.

## Local ownership

| Project or location | Owns |
| --- | --- |
| [DevPack/API](../../../../DevPack/API/) | `IMediaOpsPlanApi`, objects, repositories, handlers, validators, configuration/reference resolution and querying |
| [DevPack/Storage](../../../../DevPack/Storage/) | DOM models/mappings/helpers and core resource/reservation storage helpers/caches |
| [DevPack/Tools](../../../../DevPack/Tools/), [logging](../../../../DevPack/Logging/), [exceptions](../../../../DevPack/Exceptions/) | Connection/runtime helpers, locking, logging and typed error/bulk-result contracts |
| [DevPack.Automation](../../../../DevPack.Automation/) | NuGet library adapting `Engine`/`IEngine` to the shared API |
| [DevPack.GQI](../../../../DevPack.GQI/) | NuGet library adapting `GQIDMS` to the shared API, not a standalone GQI data source |
| [DevPack.Protocol](../../../../DevPack.Protocol/) | NuGet library adapting `SLProtocol` to the shared API, not a connector/protocol definition |
| [DevPack.UnitTesting](../../../../DevPack.UnitTesting/) | Simulation, connection/message handling, in-memory stores and query support; not an executable test suite |
| [DevPack.Tests](../../../../DevPack.Tests/) | MSTest API/domain tests, simulation-specific tests and tests that can use a real DataMiner Agent |
| [QAOpsPackage](../../../../QAOpsPackage/) | Skyline SDK `TestPackage`, test-output harvesting and QAOps execution pipeline |
| [DevPack.Injector](../../../../DevPack.Injector/) | Manual executable/tool; not the test runner or the application installer |

All eight projects target `net48`. The core and adapter projects are libraries; `DevPack.Injector` is an executable. [MediaOps Plan.slnx](../../../../MediaOps%20Plan.slnx) selects the projects. The package ID prefix is `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan`; the namespace is `Skyline.DataMiner.Solutions.MediaOps.Plan`.

There are no local application scripts, low-code app archives, Playwright projects or full Plan application installer. Do not apply those projects' review/runner instructions to these libraries merely because an adapter uses the same runtime API.

## Ownership across repositories

All repositories below belong to `SkylineCommunications`. External source entry links are navigation snapshots inherited from the source review maps, not proof of the release used by a PR. **Resolve actual package/application versions and substitute their producing commits before using external implementation or tests as evidence.** Follow [GitHub source retrieval](github-source.md).

| Short name / repository | Owns and source entry points |
| --- | --- |
| Plan solution: [SLC-S-MediaOps.Plan](https://github.com/SkylineCommunications/SLC-S-MediaOps.Plan) | Application scripts/GQI/shared logic and deployment. [Shared application code](https://github.com/SkylineCommunications/SLC-S-MediaOps.Plan/tree/5dfdcd9e8fd9a85e8456aaea740c89d47e9dc4fb/MediaOpsCommon), `MediaOpsShared/`, `DOM Classes/`, `SolutionInstallerTools/`, `SLC-S-MediaOps/`, solution integration and Playwright tests |
| Live DevPack: [Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Live](https://github.com/SkylineCommunications/Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Live) | Consumed connectivity/orchestration contracts. [Core API, orchestration and mediation](https://github.com/SkylineCommunications/Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Live/tree/193b857569c646f82c81da9204e340d1b563df45/Skyline.DataMiner.MediaOps.Live), `Skyline.DataMiner.MediaOps.Live.Plan/`, `Skyline.DataMiner.MediaOps.Live.Tests/` |
| Live solution: [SLC-S-MediaOps.Live](https://github.com/SkylineCommunications/SLC-S-MediaOps.Live) | Executable orchestration/connectivity scripts, apps and deployment. [Event execution](https://github.com/SkylineCommunications/SLC-S-MediaOps.Live/tree/f0eeb65b67677c2b1daa209d355c4889e29eb3b3/ORC-AS-EventOrchestration), `ORC-AS-SlidingWindowScheduler/`, `VSG-*`, `MediaOpsLiveCommon/`, `MediaOps.Live-Package/` |
| PNO DevPack: [Skyline.DataMiner.Dev.Utils.Solutions.PeopleAndOrganizations](https://github.com/SkylineCommunications/Skyline.DataMiner.Dev.Utils.Solutions.PeopleAndOrganizations) | Consumer mapping people/teams into Plan resources/pools; not a direct dependency in this repository's package declarations. [API and MediaOps handlers](https://github.com/SkylineCommunications/Skyline.DataMiner.Dev.Utils.Solutions.PeopleAndOrganizations/tree/b6077155ca37d371cf1545ae09d1db8fcf14ad31/DevPack/API), `DevPack/Storage/`, `DevPack.Tests/` |
| Categories DevPack: [Skyline.DataMiner.Dev.Utils.Solutions.Categories](https://github.com/SkylineCommunications/Skyline.DataMiner.Dev.Utils.Solutions.Categories) | Consumed scopes, hierarchy, membership, query/cache contracts. [API](https://github.com/SkylineCommunications/Skyline.DataMiner.Dev.Utils.Solutions.Categories/tree/9d548fbad243b8477c17f7a22a19151ef0715d09/Skyline.DataMiner.Utils.Categories/API), `Skyline.DataMiner.Utils.Categories/DOM/`, `Skyline.DataMiner.Utils.Categories.Tests/` |
| Categories solution: [SLC-LCA-Categories](https://github.com/SkylineCommunications/SLC-LCA-Categories) | Scope/category management app, scripts, GQI and deployment. [Management script](https://github.com/SkylineCommunications/SLC-LCA-Categories/tree/816e76b215953a809ce4e2a652732b11fe5c3302/CAT-AS-CategoriesManagement), `CAT-GQIDS-Categories/`, `SLC-LCA-Categories-Package/`, `SLC-LCA-Categories.Tests/` |

The declared Live and Categories versions are in [Directory.Packages.props](../../../../Directory.Packages.props); inspect affected projects for actual references and overrides. Other DataMiner/SDM/DOM/locking dependencies matter only when the changed execution path reaches their contracts. Do not assume all related packages, applications or consumers use the same release number.

## Route a change

| Starting point | Next places to inspect |
| --- | --- |
| Resource/pool/capability/capacity/configuration API | [Resource Studio](plan.md#resource-studio), `API/{Objects,Repositories,Handlers}/ResourceStudio/`, relevant usage validators, `DevPack.Tests/RST/` |
| Workflows, node graphs, connections, clone/remap | [Workflows](plan.md#workflows-and-node-graphs), `API/Objects/Workflow/`, handlers/repositories, graph validators, `DevPack.Tests/Workflow/` |
| Jobs, timing, lifecycle, recurrence | [Scheduling](plan.md#scheduling), job DOM/core/Live handlers, validators and workflow tests; solution callbacks when reached |
| Configuration, references, properties or relationships | [Configuration and shared contracts](plan.md#configuration-and-shared-contracts), linking/storage code and matching `Generic/`, `Properties/`, `Relationships/` tests |
| Filters, paging, sorting, persisted/query conversions | [Queries and persistence](plan.md#queries-and-persistence), `API/Querying/`, `Storage/`, domain query/filter tests and simulation translators |
| Automation/GQI/Protocol extension | [API entry points and adapters](plan.md#api-entry-points-and-adapters), connection extension, actual runtime caller and package references |
| Orchestration, Live events or VSG references | [Live](live.md), both sides of the actual boundary, orchestration and job simulation tests |
| Externally managed resources/pools or PNO ownership | [PNO](people-and-organizations.md), consumer mappings and affected Plan contracts |
| Categories or scope membership | [Categories](categories.md), local category handlers and dependency implementation |
| Project/package/dependency/DOM mapping/CI changes | [Packaging and CI](packaging-and-ci.md), affected consumers and test enablement |
| Test-only or simulation changes | [Testing](testing.md), production behavior claimed by the test and other users of the simulation |

The routes above use paths below `DevPack/` unless a project is named explicitly. Follow current symbols and project references; an old-looking namespace alone does not prove a legacy package dependency.

## Source and version discipline

1. Review local source/tests at the PR base/head. Do not replace changed Plan code with this repository's previously released NuGet source.
2. Resolve external dependency versions at those revisions and read their implementation/tests at the producing commits. For external consumers, establish the supported/relevant consumer release and its Plan dependency; distinguish released compatibility from an explicitly coordinated future change.
3. Distinguish the NuGet dependency, packaged DLL and installed application version. This library does not own the complete application's installation/upgrade path.
4. Prefer PR intent, applicable API/product documentation, version-matched source and executable tests over historical notes. An implementation bug is not made correct just because existing code does it.
5. Do not treat roadmap features, draft designs or old examples as requirements. Missing external access/provenance means an unverified boundary, not a speculative defect.
6. Hosted review does not inherit local paths, private repository access or OpenCode MCP servers. Follow [hosted-review setup](../../../COPILOT-REVIEW-SETUP.md), verify actual reads, and respect the public repository's confidentiality boundary.

## Documentation and provenance

- Local [getting started](../../../../Documentation/Getting%20Started.md), [quick reference](../../../../Documentation/Quick%20Reference.md) and [advanced topics](../../../../Documentation/Advanced%20Topics.md) are navigation/contract context. Check examples against the PR's implementation.
- [Public MediaOps documentation source](https://github.com/SkylineCommunications/dataminer-docs/tree/main/solutions/standard_solutions/MediaOps) covers Plan/Live applications, development and tutorials. Pin a document revision when using its content as evidence.
- Internal Plan/Live/PNO documentation in `SkylineCommunications/internal-docs` is optional, permission-sensitive historical context. It is not automatically needed or safe to quote in this public repository.

Adapted from the [Plan solution review skill](https://github.com/SkylineCommunications/SLC-S-MediaOps.Plan/blob/5dfdcd9e8fd9a85e8456aaea740c89d47e9dc4fb/.github/skills/code-review/SKILL.md). Local project layout, dependency declarations and runner wiring were checked on 2026-10-30. External navigation snapshots retain the source map's provenance; they are not a fresh verification of every dependency or consumer release.

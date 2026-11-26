# Categories map

Load for category/scoped hierarchy definitions, assignments, navigation filters or category cache/query behavior. Categories is a direct dependency; use [solution-map](solution-map.md#ownership-across-repositories) and [GitHub source retrieval](github-source.md) to resolve the actual consumed package before reading external source/tests.

## Goal and ownership

Categories supplies scoped hierarchical classification. Definitions and object membership are separate concerns: the Categories app manages scopes/categories, while consumers manage which of their objects belong to them.

| Layer | Owning source |
| --- | --- |
| Plan category/scoped identifiers | Local [CategoryScopes](../../../../DevPack/API/Objects/Categories/CategoryScopes.cs), resource-pool/job objects and affected querying/storage |
| Plan membership writes/validation | Local [DomResourcePoolHandler](../../../../DevPack/API/Handlers/ResourceStudio/ResourcePools/DomResourcePoolHandler.cs), [DomJobCategoryHandler](../../../../DevPack/API/Handlers/Workflow/Jobs/DomJobCategoryHandler.cs) and callers |
| Shared API/model | Categories DevPack `Skyline.DataMiner.Utils.Categories/API/{Objects,Repositories,Validation,Querying}/` |
| Cache/notifications | Categories DevPack `API/{Caching,Subscriptions}/` below `Skyline.DataMiner.Utils.Categories/` |
| Persistence | Categories DevPack `Skyline.DataMiner.Utils.Categories/DOM/` |
| Scope/category management and deployment | Categories solution `CAT-AS-*`, `CAT-GQIDS-*`, `SLC-LCA-Categories-Package/` |
| Application readers | Plan solution `MediaOps.GQI.Scheduling/CategoriesNavigation/`, other domain read models and app bindings; Live classification consumers when reached |

Search external symbols such as `CategoriesApi`, `StaticCategoriesCache`, `Category`, `Scope`, `CategoryItem` and `CategoryItemIdentifier`. NuGet, namespace and source-folder names differ; do not copy a namespace from old documentation without checking source.

## Contracts to investigate when affected

- A category belongs to a scope and can have a parent. The same display name can exist under different parents; a name alone is not a globally unique identifier.
- Hierarchy-aware filtering differs from direct membership. Check whether descendants and aggregate counts are intentionally included.
- `CategoryItemIdentifier` combines string `ModuleId` and `InstanceId` and can identify non-DOM objects. Plan's own object IDs may legitimately be GUIDs; distinguish a verified Plan-specific scope from assumptions imposed on the generic Categories contract.
- The Plan object's identity is not the membership object's identity. Trace create/update/delete, duplicate/remap behavior and stale memberships with the correct identifiers.
- A syntactically valid category ID is not proof it belongs to the required scope. Follow validation and actual assignment at the changed call site.
- Cache/subscription reads have lifecycle and propagation contracts. Trace changes to readers before proposing extra polling or invalidation.
- Source/destination classification in Live can use different scopes. Do not broaden a Plan filter to unrelated scopes without a requirement.
- PNO has a separate classification model; confirm the actual API before applying these rules.

## Tests and escape-gap investigation

- Local [resource-pool category tests](../../../../DevPack.Tests/RST/ResourcePools/CategoryAssignmentTests.cs) and [job category tests](../../../../DevPack.Tests/Workflow/Jobs/CategoryAssignmentTests.cs) assert observable Plan membership behavior. Follow the fixture to establish simulated versus real-DMA execution.
- Local query/filter tests cover affected Plan API readers. External Plan scheduling GQI/Playwright tests cover application navigation/bindings when the changed boundary reaches them.
- Categories DevPack `Skyline.DataMiner.Utils.Categories.Tests/` owns API/model/repository/query coverage. Categories solution `SLC-LCA-Categories.Tests/` owns GQI/app checks. Read the suite at the relevant package/app commit, not a newer branch tip.

Distinguish a wrong membership write from a wrong hierarchy/query/cache result. Close the missed-test gap at the responsible layer and assert the consumer outcome for cross-boundary defects. Do not require unrelated tests of every consumer; apply [the testing gate](testing.md).

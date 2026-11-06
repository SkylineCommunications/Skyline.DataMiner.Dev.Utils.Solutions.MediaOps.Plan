# DOM installation

`IMediaOpsPlanApi.InstallDomModules()` installs or reconciles the repository-owned DOM schemas. It uses the API's existing connection and logger; constructing the API does not install anything.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// connection is the host's existing authenticated IConnection.
connection.GetMediaOpsPlanApi().InstallDomModules();
```

Call this explicitly from an installation, upgrade, or repair entry point, not from ordinary data-access paths. `IsInstalled()` checks SDM registration, not schema completeness, and is not a reason to skip reconciliation.

## Prerequisites and scope

- DataMiner **10.6.11 or later**. The consuming installer must enforce this prerequisite; this method does not probe the server version.
- The calling identity needs permission to configure module settings. DOM Manager reinitialization can briefly interrupt concurrent operations against that module.
- The consuming package remains responsible for deploying referenced scripts and any external prerequisites. Schema declarations do not install or execute those scripts, install referenced external modules, or create their records.
- There is no export API or runtime JSON/filesystem dependency. The existing solution importer/exporter and its package wiring have not been changed by this migration.

The modules are installed sequentially in this order:

| Module | Sections | Fields | Behaviors | DOM definitions |
| --- | ---: | ---: | ---: | ---: |
| `(slc)people_organizations` | 13 | 34 | 3 | 6 |
| `(slc)properties` | 5 | 17 | 0 | 2 |
| `(slc)relationships` | 2 | 11 | 0 | 2 |
| `(slc)resource_studio` | 23 | 62 | 2 | 5 |
| `(slc)workflow` | 20 | 135 | 3 | 8 |
| **Total** | **63** | **259** | **8** | **23** |

Together with the five module-settings objects, these are 99 declared schema objects. The declarations retain the approved 74 soft-deleted fields, 16 soft-deleted section links, retired definitions, and the unlinked People and Organizations Billing section. Collaboration is deliberately excluded from the approved People and Organizations baseline.

## Layout and reconciliation

- `DevPack/Storage/DOM/Definitions/`: explicit native C# declarations, grouped by module. Generated instance wrappers remain separate and do not infer schema configuration.
- `DevPack/Storage/DOM/Interfaces/`: internal module and definition contracts.
- `DevPack/Storage/DOM/Tools/DomModuleInstaller.cs`: connection-independent reconciliation through a message-handler delegate.
- `DevPack/API/MediaOpsPlanApi.cs`: explicit module registration and the public entry point, constructing fresh declarations on every call.

For each module, the installer validates its declaration collections and identities before sending messages, then processes:

1. Module settings.
2. All declared section definitions, including additional unlinked sections. Identical shared sections are processed once; conflicting declarations with the same ID fail validation.
3. Behavior definitions in their declared order.
4. DOM definitions in their declared order.
5. DOM Manager reinitialization.

Existing objects are found by stable ID, never by name. Missing objects are created, equal objects are skipped, and different objects are updated. Custom sections use the typed `CustomSectionDefinition.Equals(CustomSectionDefinition)` overload. Settings and behavior updates submit the incoming declarations without merging target customizations.

When a managed section loses a field, or a managed definition loses a section link, the existing missing item is retained with `IsSoftDeleted = true`, including its metadata. Incoming items with the same ID take precedence, including their deletion flag. Entire sections, definitions, and modules omitted from installation remain untouched.

This means extra target-side fields and links **inside managed objects can be soft-deleted**. Unrelated whole schema objects are not pruned. Changing a field ID does not move its values to the new field. No instance conversion, backfill, seeding, deletion, or cleanup is performed.

Equality is checked before omitted items are appended as tombstones, matching Live. Repeated installation after a removal may still issue updates; it is not an unconditional zero-write operation. Every completed module also requests reinitialization, even if its schema objects compare equal.

## Failures and existing caches

Exceptions from reads, writes, logging, and reinitialization propagate. A missing or unsuccessful reinitialization response is a failure, unlike the old importer's log-only handling. Normal return means all requested operations returned without a reported failure, not that an independent read-back or application-level verification was performed.

There is no transaction, rollback, retry, installation lock, or concurrent-installer coordination. Earlier writes remain after a failure, and subsequent modules are not attempted. Re-running starts from the beginning and reads the target state again. Logs describe intended operations and precede the corresponding writes.

Plan's existing `InstanceFactory` caches soft-deleted-field IDs per module for the lifetime of the process. This API does not invalidate that cache, matching Live's lack of installer cache invalidation; Live itself has no equivalent cache. A process that already populated Plan's cache can retain an outdated field list after a schema change. Refresh the affected consuming process through its normal operational lifecycle before relying on that list; a new API object alone does not clear a static cache. No changes were made to Plan's existing instance-reading cleanup behavior.

## Approved source baselines

- Installation algorithm and architecture: [Live at ccfcb594eeb536f637e916c76c0427b163107e7a](https://github.com/SkylineCommunications/Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Live/tree/ccfcb594eeb536f637e916c76c0427b163107e7a/Skyline.DataMiner.MediaOps.Live/DOM).
- Base schema: [Plan solution DOM payload at 7a79bf3768337de562d06bd1bde0a6c062951fe5](https://github.com/SkylineCommunications/SLC-S-MediaOps.Plan/tree/7a79bf3768337de562d06bd1bde0a6c062951fe5/SLC-S-MediaOps/SetupContent/DOM), consolidated with existing DevPack identities and the Flow Rules delta below.
- Flow Rules: the solution's companion `feature/flow-rules` commit [ab6c315db41a3f0b1a682059108ccdfb2d3f4f90](https://github.com/SkylineCommunications/SLC-S-MediaOps.Plan/commit/ab6c315db41a3f0b1a682059108ccdfb2d3f4f90), linked from [DevPack PR #396](https://github.com/SkylineCommunications/Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan/pull/396). The delta includes the FlowRules definition, FlowRulesInfo and FlowRule sections, the Configuration info reference field, and all three ResourcePool behavior rules allowing that field.
- People and Organizations: user-approved `Rysomez.zip`, SHA-256 `76373a62d3d4ed349d0db420d42972128c1e0ac9d15a44e0892a01d9f8fbd796`. Its schema values match the base payload; Collaboration is not present. Its IDs and declarations are self-contained in `SlcPeopleOrganizations` for a future package split, without a dependency on the separate People and Organizations DevPack.

Plan-specific extensions to Live are explicit behavior installation, unlinked-section support, per-module declaration validation/shared-section deduplication, all-sections-first ordering, and checked DOM Manager reinitialization. There is no general dependency graph or automatic discovery.

## Unit validation and remaining acceptance checks

`DevPack.Tests/Storage/DOM/DomSchemaParityTests.cs` contains 99 independently captured SHA-256 expectations, one per approved schema object. They cover nested fields, defaults, references, enum labels and values, lifecycle script settings, behaviors, and array ordering. Normalization excludes only top-level platform tracking fields, assembly-version qualifiers in type metadata, and typed collection envelopes; schema properties and concrete descriptor/action types are retained. Update an expectation only after reviewing the authoritative schema change, not by accepting the implementation's current output as the baseline.

The API and lifecycle unit tests use an isolated in-memory SLNet store with definition validation enabled. They do not load credentials or construct the regression integration context. They cover first installation, equal/repeated installation, partial repair, updates, soft-delete preservation/reintroduction, unrelated objects, failure and rerun, logging failures, invalid declarations, reinitialization failures, and unchanged stored instance data during reconciliation.

Run only these isolated unit tests from the repository root:

```powershell
dotnet test 'DevPack.Tests/Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.Tests.csproj' -c Release --no-restore -p:GeneratePackageOnBuild=false --filter 'TestCategory=UnitTest&TestCategory!=IntegrationTest'
```

Builds and in-memory tests do **not** establish DataMiner 10.6.11 acceptance. No integration tests or live-system validation were performed for this migration. In particular, validate supported upgrades with existing instance data, external references, permissions/reinitialization, and empty Flow Rules collections before production use.

Two legacy Workflow behavior anomalies are deliberately preserved: duplicate field rules, including a conflicting Job ID visibility rule in the Running state, and references to monitoring field ID `a6649369-2c5c-4b84-a745-6aa94ca8d550` without a corresponding field declaration. Correcting these would change the approved schema and is separate work; server acceptance remains unverified.

The future solution switch must deploy the new DevPack first, enforce the 10.6.11 prerequisite, and make its existing script delegate to this method at the schema-installation phase. It must also propagate API errors as installation failures: the existing `MediaOpsScript.HandleException` default implementation only logs through its caller and does not report a failed subscript. Preserve the solution's separate initialization, data migrations, and scheduled cleanup. Exporter retention and obsolete payload removal remain deferred.

See [ADR 0001](../docs/adr/0001-code-first-dom-installation.md) for the ownership and compatibility decision.

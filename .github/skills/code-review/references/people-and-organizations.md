# People & Organizations consumer map

Load for PNO-managed Plan resources/pools, ownership, bookability or changes affecting a PNO consumer contract. PNO is not a direct dependency in this repository's current package declarations. Do not load its full implementation for an unrelated resource change.

Use [solution-map](solution-map.md#ownership-across-repositories) for aliases/source entry points and [GitHub source retrieval](github-source.md) to identify the relevant consumer release, its Plan dependency and release-matched implementation/tests.

## Goal and ownership

PNO models people, organization/team memberships, skills/experience and operational contacts. Bookable people/teams map into Plan resources/pools so human availability participates in planning.

| Layer | Owning source |
| --- | --- |
| Plan resource/pool contracts | Local `DevPack/API/{Objects,Repositories,Handlers}/ResourceStudio/`, usage validators and core/DOM mappings |
| PNO entity rules | PNO DevPack `DevPack/API/{Objects,Repositories,Validators,Querying}/`, `Handlers/{People,Teams,Organizations,Skills,Roles,Experience,Categories}/` |
| PNO -> Plan mapping | PNO DevPack `DevPack/API/Handlers/MediaOps/MediaOpsResourceHandler.cs` and `MediaOpsResourcePoolHandler.cs` |
| PNO persistence/cache | PNO DevPack `DevPack/Storage/DOM/`, `DevPack/Cache/` |
| Application scripts/read models | Plan solution `People and Organizations -*`, `PNO-*`, shared application code, People & Organizations app/GQI and scheduling owner/contact consumers |

## Contracts to investigate when affected

- Person/organization/team lifecycle and bookability prerequisites belong to PNO's API, not only its UI. Do not introduce PNO business rules into Plan without an actual ownership requirement.
- Bookable teams map to Plan pools, people to resources, and skills to capabilities. Follow both PNO source state and the mapped Plan state for changed create/update/deprecate/delete behavior.
- Externally managed resources/pools can have ownership/edit restrictions. Check supported direct Plan operations and the mapping contract before recommending removal or bypassing the owning integration.
- Organization membership and team membership differ, and memberships carry role information. A display-name collection is not an equivalent identity/relationship model.
- Mapping/bulk handlers can partially succeed. Read bulk results and error translation before assuming rollback or that all objects changed.
- Scheduling ownership/contact identifiers are consumers of PNO identities. Trace application creation/editing and persistence if the changed contract reaches those fields.
- PNO's own category objects are not automatically the same model as the separate hierarchical Categories solution. Load [Categories](categories.md) only for that actual boundary.

## Tests and escape-gap investigation

- Local `DevPack.Tests/RST/` and relevant workflow tests establish the Plan-side resource/pool/usage contracts. They do not prove PNO synchronization executed.
- PNO DevPack `DevPack.Tests/PeopleOrganization/`, especially `MediaOps/`, owns mapping/bookability tests; inspect its actual assertions and consumed Plan version.
- Plan solution Playwright `PNO/` and `SCH/` tests cover application lifecycle, skills, ownership/contact consumers and related UI behavior. They are external, not local test projects.

A bookability or ownership compatibility fix must identify which validation/synchronization boundary escaped old tests. Assert both the affected PNO state and observable mapped Plan state when the defect crosses that boundary; a mocked successful Plan call is not enough. Keep released compatibility distinct from an explicitly coordinated future consumer change, and apply [the testing gate](testing.md).

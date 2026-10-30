# Packaging, compatibility and CI map

Load for public API compatibility, NuGet contents, DOM mappings, dependency versions, CI, runners and test discovery. Paths refer to this DevPack repository unless another repository is named.

## Build and artifact ownership

| Location | Purpose |
| --- | --- |
| [Core project](../../../../DevPack/Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.csproj) | Shared Plan API NuGet library, documentation and package assets |
| [Automation](../../../../DevPack.Automation/), [GQI](../../../../DevPack.GQI/), [Protocol](../../../../DevPack.Protocol/) | Context-adapter NuGet libraries referencing the local core project |
| [DevPack.UnitTesting](../../../../DevPack.UnitTesting/) | Simulation-support library; `GeneratePackageOnBuild` is false, and it is not itself a test suite |
| [DevPack.Tests](../../../../DevPack.Tests/Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.Tests.csproj) | MSTest suite and post-build harvesting of its output |
| [QAOpsPackage](../../../../QAOpsPackage/QAOpsPackage.csproj) | Skyline SDK Test Package, not the full Plan application package |
| [DevPack.Injector](../../../../DevPack.Injector/) | Manual executable/tool; inspect before use and do not run it during hosted review |
| [MediaOps Plan.slnx](../../../../MediaOps%20Plan.slnx), [Directory.Packages.props](../../../../Directory.Packages.props), [global.json](../../../../global.json) | Project selection, central package/transitive pinning and Skyline SDK version |

Projects target .NET Framework 4.8 (`net48`); CI uses Windows and .NET 10 tooling to build/run tests. Do not infer cross-platform execution support from SDK-style project syntax. Read actual imports/settings rather than assume the application solution's x86 configuration or `net481` test targets apply here.

Core/adapter Debug packaging can copy NuGet output to `C:\Skyline_Data\MyNugets\`. Prefer the existing Release CI path when checking builds and do not mistake a developer-machine output path for a hosted prerequisite.

## Public API and persisted contracts

- Review changed signatures, interfaces, defaults, overload resolution, enum/result semantics and exception behavior against the intended compatibility/release contract and affected consumers. Public callers can exist outside this repository.
- Follow public objects through handlers, validation, serialization and storage. API-only, binary-only and data-compatible changes are different claims.
- [Storage/DOM/Model](../../../../DevPack/Storage/DOM/Model/) contains generated schema-facing code and mappings. Stable module/field/behavior IDs and existing persisted objects must agree with the installed application schema. A fresh simulation cannot establish every upgrade path.
- The Plan solution's `SLC-S-MediaOps/SetupContent/DOM/`, package entry point and `SolutionInstallerTools/` own full schema/app installation and supported upgrades. Resolve their relevant app release when a mapper/schema change requires coordination.
- Do not require a local low-code app archive, scripts-only application package or Playwright project: those artifacts belong to external application repositories. Trace actual affected consumers through [solution-map](solution-map.md).

## Dependency and deployment contracts

- Read central versions, affected `.csproj` references, overrides/imports and resolved metadata for the exact revision/configuration. `Directory.Packages.props` also enables central transitive pinning.
- Resolve consumed package releases to producing commits with [GitHub source retrieval](github-source.md). Compare both versions for upgrades; do not use a later dependency branch as either release's evidence.
- NuGet version, assembly/file version, packaged DLL version and installed application version are not interchangeable. This repository's dependency declaration alone does not establish the DLL or solution installed on an Agent.
- The Plan application installer owns Plan/PNO deployment and its prerequisites. Installed Live can own its own binaries independently of Plan's fallback dependencies. Inspect version-matched installer ownership before reviewing runtime compatibility or proposing an overwrite.
- Live is optional. Installed binaries are not proof of an installed application or registered schema. Check the actual installation/capability boundary reached by the change.
- Verify core/adapter dependency agreement and inclusion of runtime assets where changed. A project reference, a NuGet reference, harvested test output and a deployed application artifact have different effects.
- Source-reading access is not private-feed restore access. Missing feed credentials are an execution limitation, not evidence that the dependency/version is invalid.

## CI entry points

- [Plan-CICD.yml](../../../../.github/workflows/Plan-CICD.yml) owns NuGet consolidation, tag/branch validation, simulated Unit Tests, conditional QAOps Integration Tests and gated Skyline CI/publishing.
- [Internal DataMiner CICD NuGet Solution](../../../../.github/workflows/private-dataminer-cicd-internalnugetsolution.yml) separately calls Skyline's reusable master workflow. Read actual trigger/gate behavior instead of assuming every green workflow ran the same checks.
- [DevPack.Tests.runsettings](../../../../DevPack.Tests.runsettings) and [MSTestSettings.cs](../../../../DevPack.Tests/MSTestSettings.cs) configure runner/method parallelism. Check fixture isolation rather than infer isolation from a concurrency setting.
- [TestDiscovery.ps1](../../../../QAOpsPackage/TestPackageContent/TestHarvesting/TestDiscovery.ps1) copies harvested output; it does not prove nonempty test discovery.
- [The QAOps execution pipeline](../../../../QAOpsPackage/TestPackageContent/TestPackagePipeline/2.TestPackageExecution.ps1) runs the harvested Plan test assembly and publishes TRX results through the QAOps pipeline library.

## CI contracts to verify when affected

1. **Revision agreement:** checkout, build, harvested package, test results and consumed dependencies must correspond to the intended SHA/configuration. Approval-triggered QAOps revalidates the current head.
2. **Eligibility:** ordinary PR simulated tests and real-DMA QAOps have different conditions. QAOps is conditional on manual dispatch, eligible non-alpha tags, or an approved current-head, same-repository, non-draft PR to `main`. Evaluate actual job conditions for the event; skip is not pass.
3. **Target selection:** tag validation chooses the default or `dma-rc` environment according to branch ancestry and `DMA_RC_ENABLED`. Environment-specific QAOps settings affect the actual target.
4. **Shared environment:** the QAOps job serializes the selected environment through completion. Within-run method parallelism still requires isolated data, cleanup and restoration of global settings.
5. **Discovery and freshness:** the test project harvests into `postbuild.generated`, then the package copies it to `tests.generated`. Verify current DLLs/dependencies and actual discovered/executed cases. The presence of a DLL is not proof of a nonempty run.
6. **Runtime readiness:** the Test Package's setup script is a placeholder and its installer installs default package content. Neither is evidence that the complete Plan/Live application, schema or callbacks were freshly deployed. Establish what the selected QAOps environment supplies.
7. **Publishing:** inspect stable/prerelease tag conditions, branch validation, required job outcomes, package source and version metadata. A green unrelated workflow does not establish that a gated publish or QAOps run occurred.
8. **Failures:** distinguish failed assertions, retries, skips, no discovery and no execution. Use revision-matched TRX/QAOps results rather than a summary omitting cases.

For a CI/test bug fix, explain why old checks missed it and close the exact discovery/filter/execution/assertion gap. Repair selection of a sufficient existing regression test instead of adding a duplicate. Apply [the testing gate and real-environment execution boundary](testing.md).

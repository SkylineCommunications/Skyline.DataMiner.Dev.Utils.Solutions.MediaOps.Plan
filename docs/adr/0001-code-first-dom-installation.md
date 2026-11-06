# Own DOM installation in the Plan DevPack

Use repository-owned native C# declarations and Live-style stable-ID reconciliation so schema installation is part of the DevPack API rather than a runtime filesystem import. Preserve the approved deployed metadata, including retired content, and keep instance migrations, script deployment, and export outside this API; Plan additionally requires behaviors, unlinked sections, and checked DOM Manager reinitialization.

Keep People and Organizations in this DevPack for now, with its own declaration folder and stable IDs and no dependency on the separate People and Organizations DevPack. This follows the agreed current ownership while avoiding a circular package dependency and keeping a future split localized.

The [installation reference](../../Documentation/DOM%20Installation.md) records immutable sources, intentional differences from Live, failure semantics, and validation limits. Moving the solution's caller and removing its old payload/exporter are deferred changes, not part of this decision's implementation.

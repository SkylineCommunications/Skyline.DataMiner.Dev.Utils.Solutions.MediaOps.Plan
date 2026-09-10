# What's New Since 1.5

This page summarizes what changed in the `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan` API after the 1.5.x line, split into what is **stable** (released as 1.6.x, up to and including 1.6.2) and what is currently **prerelease** (shipping as `1.7.0-alpha*` packages). Use it as a map to the detailed guides: [Getting Started](Getting%20Started.md), [Quick Reference](Quick%20Reference.md) and [Advanced Topics](Advanced%20Topics.md).

> [!NOTE]
> The NuGet package ID is `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan`; the C# namespace you `using` in code is the shorter `Skyline.DataMiner.Solutions.MediaOps.Plan` (for example `Skyline.DataMiner.Solutions.MediaOps.Plan.API`). Both names are correct — one identifies the package, the other the namespace.

## Stable: 1.6.x

These features are part of the released 1.6.0–1.6.2 packages.

- **Jobs and Workflows became full objects with CRUD support.** Previously limited to reading, `api.Jobs` and `api.Workflows` now support `Create`, `Update` and `Delete`, next to `Read`. `Job.FromWorkflow(api, workflow)` builds a new job from a completed workflow, copying its node graph, orchestration settings and property settings. See [Getting Started – Jobs](Getting%20Started.md#jobs) and [Getting Started – Workflows](Getting%20Started.md#workflows).
- **Node graphs.** `Job.NodeGraph` and `Workflow.NodeGraph` expose a `NodeGraph<TNode>` with `Add`, `Connect` (data connections between nodes), `Link` (parent/child grouping), and `Swap` (replacing a node's resource/pool assignment while keeping connections and links intact). See [Getting Started – Node graphs](Getting%20Started.md#node-graphs-and-node-relationships) and [Quick Reference – NodeGraph operations](Quick%20Reference.md#nodegraph-operations).
- **Generic properties and scheduling properties.** `api.Properties` manages custom `Property` definitions (`BooleanProperty`, `DiscreteProperty`, `StringProperty`) that can be attached to jobs, workflows and their nodes through `PropertySetting`s. `api.SchedulingProperties` is a convenience repository over the same model, scoped to properties intended for scheduling objects. See [Quick Reference – Properties](Quick%20Reference.md#properties).
- **Data references (`DataReference`).** Capability, capacity and configuration settings on orchestration settings can hold a `Reference` instead of (or besides) a literal value, so a value can be resolved dynamically from a job/resource property, a job name, or another parameter at runtime. See [Advanced Topics – Data references](Advanced%20Topics.md#data-references-and-reference-resolution).
- **`GlobalSettings` / `JobSettings`.** `api.GlobalSettings.GetJobSettings()` / `UpdateJobSettings(...)` expose the job key generation settings (prefix, digits, seed, increment) and the UI-only defaults for pre-roll/post-roll and desired job state.
- **`RecurringJob` was introduced as a read-only stub.** It could be read but not created, updated, transitioned or deleted. Full support arrived in 1.7.0-alpha (see below).

## Prerelease: 1.7.0-alpha

> [!IMPORTANT]
> The following APIs ship in `1.7.0-alpha*` packages (current: `1.7.0-alpha024`). They are **prerelease**: signatures and behavior can still change before a stable 1.7.0 release.

- **Job lifecycle.** `api.Jobs` gained explicit state transitions: `SaveAsTentative`, `Confirm`, `Start` (with `JobStartOptions`), `TransitionToRunning`, `Stop` (with `JobStopOptions`), `TransitionToCompleted`, `Cancel`, `ReturnToTentative` and `MarkAsCompleted`, plus `Delete(..., JobDeleteOptions)`. See [Advanced Topics – Job lifecycle](Advanced%20Topics.md#job-lifecycle) and [Quick Reference – Job lifecycle](Quick%20Reference.md#job-lifecycle).
- **`RecurringJob` is now a full object.** It supports `Create`/`Update`/`Delete`, the `Complete`/`Cancel` lifecycle, a `Pattern` (`RecurringPattern`/`RepeatType`/`WeekDays`) describing how the series repeats, `RecurringJob.FromJob(job)` to seed a series from an existing job, and relationship endpoints. See [Getting Started – Recurring jobs](Getting%20Started.md#recurring-jobs) and [Quick Reference – Recurring jobs](Quick%20Reference.md#recurring-jobs).
- **`Job.Duplicate()` / `Workflow.Duplicate()` / `RecurringJob.Duplicate()`.** Deep-copy an existing job, workflow or recurring job (including node graph, groups, property settings and orchestration settings) into a new, unsaved instance.
- **`ConfigurationState` and `ConfigurationStateCalculator`.** A single `ConfigurationState` enum (`Unknown`, `NoParametersDefined`, `MandatoryValuesMissing`, `NonMandatoryValuesMissing`, `AllValuesProvided`) now reports whether a job, recurring job or node has all required capability/capacity/configuration values (or references) filled in. See the migration note below and [Advanced Topics – Configuration state](Advanced%20Topics.md#configuration-state).
- **Categories on jobs, workflows and recurring jobs.** `Job.JobTypeCategoryId`, `Workflow.JobTypeCategoryId` and `RecurringJob.JobTypeCategoryId` link an object to a category from the separate Categories app/DevPack. MediaOps.Plan keeps that link in sync; it does not expose category definition CRUD itself.
- **Relationships.** `api.RelationshipObjectTypes` and `api.Relationships` manage generic parent/child links between arbitrary object types, and `Job`/`RecurringJob` expose `RelationshipEndpoints` to link a job or recurring job to external business objects (e.g., a booking). See [Quick Reference – Relationships](Quick%20Reference.md#relationships).
- **Eligible resources with usage.** `api.Resources.GetEligibleResources(EligibleResourcesContext)` returns resources that satisfy a set of capability/capacity requirements for a time range, together with their current and remaining concurrency/capacity usage (`EligibleResource.Usage`). See [Quick Reference – Eligible resources](Quick%20Reference.md#eligible-resources).
- **Resource Studio ↔ SRM synchronization.** `api.ResourcePools.GetOutOfSyncItems(...)` / `Synchronize(...)` detect and push differences between the DOM-based Resource Studio configuration and its SRM (Core) counterpart. See [Advanced Topics – Synchronization](Advanced%20Topics.md#synchronization-with-srm).
- **`FileProperty` / `FilePropertySetting`.** Generic properties can now hold file attachments (`FilePropertySetting.AddFile(fileName, content)` / `ReadContent(fileName)`), with an optional size limit (`FileProperty.HasSizeLimit` / `SizeLimit`, in MB). See [Quick Reference – File properties](Quick%20Reference.md#file-properties).
- **Typed `ConnectionConfiguration`.** Connections created with `NodeGraph<TNode>.Connect(from, to, configuration)` can carry an `AllLevelBasedConnectionConfiguration` or a `ShuffleLevelBasedConnectionConfiguration` (explicit level-to-level mapping), and nodes can be grouped with `NodeGraph<TNode>.AddGroup(name)` / `NodeGroup<TNode>`.
- **Timing constraints on jobs.** Confirmed/Running jobs enforce minimum guard intervals between pre-roll start, start, end and post-roll end, and restrict which boundaries can still change depending on the job's state. See [Advanced Topics – Timing constraints](Advanced%20Topics.md#timing-constraints).

## Migration notes

### Upgrading from 1.5.x to 1.6.x

- Replace any code that read jobs/workflows only through other means with `api.Jobs` / `api.Workflows`, and adopt `Create`/`Update`/`Delete` where you previously had to work around read-only access.
- `RecurringJob` remains read-only in 1.6.x; do not build tooling that creates, updates or transitions recurring jobs until you move to 1.7.0-alpha or later.

### Upgrading from 1.6.x to 1.7.0-alpha

- **`NodeConfigurationStatus`, `ResourceSelectionMode` and `ResourceSelectionState` were removed.** Replace usages with `ConfigurationState`, computed through `ConfigurationStateCalculator` (for example `calculator.GetNodeConfigurationState(node)` instead of reading a stored `NodeConfigurationStatus`).
- **`Job.Key` is no longer settable via object initializer.** In 1.6.x, `Key` was an init-only property (`new Job { Key = "MyKey" }`). In 1.7.0-alpha, `Key` has a private setter and can only be supplied on creation through `JobData`:

  ```csharp
  // 1.6.x
  var job = new Job { Key = "MyKey" };

  // 1.7.0-alpha
  var job = new Job(new JobData { Key = "MyKey" });
  ```

  Omit `JobData` entirely to let the system generate a key based on `JobSettings` (`KeyPrefix`, `KeyMinimumDigits`, `KeyStartingSeed`, `KeyIncrement`).
- Because `RecurringJob` is now fully editable, review any code that assumed recurring jobs were read-only or that only inspected them through the UI.

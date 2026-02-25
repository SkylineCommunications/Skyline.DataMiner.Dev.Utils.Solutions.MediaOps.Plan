# Advanced Topics

This document covers advanced features of the `Skyline.DataMiner.Solutions.MediaOps.Plan` API including state management, orchestration settings, resource pool options, logging, and installation checks.

## State Management

Resources and resource pools follow a lifecycle with three states: **Draft**, **Complete**, and **Deprecated**.

### Resource State Transitions

```mermaid
stateDiagram-v2
  direction LR
  Draft --> Complete
  Complete --> Deprecated
  Deprecated --> Complete
```
Resources are created in the **Draft** state. Once fully configured, they can be transitioned to **Complete** state to make them available for planning. A completed resource can be **Deprecated** when it is no longer needed, and restored back to **Complete** if required again.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
using Skyline.DataMiner.Solutions.MediaOps.Plan.Automation;

var api = engine.GetMediaOpsPlanApi();

// Create a resource (starts in Draft state)
var resource = api.Resources.Create(new UnmanagedResource
{
    Name = "Camera 1",
    Concurrency = 1,
});
// resource.State == ResourceState.Draft

// Transition to Complete
var completed = api.Resources.Complete(resource);
// completed[0].State == ResourceState.Complete

// Transition to Deprecated
var deprecated = api.Resources.Deprecate(resource);
// deprecated[0].State == ResourceState.Deprecated

// Restore from Deprecated back to Complete
var restored = api.Resources.Restore(resource);
// restored[0].State == ResourceState.Complete
```

### Batch State Transitions

State transitions can be performed on multiple resources at once:

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
using Skyline.DataMiner.Solutions.MediaOps.Plan.Automation;

// Complete multiple resources
var completedResources = api.Resources.Complete(new[] { resource1, resource2, resource3 });

// Deprecate multiple resources
var deprecatedResources = api.Resources.Deprecate(new[] { resource1.Id, resource2.Id });

// Restore multiple resources
var restoredResources = api.Resources.Restore(new[] { resource1, resource2 });
```

### Resource Pool State Transitions

Resource pools follow the same lifecycle:

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
using Skyline.DataMiner.Solutions.MediaOps.Plan.Automation;

// Complete a pool
var completed = api.ResourcePools.Complete(pool);

// Deprecate a pool
var deprecated = api.ResourcePools.Deprecate(pool);

// Deprecate with options
var deprecated = api.ResourcePools.Deprecate(pool, new ResourcePoolDeprecateOptions
{
    AllowResourceDeprecation = true, // Also deprecate resources in the pool
});

// Restore a pool
var restored = api.ResourcePools.Restore(pool);
```

## Resource Pool Options

### Delete Options

When deleting a resource pool, you can control what happens to its resources:

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
using Skyline.DataMiner.Solutions.MediaOps.Plan.Automation;

api.ResourcePools.Delete(pool, new ResourcePoolDeleteOptions
{
    DeleteDraftResources = true,      // Delete resources in Draft state
    DeleteDeprecatedResources = true,  // Delete resources in Deprecated state
});
```

### Deprecate Options

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
using Skyline.DataMiner.Solutions.MediaOps.Plan.Automation;

api.ResourcePools.Deprecate(pool, new ResourcePoolDeprecateOptions
{
    AllowResourceDeprecation = true, // Also deprecate resources assigned to this pool
});
```

## Orchestration Settings

Orchestration settings define automation behavior for resource pools and workflows. They contain capability, capacity, configuration, and orchestration event settings.

### Resource Pool Orchestration Settings

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.Automation;

var pool = api.ResourcePools.Read(poolId);

// Access orchestration settings
var orchestrationSettings = pool.OrchestrationSettings;

// Get configured capabilities
var capabilities = orchestrationSettings.Capabilities;

// Get configured capacities
var capacities = orchestrationSettings.Capacities;

// Get configured configurations
var configurations = orchestrationSettings.Configurations;

// Get orchestration events
var events = orchestrationSettings.OrchestrationEvents;
```

### Orchestration Events

Orchestration events define scripts that are executed at specific points during the orchestration lifecycle:

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Available event types:
// - OrchestrationEventType.PrerollStart
// - OrchestrationEventType.PrerollStop
// - OrchestrationEventType.PostrollStart
// - OrchestrationEventType.PostrollStop
```

### Updating Job Orchestration State

After an orchestration event has been executed, the job state can be updated:

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
using Skyline.DataMiner.Solutions.MediaOps.Plan.Automation;

api.Jobs.SetOrchestrationState(jobId, new OrchestrationUpdateDetails
{
    Event = OrchestrationEventType.PrerollStart,
    EventState = OrchestrationEventState.Succeeded,
    Message = "Preroll completed successfully",
});

// Report a failure
api.Jobs.SetOrchestrationState(jobId, new OrchestrationUpdateDetails
{
    Event = OrchestrationEventType.PrerollStart,
    EventState = OrchestrationEventState.Failed,
    Message = "Preroll failed: device not reachable",
});
```

## Logging

The API supports custom logging through the `ILogger` interface.

### Setting a Logger

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.Automation;
using Skyline.DataMiner.Solutions.MediaOps.Plan.Logging;

var api = engine.GetMediaOpsPlanApi();

// Set a custom logger
api.SetLogger(myLogger);
```

## Installation and Setup

### Checking Installation Status

Verify that the MediaOps.PLAN application is installed:

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.Automation;

var api = engine.GetMediaOpsPlanApi();

if (!api.IsInstalled())
{
    // Application is not installed
}
```

### Getting the Installed Version

Retrieve the version of the installed application:

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.Automation;

var api = engine.GetMediaOpsPlanApi();

if (api.IsInstalled(out string version))
{
    Console.WriteLine($"MediaOps.PLAN Version: {version}");
}
```

## System Capabilities

The API provides access to built-in system capabilities:

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.Automation;

// Access the Resource Type system capability
var resourceType = api.Capabilities.SystemCapabilities.ResourceType;

// The Resource Type capability includes discrete values:
// "Element", "Pool Resource", "Service", "Unlinked Resource", "Virtual Function"
```

## Next Steps

- **[Quick Reference](Quick%20Reference.md)** – Common snippets for repositories, querying, and resource management
- **[Getting Started](Getting%20Started.md)** – Installation and basic usage

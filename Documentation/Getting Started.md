# Getting Started

This documentation describes how to use the public API exposed by `Skyline.DataMiner.Solutions.MediaOps.Plan`.

## Installation

Add the NuGet package to your solution:

```bash
dotnet add package Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan
```

Depending on your project type, one of the following additional packages is also required:

- Automation scripts: `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.Automation`
- Protocols: `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.Protocol`
- GQI Ad-hoc Data Sources and custom operators: `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.GQI`

> [!NOTE]
> This library targets `.NET Framework 4.8`.

## Entry Point

The `MediaOpsPlanApi` class is the main entry point to the MediaOps.PLAN API.

It exposes:

- **Repositories** for reading/writing DOM-backed objects (Resources, Resource Pools, Capabilities, Capacities, Configurations, Resource Properties, Jobs, Workflows, Recurring Jobs)
- **State management** for transitioning resources and resource pools through lifecycle states
- **Logging** for custom logging integration

### Obtaining an API Instance

To obtain an instance of the `MediaOpsPlanApi` class, use the `GetMediaOpsPlanApi` extension method.
This extension method is available for automation scripts, connectors, GQI ad-hoc data sources, and custom operators.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Automation scripts
var api = engine.GetMediaOpsPlanApi();

// Protocols
var api = protocol.GetMediaOpsPlanApi();

// GQI ad-hoc data sources and custom operators
var api = gqiDms.GetMediaOpsPlanApi();
```

On other places the instance can also be created starting from an `IConnection` object:

```csharp
IConnection connection;
var api = new MediaOpsPlanApi(connection);
```

## Core Concepts

### Resources

Resources represent physical or virtual assets that can be planned and scheduled. The API supports several resource types:

- **UnmanagedResource** – a standalone resource with no external link.
- **ElementResource** – linked to a DataMiner element.
- **ServiceResource** – linked to a DataMiner service.
- **VirtualFunctionResource** – linked to a virtual function.

```csharp
var resource = api.Resources.Create(new UnmanagedResource
{
    Name = "My Resource",
    Concurrency = 1,
});
```

### Resource Pools

Resource Pools group related resources together and can define shared capabilities.

```csharp
var pool = api.ResourcePools.Create(new ResourcePool
{
    Name = "Camera Pool",
});
```

### Capabilities

Capabilities define what a resource or resource pool can do (e.g., resolution, codec support). They can contain a set of discrete values.

```csharp
var capability = api.Capabilities.Create(new Capability
{
    Name = "Resolution",
}
.SetDiscretes(new[] { "1080p", "4K", "8K" }));
```

### Capacities

Capacities define measurable quantities a resource can provide (e.g., bandwidth, storage). Two types are supported:

- **NumberCapacity** – a single numeric value.
- **RangeCapacity** – a range with minimum and maximum values.

```csharp
var capacity = api.Capacities.Create(new NumberCapacity
{
    Name = "Bandwidth",
    Units = "Mbps",
    RangeMin = 0,
    RangeMax = 1000,
});
```

### Configurations

Configurations define settings that can be applied to resources during orchestration. Four types are supported:

- **TextConfiguration** – a free-text value.
- **NumberConfiguration** – a numeric value.
- **DiscreteTextConfiguration** – a value selected from a set of text options.
- **DiscreteNumberConfiguration** – a value selected from a set of numeric options.

```csharp
var config = api.Configurations.Create(new TextConfiguration
{
    Name = "Output Format",
});
```

### Resource Properties

Resource Properties are custom metadata fields that can be attached to resources.

```csharp
var property = api.ResourceProperties.Create(new ResourceProperty
{
    Name = "Location",
});
```

### Jobs

Jobs represent scheduled work that references a workflow and contains planning details.

```csharp
var job = api.Jobs.Read(jobId);
```

### Workflows

Workflows define reusable planning templates.

```csharp
var workflows = api.Workflows.Read();
```

### Recurring Jobs

Recurring Jobs represent jobs that repeat on a schedule.

```csharp
var recurringJob = api.RecurringJobs.Read(recurringJobId);
```

## Basic Usage

Once you have an instance of the `MediaOpsPlanApi` class, you can start using its features.

### Creating Objects

```csharp
// Create a capability with discrete values
var resolution = api.Capabilities.Create(new Capability
{
    Name = "Resolution",
}
.SetDiscretes(new[] { "1080p", "4K" }));

// Create a number capacity
var bandwidth = api.Capacities.Create(new NumberCapacity
{
    Name = "Bandwidth",
    Units = "Mbps",
    RangeMin = 0,
    RangeMax = 10000,
});

// Create a resource pool
var pool = api.ResourcePools.Create(new ResourcePool
{
    Name = "Camera Pool",
});

// Create an element resource
var resource = api.Resources.Create(new ElementResource
{
    Name = "Camera 1",
    AgentId = 1,
    ElementId = 100,
    Concurrency = 1,
});

// Assign the resource to a pool
resource.AssignToPool(pool);
api.Resources.Update(resource);
```

### Reading Objects

```csharp
// Read all resources
var resources = api.Resources.Read();

// Read by ID
var resource = api.Resources.Read(resourceId);

// Read all resource pools
var pools = api.ResourcePools.Read();

// Read a job
var job = api.Jobs.Read(jobId);

// Read all workflows
var workflows = api.Workflows.Read();
```

### Updating Objects

```csharp
// Update a resource name
resource.Name = "Camera 1 Updated";
api.Resources.Update(resource);

// Add a capability to a resource
resource.AddCapability(new CapabilitySettings(resolution.Id)
    .SetDiscretes(new[] { "4K" }));
api.Resources.Update(resource);
```

### Deleting Objects

```csharp
// Delete a resource
api.Resources.Delete(resource.Id);

// Delete multiple resources
api.Resources.Delete(new[] { id1, id2, id3 });
```

## Next Steps

- **[Quick Reference](Quick%20Reference.md)** – Common snippets for repositories, querying, and resource management
- **[Advanced Topics](Advanced%20Topics.md)** – Orchestration settings, state management, logging, and more

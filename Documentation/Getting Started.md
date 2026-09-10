# Getting Started

This documentation describes how to use the public API of the `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan` NuGet package, used to build custom solutions on top of the MediaOps.PLAN application.

> [!NOTE]
> The NuGet package ID (`Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan`) is not the same as the C# namespace. In code, you `using` the shorter root namespace `Skyline.DataMiner.Solutions.MediaOps.Plan` (for example `Skyline.DataMiner.Solutions.MediaOps.Plan.API`).

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
>
> See [What's New Since 1.5](What%27s%20New%20Since%201.5.md) for a summary of what changed since 1.5.x, including which APIs are still prerelease (`1.7.0-alpha*`).

## Entry Point

The `MediaOpsPlanApi` class is the main entry point to the MediaOps.PLAN API.

It exposes:

- **Repositories** for reading/writing DOM-backed objects: Resources, Resource Pools, Capabilities, Capacities, Configurations, Resource Properties, Jobs, Workflows, Recurring Jobs, generic Properties, Scheduling Properties, Property Setting Collections, Relationships and Relationship Object Types
- **State management** for transitioning resources, resource pools and jobs through their lifecycle states
- **Global settings** (`GlobalSettings`) for application-wide configuration such as job key generation
- **Logging** for custom logging integration

This guide only covers the essentials of each concept. See the [Quick Reference](Quick%20Reference.md) for copyable snippets covering every repository, and [Advanced Topics](Advanced%20Topics.md) for state machines, timing rules and synchronization.

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
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

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
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

var resource = api.Resources.Create(new UnmanagedResource
{
    Name = "My Resource",
    Concurrency = 1,
});
```

### Resource Pools

Resource Pools group related resources together and can define shared capabilities.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

var pool = api.ResourcePools.Create(new ResourcePool
{
    Name = "Camera Pool",
});
```

### Capabilities

Capabilities define what a resource or resource pool can do (e.g., resolution, codec support). They can contain a set of discrete values.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

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
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

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
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

var config = api.Configurations.Create(new TextConfiguration
{
    Name = "Output Format",
});
```

### Resource Properties

Resource Properties are custom metadata fields that can be attached to resources.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

var property = api.ResourceProperties.Create(new ResourceProperty
{
    Name = "Location",
});
```

### Workflows

A `Workflow` is a reusable planning template: a node graph of resources/resource pools plus orchestration settings, without a concrete start/end time. Workflows are full CRUD objects and go through a simple **Draft → Complete** lifecycle (`api.Workflows.Complete(workflow)`); only a completed workflow can be turned into a job.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

var workflow = api.Workflows.Create(new Workflow
{
    Name = "Studio Recording",
    PreRoll = TimeSpan.FromMinutes(15),
    PostRoll = TimeSpan.FromMinutes(15),
});

workflow = api.Workflows.Complete(workflow);
```

### Jobs

A `Job` represents scheduled work: it has a concrete `Start`/`End` (and optional pre-roll/post-roll window), a node graph of resources/resource pools, and it goes through a lifecycle (see [Advanced Topics](Advanced%20Topics.md#job-lifecycle)). Jobs are full CRUD objects, most easily created from a completed workflow with `Job.FromWorkflow`:

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Build a job from a completed workflow; copies its node graph, groups, and orchestration/property settings.
var job = Job.FromWorkflow(api, workflow.Id);
job.Start = DateTimeOffset.UtcNow.AddHours(1);
job.End = job.Start.AddHours(2);
job.PreRollStart = job.Start - workflow.PreRoll;
job.PostRollEnd = job.End + workflow.PostRoll;

job = api.Jobs.Create(job);

// Update
job.Description = "Weekly studio recording";
job = api.Jobs.Update(job);

// Move it through its lifecycle
job = api.Jobs.SaveAsTentative(job);
job = api.Jobs.Confirm(job);
```

### Node Graphs and Node Relationships

`Job.NodeGraph` and `Workflow.NodeGraph` are a `NodeGraph<TNode>` of resource/resource-pool nodes with two kinds of relationships between them:

- **Connections** (`Connect`) – directed, data-oriented links between two nodes (for example, a source feeding a destination).
- **Links** (`Link`) – a simple parent/child hierarchy; a child has at most one parent.

Nodes can also be grouped for organizational purposes (`AddGroup`), and a node's resource/pool assignment can be replaced with `Swap` without losing its connections, links or group membership.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

var cameraNode = new JobResourceNode(cameraPool, camera);
var encoderNode = new JobResourceNode(encoderPool, encoder);

job.NodeGraph
    .Add(cameraNode)
    .Add(encoderNode)
    .Connect(cameraNode, encoderNode);

job.NodeGraph.AddGroup("Studio A").Add(cameraNode).Add(encoderNode);

// Replace the camera with another one, keeping its connections and group membership.
var newCameraNode = new JobResourceNode(cameraPool, backupCamera);
job.NodeGraph.Swap(cameraNode, newCameraNode);

job = api.Jobs.Update(job);
```

### Recurring Jobs

A `RecurringJob` describes a series of jobs that repeat on a `RecurringPattern` (daily, weekly, monthly or yearly). It is a full CRUD object with its own **Active → Completed/Cancelled** lifecycle, and can be built from an existing job with `RecurringJob.FromJob`:

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

var recurringJob = RecurringJob.FromJob(job);
recurringJob.Pattern.RepeatType = RepeatType.Weekly;
recurringJob.Pattern.RepeatEvery = 1;
recurringJob.Pattern.WeekDays = WeekDays.Monday | WeekDays.Wednesday;
recurringJob.Pattern.EndDate = DateTimeOffset.UtcNow.AddMonths(3);

recurringJob = api.RecurringJobs.Create(recurringJob);
```

### Generic Properties

Besides Resource Properties, jobs, workflows, recurring jobs and their nodes can carry **generic properties**: custom metadata fields defined once and assigned a value per object through a `PropertySetting` (`BooleanPropertySetting`, `DiscretePropertySetting`, `StringPropertySetting`, `FilePropertySetting`). `api.SchedulingProperties` is a convenience repository over the same `Property` model for properties meant to be used on jobs, workflows and recurring jobs (it manages properties in the `"MediaOps"` scope for you).

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

var notesProperty = (StringProperty)api.SchedulingProperties.Create(new StringProperty
{
    Name = "Client Reference",
});

job.AddProperty(new StringPropertySetting(notesProperty) { Value = "PO-12345" });
job = api.Jobs.Update(job);
```

### Relationships

A `Job` or `RecurringJob` can be linked to external business objects (a booking, a ticket, ...) through `RelationshipEndpoints`. Each link points to a `RelationshipObjectType` that describes what kind of object is on the other end.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

var bookingType = api.RelationshipObjectTypes.Create(new RelationshipObjectType { Name = "Booking" });

job.AddRelationshipEndpoint(new JobRelationshipEndpoint(bookingType.Id)
{
    ObjectId = "BOOK-42",
    ObjectName = "Client booking #42",
});
job = api.Jobs.Update(job);
```

## Basic Usage

Once you have an instance of the `MediaOpsPlanApi` class, you can start using its features.

### Creating Objects

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

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
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

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
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

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
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Delete a resource
api.Resources.Delete(resource.Id);

// Delete multiple resources
api.Resources.Delete(new[] { id1, id2, id3 });
```

## Next Steps

- **[Quick Reference](Quick%20Reference.md)** – Common snippets for repositories, querying, and resource management
- **[Advanced Topics](Advanced%20Topics.md)** – Job lifecycle, timing rules, configuration state, orchestration, synchronization, and logging
- **[What's New Since 1.5](What%27s%20New%20Since%201.5.md)** – What changed since 1.5.x, and what is still prerelease

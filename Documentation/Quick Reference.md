# Quick Reference

Common snippets for the public API in `Skyline.DataMiner.Solutions.MediaOps.Plan`.

## Instantiate `MediaOpsPlanApi`

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
using Skyline.DataMiner.Net;

IConnection connection = /* create or retrieve connection */;
var api = new MediaOpsPlanApi(connection);

// Or use the extension method:
var api = engine.GetMediaOpsPlanApi(); // For automation scripts
var api = protocol.GetMediaOpsPlanApi(); // For protocols
var api = gqiDms.GetMediaOpsPlanApi(); // For GQI data sources
```

## Access Repositories

Repositories are the primary way to interact with stored objects.

- CRUD operations (create, read, update, delete)
- Paged reading
- Batch operations
- State transitions

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

var resourcesRepo = api.Resources;
var poolsRepo = api.ResourcePools;
var capabilitiesRepo = api.Capabilities;
var capacitiesRepo = api.Capacities;
var configurationsRepo = api.Configurations;
var propertiesRepo = api.ResourceProperties;
var jobsRepo = api.Jobs;
var workflowsRepo = api.Workflows;
var recurringJobsRepo = api.RecurringJobs;
```

## Reading Objects

### Basic Reading

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// By ID
var resource = api.Resources.Read(id);

// Multiple by IDs
var resources = api.Resources.Read(new[] { id1, id2, id3 });

// All
var allResources = api.Resources.Read();
```

### Paged Reading

For large datasets, use paged reading to process data in batches:

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Read all resources in pages (default page size)
foreach (var page in api.Resources.ReadPaged())
{
    foreach (var resource in page)
    {
        // Logic for processing resource
    }
}

// Read with custom page size
foreach (var page in api.Resources.ReadPaged(pageSize: 50))
{
    // Logic for processing each resource in page
    foreach (var resource in page)
	{
		// Logic for processing resource
	}
}
```

### Counting

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Count all resources
var totalCount = api.Resources.Count();
```

## Create and Update Objects

### Resources

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Unmanaged resource
var resource = api.Resources.Create(new UnmanagedResource
{
    Name = "Standalone Resource",
    Concurrency = 1,
});

// Element resource
var elementResource = api.Resources.Create(new ElementResource
{
    Name = "Camera 1",
    AgentId = 1,
    ElementId = 100,
    Concurrency = 1,
});

// Service resource
var serviceResource = api.Resources.Create(new ServiceResource
{
    Name = "Encoding Service",
    AgentId = 1,
    ServiceId = 200,
});

// Virtual function resource
var vfResource = api.Resources.Create(new VirtualFunctionResource
{
    Name = "Virtual Encoder",
    AgentId = 1,
    ElementId = 100,
    FunctionId = functionGuid,
    FunctionTableIndex = "1",
});

// Update resource
resource.Name = "Updated Resource";
resource = api.Resources.Update(resource);

// Delete resource
api.Resources.Delete(resource.Id);

// Delete multiple resources
api.Resources.Delete(new[] { id1, id2, id3 });
```

### Resource Pools

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Create a resource pool
var pool = api.ResourcePools.Create(new ResourcePool
{
    Name = "Camera Pool",
});

// Add capabilities to a pool
pool.AddCapability(new CapabilitySettings(capabilityId)
    .SetDiscretes(new[] { "1080p", "4K" }));
pool = api.ResourcePools.Update(pool);

// Link resource pools
pool.AddLinkedResourcePool(new LinkedResourcePool(otherPoolId)
{
    SelectionType = ResourceSelectionType.Automatic,
});
pool = api.ResourcePools.Update(pool);

// Assign resources to a pool
api.ResourcePools.AssignResourcesToPool(pool, new[] { resource1, resource2 });

// Unassign resources from a pool
api.ResourcePools.UnassignResourcesFromPool(pool, new[] { resource1 });

// Get resources in a pool
var resourcesInPool = api.Resources.GetResourcesInPool(pool);

// Get resources in a pool filtered by state
var completeResources = api.Resources.GetResourcesInPool(pool, ResourceState.Complete);

// Check if pool has resources
bool hasResources = api.Resources.HasResources(pool);

// Count resources in pool
long count = api.Resources.ResourceCount(pool);
```

### Capabilities

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Create a capability
var capability = api.Capabilities.Create(new Capability
{
    Name = "Resolution",
}
.SetDiscretes(new[] { "720p", "1080p", "4K", "8K" }));

// Create a time-dependent capability
var timeDepCapability = api.Capabilities.Create(new Capability
{
    Name = "Scheduled Resolution",
    IsTimeDependent = true,
}
.SetDiscretes(new[] { "1080p", "4K" }));

// Update capability discretes
capability.AddDiscrete("16K");
capability = api.Capabilities.Update(capability);

// Access system capabilities
var resourceType = api.Capabilities.SystemCapabilities.ResourceType;
```

### Capacities

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Create a number capacity
var numberCapacity = api.Capacities.Create(new NumberCapacity
{
    Name = "Bandwidth",
    Units = "Mbps",
    RangeMin = 0,
    RangeMax = 10000,
    StepSize = 100,
    Decimals = 0,
});

// Create a range capacity
var rangeCapacity = api.Capacities.Create(new RangeCapacity
{
    Name = "Frequency Range",
    Units = "MHz",
    RangeMin = 0,
    RangeMax = 6000,
});
```

### Configurations

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Text configuration
var textConfig = api.Configurations.Create(new TextConfiguration
{
    Name = "Output Format",
    DefaultValue = "H.264",
});

// Number configuration
var numberConfig = api.Configurations.Create(new NumberConfiguration
{
    Name = "Bitrate",
    Units = "Mbps",
    DefaultValue = 50,
    RangeMin = 1,
    RangeMax = 100,
});

// Discrete text configuration
var discreteTextConfig = api.Configurations.Create(
    new DiscreteTextConfiguration
    {
        Name = "Color Space",
    }
    .SetDiscretes(new[]
    {
        new TextDiscreet { Value = "BT.709" },
        new TextDiscreet { Value = "BT.2020" },
    }));

// Discrete number configuration
var discreteNumberConfig = api.Configurations.Create(
    new DiscreteNumberConfiguration
    {
        Name = "Frame Rate",
    }
    .SetDiscretes(new[]
    {
        new NumberDiscreet { Value = 25 },
        new NumberDiscreet { Value = 50 },
        new NumberDiscreet { Value = 60 },
    }));
```

### Resource Properties

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Create a resource property
var property = api.ResourceProperties.Create(new ResourceProperty
{
    Name = "Location",
});
```

## Resource Settings

### Assign Capabilities to a Resource

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

var resource = api.Resources.Read(resourceId);

// Add a capability setting with discrete values
resource.AddCapability(new CapabilitySettings(capabilityId)
    .SetDiscretes(new[] { "4K", "8K" }));

// Replace all capability settings
resource.SetCapabilities(new[]
{
    new CapabilitySettings(resolution.Id).SetDiscretes(new[] { "4K" }),
    new CapabilitySettings(codec.Id).SetDiscretes(new[] { "H.265" }),
});

resource = api.Resources.Update(resource);
```

### Assign Capacities to a Resource

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Number capacity setting
resource.AddCapacity(new NumberCapacitySetting(bandwidthCapacity.Id)
{
    Value = 1000,
});

// Range capacity setting
resource.AddCapacity(new RangeCapacitySetting(frequencyCapacity.Id)
{
    MinValue = 100,
    MaxValue = 500,
});

resource = api.Resources.Update(resource);
```

### Assign Properties to a Resource

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

resource.AddProperty(new ResourcePropertySettings(locationProperty.Id)
{
    Value = "Studio A",
});

resource = api.Resources.Update(resource);
```

### Pool Management on Resources

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Assign to a pool
resource.AssignToPool(pool);
resource = api.Resources.Update(resource);

// Assign to multiple pools
resource.SetPools(new[] { pool1, pool2 });
resource = api.Resources.Update(resource);

// Unassign from a pool
resource.UnassignFromPool(pool);
resource = api.Resources.Update(resource);
```

## Resource Type Conversions

Resources can be converted between types using the resources repository:

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Convert to element resource
var elementResource = api.Resources.ConvertToElementResource(
    resource,
    new ResourceElementLinkSetting
    {
        AgentId = 1,
        ElementId = 100,
    });

// Convert to service resource
var serviceResource = api.Resources.ConvertToServiceResource(
    resource,
    new ResourceServiceLinkSetting
    {
        AgentId = 1,
        ServiceId = 200,
    });

// Convert to virtual function resource
var vfResource = api.Resources.ConvertToVirtualFunctionResource(
    resource,
    new ResourceVirtualFunctionLinkSetting
    {
        AgentId = 1,
        ElementId = 100,
        FunctionId = functionGuid,
        FunctionTableIndex = "1",
    });

// Convert to unmanaged resource
var unmanagedResource = api.Resources.ConvertToUnmanagedResource(resource);

// Safe conversions (returns false instead of throwing)
if (api.Resources.TryConvertToElementResource(resource, linkSetting, out var converted))
{
    // Conversion succeeded
}
```

## Pool Relationships

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Get pools for a resource
var pools = api.ResourcePools.GetResourcePools(resource);

// Get pools for multiple resources
var poolsPerResource = api.ResourcePools.GetPoolsPerResource(new[] { resource1, resource2 });

// Get parent pool links
var parentLinks = api.ResourcePools.GetParentPoolLinks(new[] { pool1, pool2 });
```

## Jobs and Workflows

Jobs and workflows are read-only through the API:

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Read a job
var job = api.Jobs.Read(jobId);

// Read all jobs
var allJobs = api.Jobs.Read();

// Read a workflow
var workflow = api.Workflows.Read(workflowId);

// Read all workflows
var allWorkflows = api.Workflows.Read();

// Read a recurring job
var recurringJob = api.RecurringJobs.Read(recurringJobId);

// Update job orchestration state
api.Jobs.SetOrchestrationState(jobId, new OrchestrationUpdateDetails
{
    Event = OrchestrationEventType.PrerollStart,
    EventState = OrchestrationEventState.Succeeded,
    Message = "Preroll started successfully",
});
```

## Delete with Options

### Resource Pool Delete Options

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Delete a pool with options
api.ResourcePools.Delete(pool, new ResourcePoolDeleteOptions
{
    DeleteDraftResources = true,
    DeleteDeprecatedResources = true,
});
```

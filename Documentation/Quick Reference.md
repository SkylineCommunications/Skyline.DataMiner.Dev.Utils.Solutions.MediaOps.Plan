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
var resourcePropertiesRepo = api.ResourceProperties;
var jobsRepo = api.Jobs;
var workflowsRepo = api.Workflows;
var recurringJobsRepo = api.RecurringJobs;
var propertiesRepo = api.Properties;
var schedulingPropertiesRepo = api.SchedulingProperties;
var propertySettingCollectionsRepo = api.PropertySettingCollections;
var relationshipsRepo = api.Relationships;
var relationshipObjectTypesRepo = api.RelationshipObjectTypes;
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
    ProcessBatch(page);
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

### Eligible Resources

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Get the resources that are eligible for a time range, with the required capabilities and capacities
var eligibilityResult = api.Resources.GetEligibleResources(new EligibleResourcesContext(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1))
{
    CapabilitySettings = new[] { new CapabilitySetting(capabilityId) { Value = "4K" } },
    CapacitySettings = new CapacitySetting[] { new NumberCapacitySetting(capacityId) { Value = 10 } },
});

foreach (var eligibleResource in eligibilityResult.EligibleResources)
{
    Resource resource = eligibleResource.Resource;
    int concurrencyInUse = eligibleResource.Usage.ConcurrencyConsumption;
    int concurrencyRemaining = eligibleResource.Usage.RemainingConcurrency;

    var capacityUsage = eligibleResource.Usage.CapacityUsages
        .OfType<NumberCapacityUsage>()
        .FirstOrDefault(x => x.CapacityId == capacityId);
}

// Restrict the eligible resources with a filter, for example to a specific pool.
var eligibleResourcesInPool = api.Resources.GetEligibleResources(new EligibleResourcesContext(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1))
{
    CapabilitySettings = new[] { new CapabilitySetting(capabilityId) { Value = "4K" } },
    CapacitySettings = new CapacitySetting[] { new NumberCapacitySetting(capacityId) { Value = 10 } },
    Filter = ResourceExposers.ResourcePoolIds.Contains(poolId),
});
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

## Workflows

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Create a workflow
var workflow = api.Workflows.Create(new Workflow
{
    Name = "Studio Recording",
    Priority = WorkflowPriority.Normal,
    PreRoll = TimeSpan.FromMinutes(15),
    PostRoll = TimeSpan.FromMinutes(15),
    JobTypeCategoryId = categoryId, // Optional link to a category defined in the Categories app.
});

// Read
var readWorkflow = api.Workflows.Read(workflow.Id);
var allWorkflows = api.Workflows.Read();

// Update
workflow.Description = "Weekly recording template";
workflow = api.Workflows.Update(workflow);

// Draft -> Complete. Only a completed workflow can be turned into a job.
workflow = api.Workflows.Complete(workflow);

// Deep-copy into a new, unsaved workflow.
var duplicatedWorkflow = workflow.Duplicate();
var duplicatedWorkflowWithId = workflow.Duplicate(Guid.NewGuid());

// Delete
api.Workflows.Delete(workflow.Id);
```

## Job Lifecycle

A job moves through `Draft -> Tentative -> Confirmed -> Running -> Completed`, with `Cancel` reachable from `Tentative`/`Confirmed`. See [Advanced Topics](Advanced%20Topics.md#job-lifecycle) for the full diagram and the timing rules enforced at every transition. The snippets below illustrate each transition independently; a single job only follows one path through the lifecycle.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Build a job from a completed workflow, then create it (starts in Draft).
var job = Job.FromWorkflow(api, workflow.Id);
job.Start = DateTimeOffset.UtcNow.AddHours(1);
job.End = job.Start.AddHours(2);
job.PreRollStart = job.Start - workflow.PreRoll;
job.PostRollEnd = job.End + workflow.PostRoll;
job = api.Jobs.Create(job);

// Update
job.Description = "Weekly studio recording";
job = api.Jobs.Update(job);

// Deep-copy into a new, unsaved job.
var duplicatedJob = job.Duplicate();
var duplicatedJobWithId = job.Duplicate(Guid.NewGuid());

// Draft -> Tentative -> Confirmed
job = api.Jobs.SaveAsTentative(job);
job = api.Jobs.Confirm(job);

// Confirmed -> Tentative, if you need to make changes again.
job = api.Jobs.ReturnToTentative(job);
job = api.Jobs.Confirm(job);

// Start a confirmed job manually without changing JobState itself.
// Omit the options to keep the scheduled Start; set NewStartTime to reschedule it.
job = api.Jobs.Start(job, new JobStartOptions
{
    NewStartTime = DateTimeOffset.UtcNow.AddMinutes(5),
});

// Confirmed -> Running, once pre-roll start has passed and the core reservation is running.
job = api.Jobs.TransitionToRunning(job);

// Stop a running job early. Omit the options to retain its existing post-roll where possible.
job = api.Jobs.Stop(job, new JobStopOptions
{
    NewPostRollEnd = DateTimeOffset.UtcNow.AddMinutes(10),
});

// Running -> Completed, once the core reservation has ended.
job = api.Jobs.TransitionToCompleted(job);

// Alternative paths, each starting from a different job in the indicated state:
var canceledJob = api.Jobs.Cancel(tentativeOrConfirmedJob);
var importedJob = api.Jobs.MarkAsCompleted(draftOrTentativeHistoricalJob);

// Batch variants accept a collection of jobs or job IDs and return the updated jobs.
var confirmedJobs = api.Jobs.Confirm(new[] { job1, job2 });

// Report the outcome of an orchestration event back onto the job.
api.Jobs.SetOrchestrationState(job.Id, new OrchestrationUpdateDetails
{
    Event = OrchestrationEventType.PrerollStart,
    EventState = OrchestrationEventState.Succeeded,
    Message = "Preroll started successfully",
});

// Delete
api.Jobs.Delete(job.Id, new JobDeleteOptions { ForceDelete = false });
```

### Building a Job

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// From a completed workflow: copies node graph, groups, and orchestration/property settings.
var jobFromWorkflow = Job.FromWorkflow(api, workflow.Id);

// From a recurring job occurrence: copies node graph, orchestration/property settings and relationships.
var jobFromRecurringJob = Job.FromRecurringJob(recurringJob, startTime: DateTimeOffset.UtcNow.AddDays(1));

// A key is generated automatically based on GlobalSettings.JobSettings unless you supply one explicitly.
var jobWithExplicitKey = new Job(new JobData { Key = "MyKey" });
```

## Recurring Jobs

A recurring job describes a series that follows a `RecurringPattern`, and moves through `Active -> Completed`/`Active -> Cancelled`.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Build a recurring job from an existing job (Start/Duration/pre-post-roll/priority/node graph are copied).
var recurringJob = RecurringJob.FromJob(job);
recurringJob.Pattern.RepeatType = RepeatType.Weekly;
recurringJob.Pattern.RepeatEvery = 1;
recurringJob.Pattern.WeekDays = WeekDays.Monday | WeekDays.Wednesday;
recurringJob.Pattern.EndDate = DateTimeOffset.UtcNow.AddMonths(3);
recurringJob.DesiredJobState = DesiredJobState.Tentative; // State new occurrences are created in.

recurringJob = api.RecurringJobs.Create(recurringJob);

// Read / Update
recurringJob = api.RecurringJobs.Read(recurringJob.Id);
recurringJob.Description = "Weekly studio recording series";
recurringJob = api.RecurringJobs.Update(recurringJob);

// Deep-copy into a new, unsaved recurring job.
var duplicatedRecurringJob = recurringJob.Duplicate();

// Choose one terminal transition for an active recurring job.
var completedRecurringJob = api.RecurringJobs.Complete(recurringJob);
var cancelledRecurringJob = api.RecurringJobs.Cancel(otherActiveRecurringJob);

// Reflects the state of the (external) process that keeps the series' jobs up to date with the pattern.
recurringJob = api.RecurringJobs.UpdateProcessState(recurringJob, RecurringJobProcessState.UpdatingSeries);

// Delete (no ForceDelete-style options here, unlike Job).
api.RecurringJobs.Delete(recurringJob.Id);

// Calculate occurrence dates from the pattern without creating jobs.
var nextOccurrences = recurringJob.Pattern.CalculateOccurrencesByAmount(5, recurringJob.Start, recurringJob.TimeZone);
```

## NodeGraph Operations

`Job.NodeGraph`, `Workflow.NodeGraph` and `RecurringJob.NodeGraph` are all a `NodeGraph<TNode>` (`TNode` being `JobNode`, `WorkflowNode` or `RecurringJobNode`) with the same shape.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

var cameraNode = new JobResourceNode(cameraPool, camera);
var backupCameraNode = new JobResourceNode(cameraPool, backupCamera);
var poolNode = new JobResourcePoolNode(encoderPool);

// Add nodes
job.NodeGraph.Add(cameraNode).Add(poolNode);

// Connect: a directed, data-oriented link between two nodes.
// Use Connect(cameraNode, poolNode) when no explicit configuration is needed.
job.NodeGraph.Connect(cameraNode, poolNode, new ShuffleLevelBasedConnectionConfiguration()
    .AddLevelMapping(destinationLevel: 1, sourceLevel: 10));

// Link: a parent/child hierarchy, distinct from a connection. A child has at most one parent.
job.NodeGraph.Link(parent: poolNode, child: cameraNode);
job.NodeGraph.Unlink(cameraNode);

// Group nodes for organizational purposes.
var group = job.NodeGraph.AddGroup("Studio A");
group.Add(cameraNode).Add(poolNode);
job.NodeGraph.RemoveGroup(group);

// Swap: replace a node's resource/pool assignment, keeping its connections, links and group membership.
job.NodeGraph.Swap(cameraNode, backupCameraNode);

// Inspect the graph.
var outgoing = job.NodeGraph.GetOutgoing(cameraNode);
var incoming = job.NodeGraph.GetIncoming(poolNode);
var parent = job.NodeGraph.GetParent(cameraNode);
var children = job.NodeGraph.GetChildren(poolNode);

// Remove a node and its connections/links/group memberships.
job.NodeGraph.Remove(poolNode);

job = api.Jobs.Update(job);
```

## Properties

Generic properties can be attached to jobs, workflows, recurring jobs and their nodes; this is distinct from `ResourceProperties`, which only apply to resources.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// api.Properties: explicitly select the property scope.
var boolProperty = (BooleanProperty)api.Properties.Create(
    new BooleanProperty(new PropertyData { Scope = "MediaOps" })
{
    Name = "Requires Approval",
    DefaultValue = false,
});

// api.SchedulingProperties: convenience repository for properties in the "MediaOps" scope,
// meant for jobs/workflows/recurring jobs. Scope is assigned for you.
var discreteProperty = (DiscreteProperty)api.SchedulingProperties.Create(new DiscreteProperty
{
    Name = "Priority Reason",
}
.SetDiscretes(new[] { "VIP client", "Rescheduled", "Standard" }));

// Assign values on a job/workflow/recurring job or one of their nodes.
job.AddProperty(new DiscretePropertySetting(discreteProperty) { Value = "VIP client" });
job.AddProperty(new BooleanPropertySetting(boolProperty) { Value = true });
job.SetProperties(new[] { /* ... */ });
job.RemoveProperty(job.PropertySettings.First());

// Free-form name/value pairs that are not tied to a Property definition.
job.AddCustomProperty(new CustomPropertySetting("External Reference") { Value = "PO-12345" });

job = api.Jobs.Update(job);

// Delete a property definition; ForceDelete also removes it from existing setting collections.
api.Properties.Delete(boolProperty.Id, new PropertyDeleteOptions { ForceDelete = true });
```

## File Properties

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

var attachmentProperty = (FileProperty)api.SchedulingProperties.Create(new FileProperty
{
    Name = "Run Sheet",
    HasSizeLimit = true,
    SizeLimit = 10, // MB
    AllowMultiple = false,
});

var fileSetting = new FilePropertySetting(attachmentProperty)
    .AddFile("runsheet.pdf", File.ReadAllBytes(@"C:\Files\runsheet.pdf"));

job.AddProperty(fileSetting);
job = api.Jobs.Update(job);

// Read the content back (for example, after re-reading the job).
var savedSetting = job.PropertySettings.OfType<FilePropertySetting>().Single(x => x.Id == attachmentProperty.Id);
byte[] content = savedSetting.ReadContent("runsheet.pdf");

// Remove a file, or clear all files from the setting.
fileSetting.RemoveFile("runsheet.pdf");
fileSetting.ClearFiles();
```

## Relationships

Generic parent/child links between arbitrary object types, plus dedicated endpoints on `Job`/`RecurringJob` for linking to external business objects.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Define the object types that can participate in a relationship.
var bookingType = api.RelationshipObjectTypes.Create(new RelationshipObjectType { Name = "Booking" });
var clientType = api.RelationshipObjectTypes.Create(new RelationshipObjectType { Name = "Client" });

// Generic relationship between two arbitrary objects.
var relationship = api.Relationships.Create(new Relationship(new RelationshipData
{
    Parent = new RelationshipEndpoint(clientType.Id, objectId: "CLIENT-1") { ObjectName = "Acme Corp" },
    Child = new RelationshipEndpoint(bookingType.Id, objectId: "BOOK-42") { ObjectName = "Client booking #42" },
}));

// Read / Update / Delete like any other repository object.
relationship = api.Relationships.Read(relationship.Id);
api.Relationships.Delete(relationship.Id);

// Link a job or recurring job directly to an external object.
job.AddRelationshipEndpoint(new JobRelationshipEndpoint(bookingType.Id)
{
    ObjectId = "BOOK-42",
    ObjectName = "Client booking #42",
    Url = "https://booking.example.com/BOOK-42",
});
job.SetRelationshipEndpoints(new[] { /* ... */ });
job.RemoveRelationshipEndpoint(job.RelationshipEndpoints.First());
job = api.Jobs.Update(job);
```

## Categories

`Job.JobTypeCategoryId`, `Workflow.JobTypeCategoryId`, `RecurringJob.JobTypeCategoryId` and `ResourcePool.CategoryId` are plain string IDs that link an object to a category defined in the separate Categories app/DevPack. Setting the ID keeps that link in sync automatically; **category definitions themselves are not CRUD-able through this DevPack** — manage them through the Categories app or its own DevPack.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

job.JobTypeCategoryId = categoryId;
job = api.Jobs.Update(job);
```

## Global Settings

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

var jobSettings = api.GlobalSettings.GetJobSettings();

// Controls how auto-generated Job keys look, e.g. "REC-00042".
jobSettings.KeyPrefix = "REC-";
jobSettings.KeyMinimumDigits = 5;
jobSettings.KeyStartingSeed = 1;
jobSettings.KeyIncrement = 1;

api.GlobalSettings.UpdateJobSettings(jobSettings);
```

## Querying with Exposers

Every repository's `Read(FilterElement<T>)` / `Count(FilterElement<T>)` accept filters built from that object's `*Exposers` class.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Jobs that are currently running.
var runningJobs = api.Jobs.Read(JobExposers.State.Equal(JobState.Running));

// Jobs referencing a specific resource, combined with another condition.
var jobsForResource = api.Jobs.Read(JobExposers.Resources.Contains(resourceId).AND(JobExposers.HasError.Equal(false)));

// Jobs with a specific capability value, or missing mandatory configuration state.
var byCapability = api.Jobs.Read(JobExposers.Capabilities.Discretes.Contains("4K"));
var missingConfig = api.Jobs.Read(JobExposers.ConfigurationState.Equal(ConfigurationState.MandatoryValuesMissing));

// Jobs that ran longer than an hour.
var longJobs = api.Jobs.Read(JobExposers.Duration.GreaterThan(TimeSpan.FromHours(1)));

// Errors reported on a job.
foreach (var error in job.Errors)
{
    Console.WriteLine($"{error.Code}: {error.Message}");
}

var jobsWithError = api.Jobs.Read(JobExposers.Errors.Code.Contains("JobResourceNotAvailable"));

// Workflows and recurring jobs support the same pattern.
var draftWorkflows = api.Workflows.Read(WorkflowExposers.State.Equal(WorkflowState.Draft));
var recurringForResource = api.RecurringJobs.Read(RecurringJobExposers.Resources.Contains(resourceId));
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

### Job Delete Options

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// ForceDelete bypasses the usual state restrictions on delete.
api.Jobs.Delete(job.Id, new JobDeleteOptions { ForceDelete = true });
```

## Synchronization with SRM

Resource Studio is the master: the DOM configuration is pushed to SRM. Synchronization is a two-phase operation.
The first phase only reports differences, the second phase applies the ones you select.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

// Phase 1: detect. Nothing is changed in SRM.
var report = api.ResourcePools.GetOutOfSyncItems();

// Or limit the scope to specific resource pools and the resources they contain.
report = api.ResourcePools.GetOutOfSyncItems(new[] { pool1, pool2 });

if (report.IsSynchronized)
{
    return;
}

foreach (var item in report.GetAllItems())
{
    // item.Differences holds data only. Format the message yourself.
    foreach (var difference in item.Differences)
    {
        switch (difference)
        {
            case MissingCoreObjectDifference:
                Console.WriteLine($"{item.Name} does not exist in SRM.");
                break;
            case NameDifference name:
                Console.WriteLine($"{item.Name}: name is '{name.CoreValue}' in SRM.");
                break;
            case CapacityDifference capacity:
                Console.WriteLine($"{item.Name}: capacity {capacity.CapacityId} is {capacity.Kind}.");
                break;
        }
    }

    // item.CanSynchronize is false when a blocker was found, for example a duplicate name.
    foreach (var blocker in item.Blockers)
    {
        Console.WriteLine($"{item.Name} cannot be synchronized: {blocker.ErrorMessage}");
    }
}

// Phase 2: apply the selection. Items are compared again, so only actual differences are pushed.
var result = api.ResourcePools.Synchronize(report.GetAllItems().Where(x => x.CanSynchronize));

foreach (var failure in result.Failures)
{
    Console.WriteLine($"{failure.Key}: {failure.Value}");
}
```

## Next Steps

- **[Getting Started](Getting%20Started.md)** – Installation and basic usage
- **[Advanced Topics](Advanced%20Topics.md)** – Job lifecycle, timing rules, configuration state, orchestration, synchronization, and logging
- **[What's New Since 1.5](What%27s%20New%20Since%201.5.md)** – What changed since 1.5.x, and what is still prerelease

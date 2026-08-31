namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a job in MediaOps Plan.
	/// </summary>
	public class Job : ApiNamedObject
	{
		private readonly HashSet<Guid> contactIds = [];
		private readonly Dictionary<string, JobError> errors = [];

		private StorageWorkflow.JobsInstance originalInstance;
		private StorageWorkflow.JobsInstance updatedInstance;
		private PropertySettingsContext propertiesContext;
		private PropertySettingsScope propertySettingsScope;
		private JobLinksContext jobLinksContext;
		private JobLinksScope jobLinksScope;

		/// <summary>
		/// Initializes a new instance of the <see cref="Job"/> class.
		/// </summary>
		public Job() : base()
		{
			IsNew = true;

			OrchestrationSettings = new WorkflowOrchestrationSettings();
			NodeGraph = new NodeGraph<JobNode>();
			ConfigureNodeGraphSwapHooks();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Job"/> class with a specific job ID.
		/// </summary>
		public Job(Guid jobId) : base(jobId)
		{
			IsNew = true;
			HasUserDefinedId = true;

			OrchestrationSettings = new WorkflowOrchestrationSettings();
			NodeGraph = new NodeGraph<JobNode>();
			ConfigureNodeGraphSwapHooks();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Job"/> class with the specified data.
		/// </summary>
		/// <param name="data">The data that can only be provided on creation.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when a required field of <paramref name="data"/> is not filled out.</exception>
		public Job(JobData data) : this()
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}

			data.Validate(nameof(data));

			Key = data.Key;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Job"/> class with a specific job ID and the specified data.
		/// </summary>
		/// <param name="jobId">The unique identifier of the job.</param>
		/// <param name="data">The data that can only be provided on creation.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when a required field of <paramref name="data"/> is not filled out.</exception>
		public Job(Guid jobId, JobData data) : this(jobId)
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}

			data.Validate(nameof(data));

			Key = data.Key;
		}

		internal Job(MediaOpsPlanApi planApi, StorageWorkflow.JobsInstance instance) : base(instance.ID.Id)
		{
			ParseInstance(planApi, instance);

			propertiesContext = new PropertySettingsContext(planApi, Id, NodeGraph.Nodes.Select(n => n.Id));
			foreach (var node in NodeGraph.Nodes)
			{
				node.SetPropertiesContext(propertiesContext);
			}

			jobLinksContext = new JobLinksContext(planApi, Id, () => Name);

			InitTracking();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Job"/> class as a deep copy of the specified original
		/// job, using the supplied identifier for the new job. The resulting instance is a brand new,
		/// unsaved job that shares no references with <paramref name="original"/>.
		/// </summary>
		/// <param name="original">The job to duplicate.</param>
		/// <param name="id">The unique identifier of the duplicated job.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="original"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is <see cref="Guid.Empty"/>.</exception>
		internal Job(Job original, Guid id) : base(id)
		{
			if (original == null)
			{
				throw new ArgumentNullException(nameof(original));
			}

			IsNew = true;
			HasUserDefinedId = true;

			Name = original.Name;
			Description = original.Description;
			Priority = original.Priority;
			Start = original.Start;
			End = original.End;
			PreRollStart = original.PreRollStart;
			PostRollEnd = original.PostRollEnd;
			Notes = original.Notes;
			OrganizationId = original.OrganizationId;
			OwnerId = original.OwnerId;
			JobTypeCategoryId = original.JobTypeCategoryId;

			foreach (var contactId in original.ContactIds)
			{
				contactIds.Add(contactId);
			}

			OrchestrationSettings = new WorkflowOrchestrationSettings();
			NodeGraph = new NodeGraph<JobNode>();
			ConfigureNodeGraphSwapHooks();

			// 1. Clone the node graph first so we have a complete original-node-id -> duplicated-node-id map
			//    before retargeting any DataReferences (orchestration settings may reference nodes).
			var nodeIdMap = NodeGraphCloner.Clone(original.NodeGraph, NodeGraph, CreateDuplicatedNode);

			var originalNodesById = original.NodeGraph.Nodes.ToDictionary(n => n.Id);
			var duplicatedNodesById = NodeGraph.Nodes.ToDictionary(n => n.Id);

			// 2. Copy the per-node orchestration settings, pairing each new node with its source job node.
			foreach (var entry in nodeIdMap)
			{
				var originalNode = originalNodesById[entry.Key];
				var duplicatedNode = duplicatedNodesById[entry.Value];
				OrchestrationSettingsCloner.Clone(originalNode.OrchestrationSettings, duplicatedNode.OrchestrationSettings, nodeIdMap);
			}

			// 3. Copy the job-level orchestration settings.
			OrchestrationSettingsCloner.Clone(original.OrchestrationSettings, OrchestrationSettings, nodeIdMap);

			// 4. Copy the property settings from the original job (owner) and from each original node onto the
			//    corresponding new node. The property scope copies every incoming setting into an independent
			//    instance, so the duplicate never shares references with the original job.
			foreach (var setting in original.CustomPropertySettings)
			{
				AddCustomProperty(setting);
			}

			foreach (var setting in original.PropertySettings)
			{
				AddProperty(setting);
			}

			foreach (var entry in nodeIdMap)
			{
				var originalNode = originalNodesById[entry.Key];
				var duplicatedNode = duplicatedNodesById[entry.Value];

				foreach (var setting in originalNode.CustomPropertySettings)
				{
					duplicatedNode.AddCustomProperty(setting);
				}

				foreach (var setting in originalNode.PropertySettings)
				{
					duplicatedNode.AddProperty(setting);
				}
			}

			// 5. Copy the links of the original job. The copies are new links: they get their own relationship
			//    once the duplicate is saved.
			foreach (var link in original.Links)
			{
				AddLink(new JobLink(link));
			}
		}

		/// <summary>
		/// Gets or sets the name of the job.
		/// </summary>
		public override string Name { get; set; }

		/// <summary>
		/// Gets the key of the job. The key can only be provided through a <see cref="JobData"/> instance when the job is created. If no key is provided, the system automatically assigns a generated key that cannot be modified afterwards.
		/// </summary>
		public string Key { get; private set; }

		/// <summary>
		/// Gets or sets the description of the job.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Gets or sets the priority of the job.
		/// </summary>
		public JobPriority Priority { get; set; } = JobPriority.Normal;

		/// <summary>
		/// Gets or sets the start time of the job.
		/// </summary>
		public DateTimeOffset Start { get; set; }

		/// <summary>
		/// Gets or sets the end time of the job.
		/// </summary>
		public DateTimeOffset End { get; set; }

		/// <summary>
		/// Gets the duration of the job, calculated as the difference between <see cref="End"/> and <see cref="Start"/>.
		/// </summary>
		public TimeSpan Duration => End - Start;

		/// <summary>
		/// Gets or sets the pre-roll start time of the job. This must be earlier than or equal to <see cref="Start"/>.
		/// When equal to <see cref="Start"/>, the job has no pre-roll.
		/// </summary>
		public DateTimeOffset PreRollStart { get; set; }

		/// <summary>
		/// Gets or sets the post-roll end time of the job. This must be later than or equal to <see cref="End"/>.
		/// When equal to <see cref="End"/>, the job has no post-roll.
		/// </summary>
		public DateTimeOffset PostRollEnd { get; set; }

		/// <summary>
		/// Gets the pre-roll duration of the job, calculated as the difference between <see cref="Start"/> and <see cref="PreRollStart"/>.
		/// </summary>
		public TimeSpan PreRollDuration => Start - PreRollStart;

		/// <summary>
		/// Gets the post-roll duration of the job, calculated as the difference between <see cref="PostRollEnd"/> and <see cref="End"/>.
		/// </summary>
		public TimeSpan PostRollDuration => PostRollEnd - End;

		/// <summary>
		/// Gets or sets the notes or additional information.
		/// </summary>
		public string Notes { get; set; }

		/// <summary>
		/// Gets the state of the job.
		/// </summary>
		public JobState State { get; private set; }

		/// <summary>
		/// Gets the orchestration settings assigned to this job.
		/// </summary>
		public OrchestrationSettings OrchestrationSettings { get; private set; }

		/// <summary>
		/// Gets the node graph containing all nodes and connections that define the job structure.
		/// </summary>
		public NodeGraph<JobNode> NodeGraph { get; private set; }

		/// <summary>
		/// Gets the custom property settings associated with this job.
		/// Property settings are loaded lazily in a single batch together with the property settings of all nodes.
		/// Use <see cref="AddCustomProperty"/>, <see cref="SetCustomProperties"/> and <see cref="RemoveCustomProperty"/> to modify them.
		/// </summary>
		public IReadOnlyCollection<CustomPropertySetting> CustomPropertySettings => GetOrCreateScope().CustomPropertySettings;

		/// <summary>
		/// Gets the property settings associated with this job.
		/// Property settings are loaded lazily in a single batch together with the property settings of all nodes.
		/// Use <see cref="AddProperty"/>, <see cref="SetProperties"/> and <see cref="RemoveProperty"/> to modify them.
		/// </summary>
		public IReadOnlyCollection<PropertySetting> PropertySettings => GetOrCreateScope().PropertySettings;

		/// <summary>
		/// Gets the objects that are linked to this job. Links are loaded lazily.
		/// Use <see cref="AddLink"/>, <see cref="SetLinks"/> and <see cref="RemoveLink"/> to modify them.
		/// </summary>
		public IReadOnlyCollection<JobLink> Links => GetOrCreateLinksScope().Links;

		/// <summary>
		/// Gets or sets the ID of the organization associated with the job.
		/// </summary>
		public Guid OrganizationId { get; set; }

		/// <summary>
		/// Gets or sets the ID of the owner of the job.
		/// </summary>
		public Guid OwnerId { get; set; }

		/// <summary>
		/// Gets the collection of contact IDs associated with the job.
		/// </summary>
		public IReadOnlyCollection<Guid> ContactIds => contactIds;

		/// <summary>
		/// Gets the collection of errors reported on the job.
		/// </summary>
		public IReadOnlyCollection<JobError> Errors => errors.Values;

		/// <summary>
		/// Gets or sets the unique identifier of the associated job type category.
		/// </summary>
		public string JobTypeCategoryId { get; set; }

		/// <summary>
		/// Gets or sets the unique identifier of the recurring job that generated this job, if applicable.
		/// </summary>
		public Guid RecurringJobId { get; set; }

		/// <summary>
		/// Gets a value indicating whether manual actions are required to complete the job. This property is set by the system and cannot be modified directly.
		/// For example, when mandatory orchestration settings are missing, this property is set to <c>true</c>, indicating that the job cannot be confirmed
		/// until those values are provided.
		/// </summary>
		/// <remarks>
		/// This value does not depict the actual state of the job: it is only recalculated and stored when the job is created or updated.
		/// </remarks>
		public bool ActionRequired { get; internal set; }

		/// <summary>
		/// Gets the configuration state of the <see cref="OrchestrationSettings"/> of the job itself. The orchestration settings
		/// of the nodes of the job are not taken into account: those are exposed by <see cref="JobNode.ConfigurationState"/>.
		/// This property is set by the system and cannot be modified directly.
		/// </summary>
		/// <remarks>
		/// This value does not depict the actual state of the job: it is only recalculated and stored when the job is created or updated.
		/// </remarks>
		public ConfigurationState ConfigurationState { get; internal set; }

		internal StorageWorkflow.JobsInstance OriginalInstance => originalInstance;

		internal PropertySettingsScope PropertySettingsScope => propertySettingsScope;

		internal PropertySettingsContext PropertySettingsContext => propertiesContext;

		internal JobLinksScope JobLinksScope => jobLinksScope;

		internal JobLinksContext JobLinksContext => jobLinksContext;

		/// <summary>
		/// Creates a duplicate of this job with a newly generated identifier. The duplicate is a brand new,
		/// unsaved job instance without any ties to the original: all properties, orchestration settings,
		/// nodes, connections, links and property settings are deep copied.
		/// </summary>
		/// <returns>A new <see cref="Job"/> instance that is a deep copy of the current job.</returns>
		public Job Duplicate()
		{
			return new Job(this, Guid.NewGuid());
		}

		/// <summary>
		/// Creates a duplicate of this job with the specified identifier. The duplicate is a brand new,
		/// unsaved job instance without any ties to the original: all properties, orchestration settings,
		/// nodes, connections, links and property settings are deep copied.
		/// </summary>
		/// <param name="id">The unique identifier of the duplicated job.</param>
		/// <returns>A new <see cref="Job"/> instance that is a deep copy of the current job.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is <see cref="Guid.Empty"/>.</exception>
		public Job Duplicate(Guid id)
		{
			return new Job(this, id);
		}

		/// <summary>
		/// Adds a custom property setting to this job.
		/// </summary>
		/// <param name="setting">The custom property setting to add.</param>
		/// <returns>The current <see cref="Job"/> instance.</returns>
		public Job AddCustomProperty(CustomPropertySetting setting)
		{
			GetOrCreateScope().AddCustomProperty(setting);
			return this;
		}

		/// <summary>
		/// Replaces the entire collection of custom property settings associated with this job with the specified settings.
		/// </summary>
		/// <param name="settings">The custom property settings that should replace the current collection.</param>
		/// <returns>The current <see cref="Job"/> instance.</returns>
		public Job SetCustomProperties(IEnumerable<CustomPropertySetting> settings)
		{
			GetOrCreateScope().SetCustomProperties(settings);
			return this;
		}

		/// <summary>
		/// Removes the specified custom property setting from this job.
		/// </summary>
		/// <param name="setting">The custom property setting to remove.</param>
		/// <returns>The current <see cref="Job"/> instance.</returns>
		public Job RemoveCustomProperty(CustomPropertySetting setting)
		{
			GetOrCreateScope().RemoveCustomProperty(setting);
			return this;
		}

		/// <summary>
		/// Adds a property setting to this job.
		/// </summary>
		/// <param name="setting">The property setting to add.</param>
		/// <returns>The current <see cref="Job"/> instance.</returns>
		public Job AddProperty(PropertySetting setting)
		{
			GetOrCreateScope().AddProperty(setting);
			return this;
		}

		/// <summary>
		/// Replaces the entire collection of property settings associated with this job with the specified settings.
		/// </summary>
		/// <param name="settings">The property settings that should replace the current collection.</param>
		/// <returns>The current <see cref="Job"/> instance.</returns>
		public Job SetProperties(IEnumerable<PropertySetting> settings)
		{
			GetOrCreateScope().SetProperties(settings);
			return this;
		}

		/// <summary>
		/// Removes the specified property setting from this job.
		/// </summary>
		/// <param name="setting">The property setting to remove.</param>
		/// <returns>The current <see cref="Job"/> instance.</returns>
		public Job RemoveProperty(PropertySetting setting)
		{
			GetOrCreateScope().RemoveProperty(setting);
			return this;
		}

		/// <summary>
		/// Links an object to this job. When a link to the same object already exists, its name and URL are updated instead.
		/// </summary>
		/// <param name="link">The link to add.</param>
		/// <returns>The current <see cref="Job"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="link"/> is <see langword="null"/>.</exception>
		public Job AddLink(JobLink link)
		{
			GetOrCreateLinksScope().AddLink(link);
			return this;
		}

		/// <summary>
		/// Replaces the entire collection of links associated with this job with the specified links.
		/// </summary>
		/// <param name="links">The links that should replace the current collection.</param>
		/// <returns>The current <see cref="Job"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="links"/> is <see langword="null"/>.</exception>
		public Job SetLinks(IEnumerable<JobLink> links)
		{
			GetOrCreateLinksScope().SetLinks(links);
			return this;
		}

		/// <summary>
		/// Removes the link to the object described by the specified <see cref="JobLink"/> from this job.
		/// </summary>
		/// <param name="link">The link to remove.</param>
		/// <returns>The current <see cref="Job"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="link"/> is <see langword="null"/>.</exception>
		public Job RemoveLink(JobLink link)
		{
			GetOrCreateLinksScope().RemoveLink(link);
			return this;
		}

		private PropertySettingsScope GetOrCreateScope()
			=> propertySettingsScope ??= EnsureContext().CreateOwnerScope();

		private JobLinksScope GetOrCreateLinksScope()
			=> jobLinksScope ??= EnsureLinksContext().CreateOwnerScope();

		internal JobLinksContext EnsureLinksContext()
		{
			// New, unsaved job: a null planApi is fine because the lazy load will only ever return empty results.
			return jobLinksContext ??= new JobLinksContext(null, Id, () => Name);
		}

		internal PropertySettingsContext EnsureContext()
		{
			if (propertiesContext == null)
			{
				// New, unsaved job: no backend data to load. A null planApi is fine because the
				// lazy load will only ever return empty results for owner+nodes.
				propertiesContext = new PropertySettingsContext(null, Id, NodeGraph.Nodes.Select(n => n.Id));
			}

			// Always (re)wire every node currently in the graph so nodes added after the context was
			// first created still pick up the correct LinkedObjectId when their scope is persisted.
			foreach (var node in NodeGraph.Nodes)
			{
				node.SetPropertiesContext(propertiesContext);
			}

			return propertiesContext;
		}

		/// <summary>
		/// Builds a new <see cref="Job"/> from the specified <see cref="Workflow"/>.
		/// </summary>
		/// <param name="api">The <see cref="IMediaOpsPlanApi"/> instance used to interact with the API.</param>
		/// <param name="workflow">The <see cref="Workflow"/> from which to build the job.</param>
		/// <returns>A <see cref="Job"/> based on the specified workflow.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="api"/> or <paramref name="workflow"/> is <see langword="null"/>.</exception>
		public static Job FromWorkflow(IMediaOpsPlanApi api, Workflow workflow)
		{
			if (api == null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			if (workflow == null)
			{
				throw new ArgumentNullException(nameof(workflow));
			}

			return FromWorkflow(api, workflow.Id);
		}

		/// <summary>
		/// Builds a new <see cref="Job"/> from the workflow with the specified ID.
		/// </summary>
		/// <param name="api">The <see cref="IMediaOpsPlanApi"/> instance used to interact with the API.</param>
		/// <param name="workflowId">The unique identifier of the workflow from which to build the job.</param>
		/// <returns>A <see cref="Job"/> based on the specified workflow.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="api"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="workflowId"/> is <see cref="Guid.Empty"/>.</exception>
		/// <exception cref="MediaOpsException">Thrown when no workflow with the specified <paramref name="workflowId"/> is found.</exception>
		public static Job FromWorkflow(IMediaOpsPlanApi api, Guid workflowId)
		{
			if (api == null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			if (workflowId == Guid.Empty)
			{
				throw new ArgumentException(nameof(workflowId));
			}

			var workflow = api.Workflows.Read(workflowId);
			if (workflow == null)
			{
				var error = new WorkflowNotFoundError
				{
					ErrorMessage = $"Workflow with ID {workflowId} was not found.",
					Id = workflowId,
				};

				throw new MediaOpsException(error);
			}
			else if (workflow.State != WorkflowState.Complete)
			{
				var error = new WorkflowInvalidStateError
				{
					ErrorMessage = "Not allowed to build a job from a workflow that is not in Complete state.",
					Id = workflowId,
				};

				throw new MediaOpsException(error);
			}

			var job = new Job
			{
				Priority = EnumExtensions.MapEnum<WorkflowPriority, JobPriority>(workflow.Priority),
			};

			// 1. Clone the node graph first so we have a complete workflow-node-id -> job-node-id map
			//    before retargeting any DataReferences (orchestration settings may reference nodes).
			var nodeIdMap = NodeGraphCloner.Clone(workflow.NodeGraph, job.NodeGraph, CreateJobNode);

			// 2. On top of the groups copied from the workflow, the job gets a group holding every node it originates
			//    from. Group names are not unique, so a workflow group with the same name is left as is.
			var workflowGroup = job.NodeGraph.AddGroup(workflow.Name);
			foreach (var jobNode in job.NodeGraph.Nodes)
			{
				workflowGroup.Add(jobNode);
			}

			// 3. Copy the per-node orchestration settings, pairing each new job node with its source workflow node.
			var workflowNodesById = workflow.NodeGraph.Nodes.ToDictionary(n => n.Id);
			var jobNodesById = job.NodeGraph.Nodes.ToDictionary(n => n.Id);
			foreach (var entry in nodeIdMap)
			{
				var workflowNode = workflowNodesById[entry.Key];
				var jobNode = jobNodesById[entry.Value];
				OrchestrationSettingsCloner.Clone(workflowNode.OrchestrationSettings, jobNode.OrchestrationSettings, nodeIdMap);
			}

			// 4. Copy the job-level orchestration settings.
			OrchestrationSettingsCloner.Clone(workflow.OrchestrationSettings, job.OrchestrationSettings, nodeIdMap);

			// 5. Copy the property settings from the workflow (owner) and from each workflow node onto the
			//    corresponding job node. The property scope copies every incoming setting into an independent
			//    instance, so the job never shares references with the source workflow.
			foreach (var setting in workflow.CustomPropertySettings)
			{
				job.AddCustomProperty(setting);
			}

			foreach (var setting in workflow.PropertySettings)
			{
				job.AddProperty(setting);
			}

			foreach (var entry in nodeIdMap)
			{
				var workflowNode = workflowNodesById[entry.Key];
				var jobNode = jobNodesById[entry.Value];

				foreach (var setting in workflowNode.CustomPropertySettings)
				{
					jobNode.AddCustomProperty(setting);
				}

				foreach (var setting in workflowNode.PropertySettings)
				{
					jobNode.AddProperty(setting);
				}
			}

			return job;
		}

		/// <summary>
		/// Creates a new job based on the specified recurring job.
		/// </summary>
		/// <param name="recurringJob">The recurring job to copy.</param>
		/// <param name="startTime">The start time of the generated job.</param>
		/// <returns>A new job containing the recurring job configuration.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="recurringJob"/> is <see langword="null"/>.
		/// </exception>
		public static Job FromRecurringJob(RecurringJob recurringJob, DateTimeOffset startTime)
		{
			if (recurringJob == null)
			{
				throw new ArgumentNullException(nameof(recurringJob));
			}

			var endTime = startTime + recurringJob.Duration;

			var job = new Job
			{
				Name = recurringJob.Name,
				Description = recurringJob.Description,
				Notes = recurringJob.Notes,
				Start = startTime,
				End = endTime,
				PreRollStart = startTime - recurringJob.PreRollDuration,
				PostRollEnd = endTime + recurringJob.PostRollDuration,
				Priority = EnumExtensions.MapEnum<RecurringJobPriority, JobPriority>(recurringJob.Priority),
				OrganizationId = recurringJob.OrganizationId,
				OwnerId = recurringJob.OwnerId,
				JobTypeCategoryId = recurringJob.JobTypeCategoryId,
			};

			foreach (var contactId in recurringJob.ContactIds)
			{
				job.AddContact(contactId);
			}

			// Clone the graph first to establish the recurring-node-to-job-node ID mapping.
			var nodeIdMap = NodeGraphCloner.Clone(
				recurringJob.NodeGraph,
				job.NodeGraph,
				CreateJobNode);

			var recurringNodesById = recurringJob.NodeGraph.Nodes.ToDictionary(node => node.Id);
			var jobNodesById = job.NodeGraph.Nodes.ToDictionary(node => node.Id);

			// Clone node-level orchestration settings.
			foreach (var entry in nodeIdMap)
			{
				var recurringNode = recurringNodesById[entry.Key];
				var jobNode = jobNodesById[entry.Value];

				OrchestrationSettingsCloner.Clone(
					recurringNode.OrchestrationSettings,
					jobNode.OrchestrationSettings,
					nodeIdMap);
			}

			// Clone job-level orchestration settings.
			OrchestrationSettingsCloner.Clone(
				recurringJob.OrchestrationSettings,
				job.OrchestrationSettings,
				nodeIdMap);

			// Clone recurring-job-level property settings.
			foreach (var setting in recurringJob.CustomPropertySettings)
			{
				job.AddCustomProperty(setting);
			}

			foreach (var setting in recurringJob.PropertySettings)
			{
				job.AddProperty(setting);
			}

			// Clone node-level property settings.
			foreach (var entry in nodeIdMap)
			{
				var recurringNode = recurringNodesById[entry.Key];
				var jobNode = jobNodesById[entry.Value];

				foreach (var setting in recurringNode.CustomPropertySettings)
				{
					jobNode.AddCustomProperty(setting);
				}

				foreach (var setting in recurringNode.PropertySettings)
				{
					jobNode.AddProperty(setting);
				}
			}

			// TODO: linked items (=relationships) are not yet being taken over

			return job;
		}

		/// <summary>
		/// Produces the <see cref="JobNode"/> that should replace the given <see cref="WorkflowNode"/> inside the
		/// cloned graph. This is the only piece of "workflow ? job" specific knowledge that <see cref="FromWorkflow(IMediaOpsPlanApi, Workflow)"/>
		/// contributes; the generic cloning and reference retargeting is performed by <see cref="NodeGraphCloner"/>
		/// and <see cref="OrchestrationSettingsCloner"/>.
		/// </summary>
		private static JobNode CreateJobNode(WorkflowNode workflowNode)
		{
			return workflowNode switch
			{
				WorkflowResourceNode resourceNode => new JobResourceNode(resourceNode.ResourcePoolId, resourceNode.ResourceId)
				{
					Alias = resourceNode.Alias,
					IconImage = resourceNode.IconImage,
				},
				WorkflowResourcePoolNode resourcePoolNode => new JobResourcePoolNode(resourcePoolNode.ResourcePoolId)
				{
					Alias = resourcePoolNode.Alias,
					IconImage = resourcePoolNode.IconImage,
				},
				_ => null,
			};
		}

		private static JobNode CreateJobNode(RecurringJobNode recurringJobNode)
		{
			return recurringJobNode switch
			{
				RecurringJobResourceNode resourceNode => new JobResourceNode(
					resourceNode.ResourcePoolId,
					resourceNode.ResourceId)
				{
					Alias = resourceNode.Alias,
					IconImage = resourceNode.IconImage,
				},
				RecurringJobResourcePoolNode resourcePoolNode => new JobResourcePoolNode(
					resourcePoolNode.ResourcePoolId)
				{
					Alias = resourcePoolNode.Alias,
					IconImage = resourcePoolNode.IconImage,
				},
				_ => null,
			};
		}

		/// <summary>
		/// Produces the <see cref="JobNode"/> that should replace the given source <see cref="JobNode"/>
		/// inside the duplicated graph. The generic cloning and reference retargeting is performed by
		/// <see cref="NodeGraphCloner"/> and <see cref="OrchestrationSettingsCloner"/>.
		/// </summary>
		private static JobNode CreateDuplicatedNode(JobNode source)
		{
			return source switch
			{
				JobResourceNode resourceNode => new JobResourceNode(resourceNode.ResourcePoolId, resourceNode.ResourceId)
				{
					Alias = resourceNode.Alias,
					IconImage = resourceNode.IconImage,
				},
				JobResourcePoolNode resourcePoolNode => new JobResourcePoolNode(resourcePoolNode.ResourcePoolId)
				{
					Alias = resourcePoolNode.Alias,
					IconImage = resourcePoolNode.IconImage,
				},
				_ => null,
			};
		}

		/// <summary>
		/// Assigns a resource to every node of the job that only has a resource pool assigned. For each such node the
		/// eligible resources of its pool are requested for the time range of that node, and the first eligible resource
		/// is assigned by swapping the resource pool node for a resource node that keeps the alias, icon, orchestration
		/// settings and property settings of the original node.
		/// </summary>
		/// <remarks>
		/// Resources that are already assigned to another node of this job are excluded from the lookup, so the same
		/// resource is never assigned twice within the same job. A node for which no eligible resource is found keeps
		/// its resource pool node. The changes are only applied in memory; the job must still be saved to persist them.
		/// </remarks>
		/// <param name="api">The <see cref="IMediaOpsPlanApi"/> instance used to look up the eligible resources.</param>
		/// <returns>The current <see cref="Job"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="api"/> is <see langword="null"/>.</exception>
		/// <exception cref="MediaOpsException">Thrown when the time range of a node that needs a resource is not valid.</exception>
		public Job AssignEligibleResources(IMediaOpsPlanApi api)
		{
			if (api == null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			var assignedResourceIds = new HashSet<Guid>(
				NodeGraph.Nodes.OfType<JobResourceNode>().Select(node => node.ResourceId).Where(id => id != Guid.Empty));

			foreach (var poolNode in NodeGraph.Nodes.OfType<JobResourcePoolNode>().ToList())
			{
				GetNodeTimeRange(poolNode, out var start, out var end);

				var eligibleResources = api.Resources.GetEligibleResources(new EligibleResourcesContext(start, end)
				{
					CapabilitySettings = poolNode.OrchestrationSettings.Capabilities,
					CapacitySettings = poolNode.OrchestrationSettings.Capacities,
					Filter = CreateEligibleResourceFilter(poolNode.ResourcePoolId, assignedResourceIds),
				});

				// The already assigned resources are excluded through the filter, so any returned resource can be used.
				// A node for which no resource is eligible keeps its resource pool node so it can be assigned later.
				var resource = eligibleResources?.FirstOrDefault(x => x != null);
				if (resource == null)
				{
					continue;
				}

				var resourceNode = new JobResourceNode(poolNode.ResourcePoolId, resource.Id)
				{
					Alias = poolNode.Alias,
					IconImage = poolNode.IconImage,
				};

				resourceNode.CopyOrchestrationSettingsFrom(poolNode);
				resourceNode.CopyPropertiesFrom(poolNode);

				NodeGraph.Swap(poolNode, resourceNode);

				assignedResourceIds.Add(resource.Id);
			}

			return this;
		}

		/// <summary>
		/// Creates the filter that restricts the eligible resources of a node to the resources of its resource pool,
		/// excluding the resources that are already assigned to another node of the job.
		/// </summary>
		private static FilterElement<Resource> CreateEligibleResourceFilter(Guid resourcePoolId, IReadOnlyCollection<Guid> excludedResourceIds)
		{
			var poolFilter = ResourceExposers.ResourcePoolIds.Contains(resourcePoolId);
			if (excludedResourceIds.Count == 0)
			{
				return poolFilter;
			}

			var subFilters = new List<FilterElement<Resource>> { poolFilter };
			subFilters.AddRange(excludedResourceIds.Select(resourceId => ResourceExposers.Id.NotEqual(resourceId)));

			return new ANDFilterElement<Resource>(subFilters.ToArray());
		}

		/// <summary>
		/// Resolves the time range for which a resource must be available to be assignable to the specified node. Node
		/// timings are only resolved when the job is saved, so a node without a time range falls back to the full
		/// pre-roll to post-roll window of the job, which is the window such a node receives for a job that has not
		/// started yet.
		/// </summary>
		private void GetNodeTimeRange(JobNode node, out DateTimeOffset start, out DateTimeOffset end)
		{
			if (node.End > node.Start)
			{
				start = node.Start;
				end = node.End;
			}
			else
			{
				start = PreRollStart;
				end = PostRollEnd;
			}

			if (end <= start)
			{
				throw new MediaOpsException(new JobInvalidTimingError
				{
					ErrorMessage = $"Cannot determine the time range of node with ID '{node.Id}' because the job has no valid timings.",
					Id = Id,
					Start = start,
					End = end,
				});
			}
		}

		/// <summary>
		/// Adds a contact to the job.
		/// </summary>
		/// <param name="contactId">The unique identifier of the contact to add.</param>
		/// <returns>The current <see cref="Job"/> instance.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="contactId"/> is <see cref="Guid.Empty"/>.</exception>
		public Job AddContact(Guid contactId)
		{
			if (contactId == Guid.Empty)
			{
				throw new ArgumentException(nameof(contactId));
			}

			contactIds.Add(contactId);
			return this;
		}

		/// <summary>
		/// Removes a contact from the job.
		/// </summary>
		/// <param name="contactId">The unique identifier of the contact to remove.</param>
		/// <returns>The current <see cref="Job"/> instance.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="contactId"/> is <see cref="Guid.Empty"/>.</exception>
		public Job RemoveContact(Guid contactId)
		{
			if (contactId == Guid.Empty)
			{
				throw new ArgumentException(nameof(contactId));
			}

			contactIds.Remove(contactId);
			return this;
		}

		/// <summary>
		/// Adds an error to the job. When an error with the same error code is already present, its message is updated.
		/// </summary>
		/// <param name="error">The error to add.</param>
		/// <returns>The current <see cref="Job"/> instance.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="error"/> is <see langword="null"/>.</exception>
		public Job AddError(JobError error)
		{
			if (error == null)
			{
				throw new ArgumentNullException(nameof(error));
			}

			errors[error.Code] = error;
			return this;
		}

		/// <summary>
		/// Removes the error with the specified error code from the job.
		/// </summary>
		/// <param name="errorCode">The code that identifies the error to remove.</param>
		/// <returns>The current <see cref="Job"/> instance.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="errorCode"/> is <see langword="null"/> or whitespace.</exception>
		public Job RemoveError(string errorCode)
		{
			if (string.IsNullOrWhiteSpace(errorCode))
			{
				throw new ArgumentException("Error code cannot be null or whitespace.", nameof(errorCode));
			}

			errors.Remove(errorCode);
			return this;
		}

		/// <summary>
		/// Removes the specified error from the job.
		/// </summary>
		/// <param name="error">The error to remove.</param>
		/// <returns>The current <see cref="Job"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is <see langword="null"/>.</exception>
		public Job RemoveError(JobError error)
		{
			if (error == null)
			{
				throw new ArgumentNullException(nameof(error));
			}

			return RemoveError(error.Code);
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + Id.GetHashCode();
				hash = (hash * 23) + (Key != null ? Key.GetHashCode() : 0);
				hash = (hash * 23) + (Name != null ? Name.GetHashCode() : 0);
				hash = (hash * 23) + (Description != null ? Description.GetHashCode() : 0);
				hash = (hash * 23) + Priority.GetHashCode();
				hash = (hash * 23) + Start.GetHashCode();
				hash = (hash * 23) + End.GetHashCode();
				hash = (hash * 23) + PreRollStart.GetHashCode();
				hash = (hash * 23) + PostRollEnd.GetHashCode();
				hash = (hash * 23) + (Notes != null ? Notes.GetHashCode() : 0);
				hash = (hash * 23) + (JobTypeCategoryId != null ? JobTypeCategoryId.GetHashCode() : 0);
				hash = (hash * 23) + (OrchestrationSettings != null ? OrchestrationSettings.GetHashCode() : 0);
				hash = (hash * 23) + (NodeGraph != null ? NodeGraph.GetHashCode() : 0);
				hash = (hash * 23) + State.GetHashCode();
				hash = (hash * 23) + OrganizationId.GetHashCode();
				hash = (hash * 23) + OwnerId.GetHashCode();
				hash = (hash * 23) + RecurringJobId.GetHashCode();

				foreach (var contactId in contactIds.OrderBy(x => x).ToArray())
				{
					hash = (hash * 23) + contactId.GetHashCode();
				}

				foreach (var error in errors.Values.OrderBy(x => x.Code, StringComparer.Ordinal).ToArray())
				{
					hash = (hash * 23) + error.GetHashCode();
				}

				return hash;
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not Job other)
			{
				return false;
			}

			return Id == other.Id &&
				   Name == other.Name &&
				   Key == other.Key &&
				   Description == other.Description &&
				   Priority == other.Priority &&
				   Start == other.Start &&
				   End == other.End &&
				   PreRollStart == other.PreRollStart &&
				   PostRollEnd == other.PostRollEnd &&
				   Notes == other.Notes &&
				   JobTypeCategoryId == other.JobTypeCategoryId &&
				   OrchestrationSettings == other.OrchestrationSettings &&
				   NodeGraph == other.NodeGraph &&
				   State == other.State &&
				   OrganizationId == other.OrganizationId &&
				   OwnerId == other.OwnerId &&
				   RecurringJobId == other.RecurringJobId &&
				   contactIds.SetEquals(other.contactIds) &&
				   ErrorsEqual(other);
		}

		private bool ErrorsEqual(Job other)
		{
			if (errors.Count != other.errors.Count)
			{
				return false;
			}

			foreach (var error in errors)
			{
				if (!other.errors.TryGetValue(error.Key, out var otherError) || !error.Value.Equals(otherError))
				{
					return false;
				}
			}

			return true;
		}

		internal StorageWorkflow.JobsInstance GetInstanceWithChanges()
		{
			if (updatedInstance == null)
			{
				updatedInstance = IsNew ? new StorageWorkflow.JobsInstance(Id) : originalInstance.Clone();
			}

			updatedInstance.JobInfo.JobName = Name;
			updatedInstance.JobInfo.JobID = Key;
			updatedInstance.JobInfo.JobDescription = Description;
			updatedInstance.JobInfo.JobStart = Start.UtcDateTime;
			updatedInstance.JobInfo.JobEnd = End.UtcDateTime;
			updatedInstance.JobInfo.JobDuration = Duration;
			updatedInstance.JobInfo.Preroll = PreRollStart.UtcDateTime;
			updatedInstance.JobInfo.Postroll = PostRollEnd.UtcDateTime;
			updatedInstance.JobInfo.JobNotes = Notes;
			updatedInstance.JobInfo.JobSeriesID = RecurringJobId != Guid.Empty ? RecurringJobId.ToString() : null;
			updatedInstance.JobInfo.ActionNeeded = ActionRequired;

			// Reusing JobSource field to store the job type category ID to be backwards compatible with existing implementations.
			updatedInstance.JobInfo.JobSource = JobTypeCategoryId;

			updatedInstance.JobExecution.JobConfiguration = OrchestrationSettings.Id;

			updatedInstance.JobExecution.JobConfigurationStatus = EnumExtensions.TryMapEnum<ConfigurationState, StorageWorkflow.SlcWorkflowIds.Enums.Jobconfigurationstatus>(ConfigurationState, out var storedConfigurationState)
				? storedConfigurationState
				: (StorageWorkflow.SlcWorkflowIds.Enums.Jobconfigurationstatus?)null;

			updatedInstance.JobInfo.JobPriority = EnumExtensions.MapEnum<JobPriority, StorageWorkflow.SlcWorkflowIds.Enums.Jobpriority>(Priority);

			updatedInstance.CostingAndBilling.Organization = OrganizationId != Guid.Empty ? OrganizationId : null;
			updatedInstance.CostingAndBilling.JobOwner = OwnerId != Guid.Empty ? OwnerId : null;

			updatedInstance.CostingAndBilling.AdditionalContacts.Clear();
			foreach (var contactId in ContactIds)
			{
				updatedInstance.CostingAndBilling.AdditionalContacts.Add(contactId);
			}

			updatedInstance.Errors.Clear();
			foreach (var error in Errors)
			{
				updatedInstance.Errors.Add(new StorageWorkflow.ErrorsSection
				{
					ErrorCode = error.Code,
					ErrorMessage = error.Message,
				});
			}

			// Guarantee every job node has a unique core reservation node ID before persisting, including
			// nodes loaded from storage that bypass NodeGraph.Add (e.g. legacy nodes without an assigned ID).
			NodeGraph.EnsureCoreReservationNodeIds();

			updatedInstance.Nodes.Clear();
			foreach (var node in NodeGraph.Nodes)
			{
				updatedInstance.Nodes.Add(node.GetSectionWithChanges());
			}

			updatedInstance.Connections.Clear();
			foreach (var connection in NodeGraph.Connections)
			{
				updatedInstance.Connections.Add(connection.GetSectionWithChanges());
			}

			updatedInstance.NodeRelationships.Clear();
			foreach (var link in NodeGraph.Links)
			{
				updatedInstance.NodeRelationships.Add(new StorageWorkflow.NodeRelationshipsSection
				{
					ParentNodeID = link.Value.Id,
					ChildNodeID = link.Key.Id,
				});
			}

			updatedInstance.NodeGroups.Clear();
			foreach (var group in NodeGraph.Groups)
			{
				var groupSection = new StorageWorkflow.NodeGroupsSection
				{
					GroupName = group.Name,
				};

				foreach (var node in group.Nodes)
				{
					groupSection.GroupNodeIds.Add(node.Id);
				}

				updatedInstance.NodeGroups.Add(groupSection);
			}

			return updatedInstance;
		}

		internal void AssignKey(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
			}

			if (!IsNew)
			{
				throw new InvalidOperationException("Key can only be assigned to new jobs.");
			}

			if (!string.IsNullOrEmpty(Key))
			{
				throw new InvalidOperationException("Key has already been assigned and cannot be modified.");
			}

			Key = key;
		}

		private void ParseInstance(MediaOpsPlanApi planApi, StorageWorkflow.JobsInstance instance)
		{
			this.originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

			Name = instance.JobInfo.JobName;
			Key = instance.JobInfo.JobID;
			Description = instance.JobInfo.JobDescription;
			Start = instance.JobInfo.JobStart.Value;
			End = instance.JobInfo.JobEnd.Value;
			PreRollStart = instance.JobInfo.Preroll.HasValue ? instance.JobInfo.Preroll.Value : Start;
			PostRollEnd = instance.JobInfo.Postroll.HasValue ? instance.JobInfo.Postroll.Value : End;
			Notes = instance.JobInfo.JobNotes;
			RecurringJobId = Guid.TryParse(instance.JobInfo.JobSeriesID, out var recurringJobId) ? recurringJobId : Guid.Empty;
			ActionRequired = instance.JobInfo.ActionNeeded ?? false;

			ConfigurationState = instance.JobExecution.JobConfigurationStatus.HasValue
				? EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Enums.Jobconfigurationstatus, ConfigurationState>(instance.JobExecution.JobConfigurationStatus.Value)
				: ConfigurationState.Unknown;

			// Reusing JobSource field to store the job type category ID to be backwards compatible with existing implementations.
			JobTypeCategoryId = instance.JobInfo.JobSource;

			Priority = instance.JobInfo.JobPriority.HasValue
				? EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Enums.Jobpriority, JobPriority>(instance.JobInfo.JobPriority.Value)
				: JobPriority.Normal;
			State = EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Behaviors.Job_Behavior.StatusesEnum, JobState>(instance.Status);

			OrganizationId = instance.CostingAndBilling.Organization ?? Guid.Empty;
			OwnerId = instance.CostingAndBilling.JobOwner ?? Guid.Empty;

			foreach (var contactId in instance.CostingAndBilling.AdditionalContacts)
			{
				contactIds.Add(contactId);
			}

			foreach (var error in instance.Errors)
			{
				if (string.IsNullOrWhiteSpace(error.ErrorCode))
				{
					continue;
				}

				errors[error.ErrorCode] = new JobError(error.ErrorCode, error.ErrorMessage);
			}

			if (instance.JobExecution.JobConfiguration == null || instance.JobExecution.JobConfiguration == Guid.Empty)
			{
				OrchestrationSettings = new WorkflowOrchestrationSettings();
			}
			else
			{
				var domConfiguration = planApi.DomHelpers.SlcWorkflowHelper.GetConfigurations([instance.JobExecution.JobConfiguration.Value]).FirstOrDefault();
				if (domConfiguration != null)
				{
					OrchestrationSettings = new WorkflowOrchestrationSettings(planApi, domConfiguration);
				}
				else
				{
					OrchestrationSettings = new WorkflowOrchestrationSettings();
				}
			}

			ParseNodesAndConnections(planApi, instance.Nodes, instance.Connections, instance.NodeRelationships, instance.NodeGroups);
		}

		private void ParseNodesAndConnections(MediaOpsPlanApi planApi, ICollection<StorageWorkflow.NodesSection> nodes, ICollection<StorageWorkflow.ConnectionsSection> connections, ICollection<StorageWorkflow.NodeRelationshipsSection> relationships, ICollection<StorageWorkflow.NodeGroupsSection> nodeGroups)
		{
			if (nodes == null || nodes.Count == 0)
			{
				NodeGraph = new NodeGraph<JobNode>();
				ConfigureNodeGraphSwapHooks();
				ParseGroups(planApi, new Dictionary<string, JobNode>(), nodeGroups);
				return;
			}

			var parsedNodesById = ParseNodes(planApi, nodes);
			var parsedConnections = ParseConnections(planApi, parsedNodesById, connections);
			var parsedLinks = ParseLinks(planApi, parsedNodesById, relationships);

			NodeGraph = new NodeGraph<JobNode>(parsedNodesById.Values, parsedConnections, parsedLinks);
			ConfigureNodeGraphSwapHooks();

			ParseGroups(planApi, parsedNodesById, nodeGroups);
		}

		private void ParseGroups(MediaOpsPlanApi planApi, IReadOnlyDictionary<string, JobNode> parsedNodesById, ICollection<StorageWorkflow.NodeGroupsSection> nodeGroups)
		{
			if (nodeGroups == null)
			{
				return;
			}

			foreach (var groupSection in nodeGroups)
			{
				var group = NodeGraph.AddGroup(groupSection.GroupName);
				foreach (var nodeId in groupSection.GroupNodeIds ?? [])
				{
					if (!parsedNodesById.TryGetValue(nodeId ?? string.Empty, out var node))
					{
						planApi.Logger.Warning(this, $"Node group '{groupSection.GroupName}' references unknown node '{nodeId}'. This node will be ignored.");
						continue;
					}

					group.Add(node);
				}
			}
		}

		private Dictionary<string, JobNode> ParseNodes(MediaOpsPlanApi planApi, ICollection<StorageWorkflow.NodesSection> nodes)
		{
			var parsedNodesById = new Dictionary<string, JobNode>();
			foreach (var nodeSecion in nodes)
			{
				var node = CreateNode(planApi, nodeSecion);
				if (node == null)
				{
					continue;
				}

				parsedNodesById.Add(node.Id, node);
			}

			return parsedNodesById;
		}

		private JobNode CreateNode(MediaOpsPlanApi planApi, StorageWorkflow.NodesSection nodeSecion)
		{
			switch (nodeSecion.NodeType.Value)
			{
				case StorageWorkflow.SlcWorkflowIds.Enums.Nodetype.Resource:
					return new JobResourceNode(planApi, nodeSecion);
				case StorageWorkflow.SlcWorkflowIds.Enums.Nodetype.ResourcePool:
					return new JobResourcePoolNode(planApi, nodeSecion);
				default:
					planApi.Logger.Warning(this, $"Node with ID {nodeSecion.NodeID} has unsupported node type {nodeSecion.NodeType.Value}. This node will be ignored.");
					return null;
			}
		}

		private List<NodeConnection<JobNode>> ParseConnections(MediaOpsPlanApi planApi, IReadOnlyDictionary<string, JobNode> parsedNodesById, ICollection<StorageWorkflow.ConnectionsSection> connections)
		{
			var parsedConnections = new List<NodeConnection<JobNode>>();
			if (connections == null)
			{
				return parsedConnections;
			}

			foreach (var connectionSection in connections)
			{
				try
				{
					parsedConnections.Add(new NodeConnection<JobNode>(connectionSection, id => parsedNodesById.TryGetValue(id, out var n) ? n : null));
				}
				catch (InvalidOperationException ex)
				{
					planApi.Logger.Warning(this, $"Connection with ID {connectionSection.ConnectionID} has invalid source or destination node. This connection will be ignored. Exception details: {ex}");
				}
			}

			return parsedConnections;
		}

		/// <summary>
		/// Configures the swap behavior of <see cref="NodeGraph"/> for the job context: retargets the job-level
		/// orchestration settings after a swap. The job-specific swap type rules are validated against the net
		/// original-to-final transition by <see cref="JobNodeGraphValidator"/> when the job is saved.
		/// </summary>
		private void ConfigureNodeGraphSwapHooks()
		{
			NodeGraph.SetExternalReferenceRetargeter(nodeIdMap => OrchestrationSettingsCloner.RetargetReferences(OrchestrationSettings, nodeIdMap));
		}

		private List<KeyValuePair<JobNode, JobNode>> ParseLinks(MediaOpsPlanApi planApi, IReadOnlyDictionary<string, JobNode> parsedNodesById, ICollection<StorageWorkflow.NodeRelationshipsSection> relationships)
		{
			var parsedLinks = new List<KeyValuePair<JobNode, JobNode>>();
			if (relationships == null)
			{
				return parsedLinks;
			}

			foreach (var relationship in relationships)
			{
				if (!parsedNodesById.TryGetValue(relationship.ParentNodeID ?? string.Empty, out var parent) ||
					!parsedNodesById.TryGetValue(relationship.ChildNodeID ?? string.Empty, out var child))
				{
					planApi.Logger.Warning(this, $"Node relationship referencing parent '{relationship.ParentNodeID}' and child '{relationship.ChildNodeID}' has an invalid node. This link will be ignored.");
					continue;
				}

				parsedLinks.Add(new KeyValuePair<JobNode, JobNode>(child, parent));
			}

			return parsedLinks;
		}
	}
}

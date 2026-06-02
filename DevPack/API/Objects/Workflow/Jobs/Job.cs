namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a job in MediaOps Plan.
	/// </summary>
	public class Job : ApiNamedObject
	{
		private StorageWorkflow.JobsInstance originalInstance;
		private StorageWorkflow.JobsInstance updatedInstance;
		private PropertySettingsContext propertiesContext;
		private PropertySettingsScope propertySettingsScope;

		private string key;

		/// <summary>
		/// Initializes a new instance of the <see cref="Job"/> class.
		/// </summary>
		public Job() : base()
		{
			IsNew = true;

			OrchestrationSettings = new WorkflowOrchestrationSettings();
			NodeGraph = new NodeGraph<JobNode>();
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
		}

		internal Job(MediaOpsPlanApi planApi, StorageWorkflow.JobsInstance instance) : base(instance.ID.Id)
		{
			ParseInstance(planApi, instance);

			propertiesContext = new PropertySettingsContext(planApi, Id, NodeGraph.Nodes.Select(n => n.Id));
			foreach (var node in NodeGraph.Nodes)
			{
				node.SetPropertiesContext(propertiesContext);
			}

			InitTracking();
		}

		/// <summary>
		/// Gets or sets the name of the job.
		/// </summary>
		public override string Name { get; set; }

		/// <summary>
		/// Gets or sets the key of the job. If the key is not explicitly set during initialization, the system automatically assigns a generated key that cannot be modified afterwards.
		/// </summary>
		public string Key { get => key; init => key = value; }

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
		/// Gets or sets the pre-roll of the job.
		/// </summary>
		public TimeSpan PreRoll { get; set; }

		/// <summary>
		/// Gets or sets the post-roll of the job.
		/// </summary>
		public TimeSpan PostRoll { get; set; }

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

		internal StorageWorkflow.JobsInstance OriginalInstance => originalInstance;

		internal PropertySettingsScope PropertySettingsScope => propertySettingsScope;

		internal PropertySettingsContext PropertySettingsContext => propertiesContext;

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

		private PropertySettingsScope GetOrCreateScope()
			=> propertySettingsScope ??= EnsureContext().CreateOwnerScope();

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
				PreRoll = workflow.PreRoll,
				PostRoll = workflow.PostRoll,
			};

			// 1. Clone the node graph first so we have a complete workflow-node-id -> job-node-id map
			//    before retargeting any DataReferences (orchestration settings may reference nodes).
			var nodeIdMap = NodeGraphCloner.Clone(workflow.NodeGraph, job.NodeGraph, CreateJobNode);

			// 2. Copy the per-node orchestration settings, pairing each new job node with its source workflow node.
			var workflowNodesById = workflow.NodeGraph.Nodes.ToDictionary(n => n.Id);
			var jobNodesById = job.NodeGraph.Nodes.ToDictionary(n => n.Id);
			foreach (var entry in nodeIdMap)
			{
				var workflowNode = workflowNodesById[entry.Key];
				var jobNode = jobNodesById[entry.Value];
				OrchestrationSettingsCloner.Clone(workflowNode.OrchestrationSettings, jobNode.OrchestrationSettings, nodeIdMap);
			}

			// 3. Copy the job-level orchestration settings.
			OrchestrationSettingsCloner.Clone(workflow.OrchestrationSettings, job.OrchestrationSettings, nodeIdMap);

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
				hash = (hash * 23) + PreRoll.GetHashCode();
				hash = (hash * 23) + PostRoll.GetHashCode();
				hash = (hash * 23) + (Notes != null ? Notes.GetHashCode() : 0);
				hash = (hash * 23) + (OrchestrationSettings != null ? OrchestrationSettings.GetHashCode() : 0);
				hash = (hash * 23) + (NodeGraph != null ? NodeGraph.GetHashCode() : 0);
				hash = (hash * 23) + State.GetHashCode();

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
				   PreRoll == other.PreRoll &&
				   PostRoll == other.PostRoll &&
				   Notes == other.Notes &&
				   OrchestrationSettings == other.OrchestrationSettings &&
				   NodeGraph == other.NodeGraph &&
				   State == other.State;
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
			updatedInstance.JobInfo.Preroll = PreRoll != TimeSpan.Zero ? Start.Add(-PreRoll).UtcDateTime : Start.UtcDateTime;
			updatedInstance.JobInfo.Postroll = PostRoll != TimeSpan.Zero ? End.Add(PostRoll).UtcDateTime : End.UtcDateTime;
			updatedInstance.JobInfo.JobNotes = Notes;

			updatedInstance.JobExecution.JobConfiguration = OrchestrationSettings.Id;

			updatedInstance.JobInfo.JobPriority = EnumExtensions.MapEnum<JobPriority, StorageWorkflow.SlcWorkflowIds.Enums.Jobpriority>(Priority);

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

			this.key = key;
		}

		private void ParseInstance(MediaOpsPlanApi planApi, StorageWorkflow.JobsInstance instance)
		{
			this.originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

			Name = instance.JobInfo.JobName;
			key = instance.JobInfo.JobID;
			Description = instance.JobInfo.JobDescription;
			Start = instance.JobInfo.JobStart.Value;
			End = instance.JobInfo.JobEnd.Value;
			PreRoll = instance.JobInfo.Preroll.HasValue ? (Start - instance.JobInfo.Preroll.Value) : TimeSpan.Zero;
			PostRoll = instance.JobInfo.Postroll.HasValue ? (instance.JobInfo.Postroll.Value - End) : TimeSpan.Zero;
			Notes = instance.JobInfo.JobNotes;

			Priority = instance.JobInfo.JobPriority.HasValue
				? EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Enums.Jobpriority, JobPriority>(instance.JobInfo.JobPriority.Value)
				: JobPriority.Normal;
			State = EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Behaviors.Job_Behavior.StatusesEnum, JobState>(instance.Status);

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

			ParseNodesAndConnections(planApi, instance.Nodes, instance.Connections);
		}

		private void ParseNodesAndConnections(MediaOpsPlanApi planApi, ICollection<StorageWorkflow.NodesSection> nodes, ICollection<StorageWorkflow.ConnectionsSection> connections)
		{
			if (nodes == null || nodes.Count == 0)
			{
				NodeGraph = new NodeGraph<JobNode>();
				return;
			}

			var parsedNodesById = new Dictionary<string, JobNode>();
			foreach (var nodeSecion in nodes)
			{
				JobNode node = null;
				switch (nodeSecion.NodeType.Value)
				{
					case StorageWorkflow.SlcWorkflowIds.Enums.Nodetype.Resource:
						node = new JobResourceNode(planApi, nodeSecion);
						break;
					case StorageWorkflow.SlcWorkflowIds.Enums.Nodetype.ResourcePool:
						node = new JobResourcePoolNode(planApi, nodeSecion);
						break;
					default:
						planApi.Logger.Warning(this, $"Node with ID {nodeSecion.NodeID} has unsupported node type {nodeSecion.NodeType.Value}. This node will be ignored.");
						break;
				}

				if (node == null)
				{
					continue;
				}

				parsedNodesById.Add(node.Id, node);
			}

			if (connections == null || connections.Count == 0)
			{
				NodeGraph = new NodeGraph<JobNode>(parsedNodesById.Values);
				return;
			}

			var parsedConnections = new List<NodeConnection<JobNode>>();
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

			NodeGraph = new NodeGraph<JobNode>(parsedNodesById.Values, parsedConnections);
		}
	}
}

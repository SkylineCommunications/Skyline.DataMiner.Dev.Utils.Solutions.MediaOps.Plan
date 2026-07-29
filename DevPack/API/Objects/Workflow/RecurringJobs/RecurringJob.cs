namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a recurring job in MediaOps Plan.
	/// </summary>
	public class RecurringJob : ApiNamedObject
	{
		private readonly HashSet<Guid> contactIds = [];

		private RecurringJobsInstance originalInstance;
		private RecurringJobsInstance updatedInstance;
		private PropertySettingsContext propertiesContext;
		private PropertySettingsScope propertySettingsScope;

		/// <summary>
		/// Initializes a new instance of the <see cref="RecurringJob"/> class.
		/// </summary>
		public RecurringJob() : base()
		{
			IsNew = true;

			OrchestrationSettings = new WorkflowOrchestrationSettings();
			NodeGraph = new NodeGraph<RecurringJobNode>();
			ConfigureNodeGraphSwapHooks();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RecurringJob"/> class with a specific recurring job ID.
		/// </summary>
		public RecurringJob(Guid jobId) : base(jobId)
		{
			IsNew = true;
			HasUserDefinedId = true;

			OrchestrationSettings = new WorkflowOrchestrationSettings();
			NodeGraph = new NodeGraph<RecurringJobNode>();
			ConfigureNodeGraphSwapHooks();
		}

		internal RecurringJob(MediaOpsPlanApi planApi, RecurringJobsInstance instance) : base(instance.ID.Id)
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
		/// Gets or sets the name of the recurring job.
		/// </summary>
		public override string Name { get; set; }

		/// <summary>
		/// Gets or sets the description of the recurring job.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Gets or sets the priority of the recurring job.
		/// </summary>
		public RecurringJobPriority Priority { get; set; } = RecurringJobPriority.Normal;

		/// <summary>
		/// Gets the state of the recurring job.
		/// </summary>
		public RecurringJobState State { get; private set; } = RecurringJobState.Active;

		/// <summary>
		/// Gets the orchestration settings assigned to this recurring job.
		/// </summary>
		public OrchestrationSettings OrchestrationSettings { get; private set; }

		/// <summary>
		/// Gets the node graph containing all nodes and connections that define the recurring job structure.
		/// </summary>
		public NodeGraph<RecurringJobNode> NodeGraph { get; private set; }

		/// <summary>
		/// Gets the custom property settings associated with this recurring job.
		/// Property settings are loaded lazily in a single batch together with the property settings of all nodes.
		/// </summary>
		public IReadOnlyCollection<CustomPropertySetting> CustomPropertySettings => GetOrCreateScope().CustomPropertySettings;

		/// <summary>
		/// Gets the property settings associated with this recurring job.
		/// Property settings are loaded lazily in a single batch together with the property settings of all nodes.
		/// </summary>
		public IReadOnlyCollection<PropertySetting> PropertySettings => GetOrCreateScope().PropertySettings;

		/// <summary>
		/// Gets or sets the start time of the first job generated from this recurring job, in the time zone specified by <see cref="TimeZone"/>.
		/// </summary>
		public DateTimeOffset Start { get; set; }

		/// <summary>
		/// Gets or sets the duration of each generated job, excluding pre-roll and post-roll.
		/// </summary>
		public TimeSpan Duration { get; set; }

		/// <summary>
		/// Gets or sets the pre-roll duration of each generated job.
		/// </summary>
		public TimeSpan PreRollDuration { get; set; }

		/// <summary>
		/// Gets or sets the post-roll duration of each generated job.
		/// </summary>
		public TimeSpan PostRollDuration { get; set; }

		/// <summary>
		/// Gets or sets the time zone used when generating jobs from this recurring job.
		/// </summary>
		public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Utc;

		/// <summary>
		/// Gets or sets the desired state of each job generated from this recurring job.
		/// </summary>
		public DesiredJobState DesiredJobState { get; set; } = DesiredJobState.Draft;

		/// <summary>
		/// Gets or sets the current process state of this recurring job.
		/// </summary>
		public RecurringJobProcessState ProcessState { get; set; } = RecurringJobProcessState.NA;

		/// <summary>
		/// Gets or sets the recurring pattern that defines when jobs are generated from this recurring job.
		/// </summary>
		public RecurringPattern Pattern { get; private set; } = new RecurringPattern();

		/// <summary>
		/// Gets or sets the ID of the organization associated with the recurring job.
		/// </summary>
		public Guid OrganizationId { get; set; }

		/// <summary>
		/// Gets or sets the ID of the owner of the recurring job.
		/// </summary>
		public Guid OwnerId { get; set; }

		/// <summary>
		/// Gets the collection of contact IDs associated with the recurring job.
		/// </summary>
		public IReadOnlyCollection<Guid> ContactIds => contactIds;

		/// <summary>
		/// Adds a contact to the recurring job.
		/// </summary>
		/// <param name="contactId">The unique identifier of the contact to add.</param>
		/// <returns>The current <see cref="RecurringJob"/> instance.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="contactId"/> is <see cref="Guid.Empty"/>.</exception>
		public RecurringJob AddContact(Guid contactId)
		{
			if (contactId == Guid.Empty)
			{
				throw new ArgumentException(nameof(contactId));
			}

			contactIds.Add(contactId);
			return this;
		}

		/// <summary>
		/// Removes a contact from the recurring job.
		/// </summary>
		/// <param name="contactId">The unique identifier of the contact to remove.</param>
		/// <returns>The current <see cref="RecurringJob"/> instance.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="contactId"/> is <see cref="Guid.Empty"/>.</exception>
		public RecurringJob RemoveContact(Guid contactId)
		{
			if (contactId == Guid.Empty)
			{
				throw new ArgumentException(nameof(contactId));
			}

			contactIds.Remove(contactId);
			return this;
		}

		/// <summary>
		/// Gets or sets the unique identifier of the associated recurring job type.
		/// </summary>
		public string JobTypeCategoryId { get; set; }

		internal RecurringJobsInstance OriginalInstance => originalInstance;

		internal PropertySettingsScope PropertySettingsScope => propertySettingsScope;

		internal PropertySettingsContext PropertySettingsContext => propertiesContext;

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + Id.GetHashCode();
				hash = (hash * 23) + (Name != null ? Name.GetHashCode() : 0);
				hash = (hash * 23) + (Description != null ? Description.GetHashCode() : 0);
				hash = (hash * 23) + Priority.GetHashCode();
				hash = (hash * 23) + (OrchestrationSettings != null ? OrchestrationSettings.GetHashCode() : 0);
				hash = (hash * 23) + (NodeGraph != null ? NodeGraph.GetHashCode() : 0);
				hash = (hash * 23) + Duration.GetHashCode();
				hash = (hash * 23) + PreRollDuration.GetHashCode();
				hash = (hash * 23) + PostRollDuration.GetHashCode();
				hash = (hash * 23) + (TimeZone != null ? TimeZone.GetHashCode() : 0);
				hash = (hash * 23) + DesiredJobState.GetHashCode();
				hash = (hash * 23) + ProcessState.GetHashCode();
				hash = (hash * 23) + State.GetHashCode();
				hash = (hash * 23) + (Pattern != null ? Pattern.GetHashCode() : 0);
				hash = (hash * 23) + OrganizationId.GetHashCode();
				hash = (hash * 23) + OwnerId.GetHashCode();
				hash = (hash * 23) + (JobTypeCategoryId != null ? JobTypeCategoryId.GetHashCode() : 0);

				foreach (var contactId in contactIds.OrderBy(x => x).ToArray())
				{
					hash = (hash * 23) + contactId.GetHashCode();
				}

				return hash;
			}
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current recurring job instance.
		/// </summary>
		/// <param name="obj">The object to compare with the current recurring job instance.</param>
		/// <returns>true if the specified object is a recurring job and has the same values for all properties as the current
		/// instance; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			if (obj is not RecurringJob other)
			{
				return false;
			}

			return Id == other.Id
				&& Name == other.Name
				&& Description == other.Description
				&& Priority == other.Priority
				&& Duration == other.Duration
				&& PreRollDuration == other.PreRollDuration
				&& PostRollDuration == other.PostRollDuration
				&& Equals(OrchestrationSettings, other.OrchestrationSettings)
				&& Equals(NodeGraph, other.NodeGraph)
				&& Equals(TimeZone, other.TimeZone)
				&& DesiredJobState == other.DesiredJobState
				&& ProcessState == other.ProcessState
				&& State == other.State
				&& Equals(Pattern, other.Pattern)
				&& OrganizationId == other.OrganizationId
				&& OwnerId == other.OwnerId
				&& JobTypeCategoryId == other.JobTypeCategoryId
				&& contactIds.SetEquals(other.contactIds);
		}

		private PropertySettingsScope GetOrCreateScope()
			=> propertySettingsScope ??= EnsureContext().CreateOwnerScope();

		internal PropertySettingsContext EnsureContext()
		{
			if (propertiesContext == null)
			{
				// New, unsaved recurring job: no backend data to load. A null planApi is fine because the
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

		private void ParseInstance(MediaOpsPlanApi planApi, RecurringJobsInstance instance)
		{
			this.originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

			Name = instance.JobInfo.JobName;
			Description = instance.JobInfo.JobDescription;
			Start = instance.JobInfo.JobStart.Value;
			Duration = instance.RecurringInfo.Duration ?? TimeSpan.Zero;
			State = EnumExtensions.MapEnum<SlcWorkflowIds.Behaviors.Recurringjob_Behavior.StatusesEnum, RecurringJobState>(instance.Status);

			PreRollDuration = instance.JobInfo.JobStart.HasValue && instance.JobInfo.Preroll.HasValue
				? instance.JobInfo.JobStart.Value - instance.JobInfo.Preroll.Value
				: TimeSpan.Zero;

			PostRollDuration = instance.JobInfo.JobEnd.HasValue && instance.JobInfo.Postroll.HasValue
				? instance.JobInfo.Postroll.Value - instance.JobInfo.JobEnd.Value
				: TimeSpan.Zero;

			var timeZoneString = Convert.ToString(instance.RecurringInfo.TimeZone);
			TimeZone = string.IsNullOrEmpty(timeZoneString)
				? TimeZoneInfo.Utc
				: TimeZoneInfo.FromSerializedString(timeZoneString);

			var patternJson = instance.RecurringInfo.RecurringPattern;
			Pattern = string.IsNullOrEmpty(patternJson)
				? new RecurringPattern()
				: RecurringPattern.Deserialize(patternJson);

			ProcessState = (instance.RecurringInfo.ProcessStatus ?? SlcWorkflowIds.Enums.Processstatus.NA).MapEnum<SlcWorkflowIds.Enums.Processstatus, RecurringJobProcessState>();
			DesiredJobState = (instance.RecurringInfo.DesiredJobStatus ?? SlcWorkflowIds.Enums.Desiredjobstatus.Draft).MapEnum<SlcWorkflowIds.Enums.Desiredjobstatus, DesiredJobState>();

			if (instance.JobExecution.JobConfiguration == null || instance.JobExecution.JobConfiguration == Guid.Empty)
			{
				OrchestrationSettings = new WorkflowOrchestrationSettings();
			}
			else
			{
				var domConfiguration = planApi.DomHelpers.SlcWorkflowHelper.GetConfigurations([instance.JobExecution.JobConfiguration.Value]).FirstOrDefault();
				OrchestrationSettings = domConfiguration != null
					? new WorkflowOrchestrationSettings(planApi, domConfiguration)
					: new WorkflowOrchestrationSettings();
			}

			ParseNodesAndConnections(planApi, instance.Nodes, instance.Connections, instance.NodeRelationships);
		}

		private void ParseNodesAndConnections(MediaOpsPlanApi planApi, ICollection<StorageWorkflow.NodesSection> nodes, ICollection<StorageWorkflow.ConnectionsSection> connections, ICollection<StorageWorkflow.NodeRelationshipsSection> relationships)
		{
			if (nodes == null || nodes.Count == 0)
			{
				NodeGraph = new NodeGraph<RecurringJobNode>();
				ConfigureNodeGraphSwapHooks();
				return;
			}

			var parsedNodesById = ParseNodes(planApi, nodes);
			var parsedConnections = ParseConnections(planApi, parsedNodesById, connections);
			var parsedLinks = ParseLinks(planApi, parsedNodesById, relationships);

			NodeGraph = new NodeGraph<RecurringJobNode>(parsedNodesById.Values, parsedConnections, parsedLinks);
			ConfigureNodeGraphSwapHooks();
		}

		private Dictionary<string, RecurringJobNode> ParseNodes(MediaOpsPlanApi planApi, ICollection<StorageWorkflow.NodesSection> nodes)
		{
			var parsedNodesById = new Dictionary<string, RecurringJobNode>();
			foreach (var nodeSection in nodes)
			{
				var node = CreateNode(planApi, nodeSection);
				if (node == null)
				{
					continue;
				}

				parsedNodesById.Add(node.Id, node);
			}

			return parsedNodesById;
		}

		private RecurringJobNode CreateNode(MediaOpsPlanApi planApi, StorageWorkflow.NodesSection nodeSection)
		{
			switch (nodeSection.NodeType.Value)
			{
				case StorageWorkflow.SlcWorkflowIds.Enums.Nodetype.Resource:
					return new RecurringJobResourceNode(planApi, nodeSection);
				case StorageWorkflow.SlcWorkflowIds.Enums.Nodetype.ResourcePool:
					return new RecurringJobResourcePoolNode(planApi, nodeSection);
				default:
					planApi.Logger.Warning(this, $"Node with ID {nodeSection.NodeID} has unsupported node type {nodeSection.NodeType.Value}. This node will be ignored.");
					return null;
			}
		}

		private List<NodeConnection<RecurringJobNode>> ParseConnections(MediaOpsPlanApi planApi, IReadOnlyDictionary<string, RecurringJobNode> parsedNodesById, ICollection<StorageWorkflow.ConnectionsSection> connections)
		{
			var parsedConnections = new List<NodeConnection<RecurringJobNode>>();
			if (connections == null)
			{
				return parsedConnections;
			}

			foreach (var connectionSection in connections)
			{
				try
				{
					parsedConnections.Add(new NodeConnection<RecurringJobNode>(connectionSection, id => parsedNodesById.TryGetValue(id, out var n) ? n : null));
				}
				catch (InvalidOperationException ex)
				{
					planApi.Logger.Warning(this, $"Connection with ID {connectionSection.ConnectionID} has invalid source or destination node. This connection will be ignored. Exception details: {ex}");
				}
			}

			return parsedConnections;
		}

		private List<KeyValuePair<RecurringJobNode, RecurringJobNode>> ParseLinks(MediaOpsPlanApi planApi, IReadOnlyDictionary<string, RecurringJobNode> parsedNodesById, ICollection<StorageWorkflow.NodeRelationshipsSection> relationships)
		{
			var parsedLinks = new List<KeyValuePair<RecurringJobNode, RecurringJobNode>>();
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

				parsedLinks.Add(new KeyValuePair<RecurringJobNode, RecurringJobNode>(child, parent));
			}

			return parsedLinks;
		}

		/// <summary>
		/// Configures the swap behavior of <see cref="NodeGraph"/> for the recurring job context: retargets the
		/// job-level orchestration settings after a swap so any node-scoped <see cref="DataReference"/> instances
		/// continue to point at the correct node after the swap.
		/// </summary>
		private void ConfigureNodeGraphSwapHooks()
		{
			NodeGraph.SetExternalReferenceRetargeter(nodeIdMap => OrchestrationSettingsCloner.RetargetReferences(OrchestrationSettings, nodeIdMap));
		}

		internal RecurringJobsInstance GetInstanceWithChanges()
		{
			if (updatedInstance == null)
			{
				updatedInstance = IsNew ? new RecurringJobsInstance(Id) : originalInstance.Clone();
			}

			updatedInstance.JobInfo.JobName = Name;
			updatedInstance.JobInfo.JobDescription = Description;

			updatedInstance.JobInfo.JobStart = Start.UtcDateTime;
			updatedInstance.JobInfo.JobEnd = Start.Add(Duration).UtcDateTime;
			updatedInstance.JobInfo.Preroll = Start.Subtract(PreRollDuration).UtcDateTime;
			updatedInstance.JobInfo.Postroll = Start.Add(Duration).Add(PostRollDuration).UtcDateTime;

			updatedInstance.RecurringInfo.Duration = Duration;
			updatedInstance.RecurringInfo.ProcessStatus = ProcessState.MapEnum<RecurringJobProcessState, SlcWorkflowIds.Enums.Processstatus>();
			updatedInstance.RecurringInfo.DesiredJobStatus = DesiredJobState.MapEnum<DesiredJobState, SlcWorkflowIds.Enums.Desiredjobstatus>();
			updatedInstance.RecurringInfo.TimeZone = TimeZone.ToSerializedString();
			updatedInstance.RecurringInfo.RecurringPattern = Pattern.Serialize();

			// Reusing JobSource field to store CategoryId to be backwards compatible with existing implementations.
			updatedInstance.JobInfo.JobSource = JobTypeCategoryId;

			updatedInstance.JobExecution.JobConfiguration = OrchestrationSettings.Id;

			updatedInstance.JobInfo.JobPriority = Priority.MapEnum<RecurringJobPriority, SlcWorkflowIds.Enums.Jobpriority>();

			updatedInstance.CostingAndBilling.Organization = OrganizationId != Guid.Empty ? OrganizationId : null;
			updatedInstance.CostingAndBilling.JobOwner = OwnerId != Guid.Empty ? OwnerId : null;

			updatedInstance.CostingAndBilling.AdditionalContacts.Clear();
			foreach (var contactId in ContactIds)
			{
				updatedInstance.CostingAndBilling.AdditionalContacts.Add(contactId);
			}

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

			return updatedInstance;
		}
	}
}

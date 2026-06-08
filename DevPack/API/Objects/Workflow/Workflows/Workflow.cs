namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a workflow in MediaOps Plan.
	/// </summary>
	public class Workflow : ApiNamedObject
	{
		private StorageWorkflow.WorkflowsInstance originalInstance;
		private StorageWorkflow.WorkflowsInstance updatedInstance;
		private PropertySettingsContext propertiesContext;
		private PropertySettingsScope propertySettingsScope;

		/// <summary>
		/// Initializes a new instance of the <see cref="Workflow"/> class.
		/// </summary>
		public Workflow() : base()
		{
			IsNew = true;

			OrchestrationSettings = new WorkflowOrchestrationSettings();
			NodeGraph = new NodeGraph<WorkflowNode>();
			ConfigureNodeGraphSwapHooks();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Workflow"/> class with a specific workflow ID.
		/// </summary>
		/// <param name="workflowId">The unique identifier of the workflow.</param>
		public Workflow(Guid workflowId) : base(workflowId)
		{
			IsNew = true;
			HasUserDefinedId = true;

			OrchestrationSettings = new WorkflowOrchestrationSettings();
			NodeGraph = new NodeGraph<WorkflowNode>();
			ConfigureNodeGraphSwapHooks();
		}

		internal Workflow(MediaOpsPlanApi planApi, StorageWorkflow.WorkflowsInstance instance) : base(instance.ID.Id)
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
		/// Gets or sets the name of the workflow.
		/// </summary>
		public override string Name { get; set; }

		/// <summary>
		/// Gets or sets the description of the workflow.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Gets or sets the priority of the workflow.
		/// </summary>
		public WorkflowPriority Priority { get; set; } = WorkflowPriority.Normal;

		/// <summary>
		/// Gets or sets a value indicating whether the workflow is a favorite.
		/// </summary>
		public bool IsFavorite { get; set; }

		/// <summary>
		/// Gets or sets the pre-roll of the workflow.
		/// </summary>
		public TimeSpan PreRoll { get; set; }

		/// <summary>
		/// Gets or sets the post-roll of the workflow.
		/// </summary>
		public TimeSpan PostRoll { get; set; }

		/// <summary>
		/// Gets or sets the notes of the workflow.
		/// </summary>
		public string Notes { get; set; }

		/// <summary>
		/// Gets the state of the workflow.
		/// </summary>
		public WorkflowState State { get; private set; }

		/// <summary>
		/// Gets the orchestration settings assigned to this workflow.
		/// </summary>
		public OrchestrationSettings OrchestrationSettings { get; private set; }

		/// <summary>
		/// Gets the node graph containing all nodes and connections that define the workflow structure.
		/// </summary>
		public NodeGraph<WorkflowNode> NodeGraph { get; private set; }

		/// <summary>
		/// Gets the custom property settings associated with this workflow.
		/// Property settings are loaded lazily in a single batch together with the property settings of all nodes.
		/// Use <see cref="AddCustomProperty"/>, <see cref="SetCustomProperties"/> and <see cref="RemoveCustomProperty"/> to modify them.
		/// </summary>
		public IReadOnlyCollection<CustomPropertySetting> CustomPropertySettings => GetOrCreateScope().CustomPropertySettings;

		/// <summary>
		/// Gets the property settings associated with this workflow.
		/// Property settings are loaded lazily in a single batch together with the property settings of all nodes.
		/// Use <see cref="AddProperty"/>, <see cref="SetProperties"/> and <see cref="RemoveProperty"/> to modify them.
		/// </summary>
		public IReadOnlyCollection<PropertySetting> PropertySettings => GetOrCreateScope().PropertySettings;

		internal StorageWorkflow.WorkflowsInstance OriginalInstance => originalInstance;

		internal PropertySettingsScope PropertySettingsScope => propertySettingsScope;

		internal PropertySettingsContext PropertySettingsContext => propertiesContext;

		/// <summary>
		/// Adds a custom property setting to this workflow.
		/// </summary>
		/// <param name="setting">The custom property setting to add.</param>
		/// <returns>The current <see cref="Workflow"/> instance.</returns>
		public Workflow AddCustomProperty(CustomPropertySetting setting)
		{
			GetOrCreateScope().AddCustomProperty(setting);
			return this;
		}

		/// <summary>
		/// Replaces the entire collection of custom property settings associated with this workflow with the specified settings.
		/// </summary>
		/// <param name="settings">The custom property settings that should replace the current collection.</param>
		/// <returns>The current <see cref="Workflow"/> instance.</returns>
		public Workflow SetCustomProperties(IEnumerable<CustomPropertySetting> settings)
		{
			GetOrCreateScope().SetCustomProperties(settings);
			return this;
		}

		/// <summary>
		/// Removes the specified custom property setting from this workflow.
		/// </summary>
		/// <param name="setting">The custom property setting to remove.</param>
		/// <returns>The current <see cref="Workflow"/> instance.</returns>
		public Workflow RemoveCustomProperty(CustomPropertySetting setting)
		{
			GetOrCreateScope().RemoveCustomProperty(setting);
			return this;
		}

		/// <summary>
		/// Adds a property setting to this workflow.
		/// </summary>
		/// <param name="setting">The property setting to add.</param>
		/// <returns>The current <see cref="Workflow"/> instance.</returns>
		public Workflow AddProperty(PropertySetting setting)
		{
			GetOrCreateScope().AddProperty(setting);
			return this;
		}

		/// <summary>
		/// Replaces the entire collection of property settings associated with this workflow with the specified settings.
		/// </summary>
		/// <param name="settings">The property settings that should replace the current collection.</param>
		/// <returns>The current <see cref="Workflow"/> instance.</returns>
		public Workflow SetProperties(IEnumerable<PropertySetting> settings	)
		{
			GetOrCreateScope().SetProperties(settings);
			return this;
		}

		/// <summary>
		/// Removes the specified property setting from this workflow.
		/// </summary>
		/// <param name="setting">The property setting to remove.</param>
		/// <returns>The current <see cref="Workflow"/> instance.</returns>
		public Workflow RemoveProperty(PropertySetting setting)
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
				// New, unsaved workflow: no backend data to load. A null planApi is fine because the
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
				hash = (hash * 23) + IsFavorite.GetHashCode();
				hash = (hash * 23) + PreRoll.GetHashCode();
				hash = (hash * 23) + PostRoll.GetHashCode();
				hash = (hash * 23) + (Notes != null ? Notes.GetHashCode() : 0);
				hash = (hash * 23) + (OrchestrationSettings != null ? OrchestrationSettings.GetHashCode() : 0);
				hash = (hash * 23) + (NodeGraph != null ? NodeGraph.GetHashCode() : 0);
				hash = (hash * 23) + State.GetHashCode();

				return hash;
			}
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current workflow instance.
		/// </summary>
		/// <param name="obj">The object to compare with the current workflow instance.</param>
		/// <returns>true if the specified object is a workflow and has the same values for all properties as the current
		/// instance; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			if (obj is not Workflow other)
			{
				return false;
			}

			return Id == other.Id &&
				   Name == other.Name &&
				   Description == other.Description &&
				   Priority == other.Priority &&
				   IsFavorite == other.IsFavorite &&
				   PreRoll == other.PreRoll &&
				   PostRoll == other.PostRoll &&
				   Notes == other.Notes &&
				   OrchestrationSettings == other.OrchestrationSettings &&
				   NodeGraph == other.NodeGraph &&
				   State == other.State;
		}

		internal StorageWorkflow.WorkflowsInstance GetInstanceWithChanges()
		{
			if (updatedInstance == null)
			{
				updatedInstance = IsNew ? new StorageWorkflow.WorkflowsInstance(Id) : originalInstance.Clone();
			}

			updatedInstance.WorkflowInfo.WorkflowName = Name;
			updatedInstance.WorkflowInfo.WorkflowDescription = Description;
			updatedInstance.WorkflowInfo.Favorite = IsFavorite;
			updatedInstance.WorkflowInfo.Preroll = PreRoll != TimeSpan.Zero ? PreRoll: null;
			updatedInstance.WorkflowInfo.Postroll = PostRoll != TimeSpan.Zero ? PostRoll : null;
			updatedInstance.WorkflowInfo.WorkflowNotes = Notes;

			updatedInstance.WorkflowExecution.WorkflowConfiguration = OrchestrationSettings.Id;

			updatedInstance.WorkflowInfo.Priority = EnumExtensions.MapEnum<WorkflowPriority, StorageWorkflow.SlcWorkflowIds.Enums.Priority>(Priority);

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

		private void ParseInstance(MediaOpsPlanApi planApi, StorageWorkflow.WorkflowsInstance instance)
		{
			originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

			Name = instance.WorkflowInfo.WorkflowName;
			Description = instance.WorkflowInfo.WorkflowDescription;
			IsFavorite = instance.WorkflowInfo.Favorite.GetValueOrDefault();
			PreRoll = instance.WorkflowInfo.Preroll.HasValue ? instance.WorkflowInfo.Preroll.Value : TimeSpan.Zero;
			PostRoll = instance.WorkflowInfo.Postroll.HasValue ? instance.WorkflowInfo.Postroll.Value : TimeSpan.Zero;
			Notes = instance.WorkflowInfo.WorkflowNotes;

			Priority = instance.WorkflowInfo.Priority.HasValue ? EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Enums.Priority, WorkflowPriority>(instance.WorkflowInfo.Priority.Value) : WorkflowPriority.Normal;
			State = EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Behaviors.Workflow_Behavior.StatusesEnum, WorkflowState>(instance.Status);

			if (instance.WorkflowExecution.WorkflowConfiguration == null || instance.WorkflowExecution.WorkflowConfiguration == Guid.Empty)
			{
				OrchestrationSettings = new WorkflowOrchestrationSettings();
			}
			else
			{
				var domConfiguration = planApi.DomHelpers.SlcWorkflowHelper.GetConfigurations([instance.WorkflowExecution.WorkflowConfiguration.Value]).FirstOrDefault();
				if (domConfiguration != null)
				{
					OrchestrationSettings = new WorkflowOrchestrationSettings(planApi, domConfiguration);
				}
				else
				{
					OrchestrationSettings = new WorkflowOrchestrationSettings();
				}
			}

			ParseNodesAndConnections(planApi, instance.Nodes, instance.Connections, instance.NodeRelationships);
		}

		private void ParseNodesAndConnections(MediaOpsPlanApi planApi, ICollection<StorageWorkflow.NodesSection> nodes, ICollection<StorageWorkflow.ConnectionsSection> connections, ICollection<StorageWorkflow.NodeRelationshipsSection> relationships)
		{
			if (nodes == null || nodes.Count == 0)
			{
				NodeGraph = new NodeGraph<WorkflowNode>();
				ConfigureNodeGraphSwapHooks();
				return;
			}

			var parsedNodesById = ParseNodes(planApi, nodes);
			var parsedConnections = ParseConnections(planApi, parsedNodesById, connections);
			var parsedLinks = ParseLinks(planApi, parsedNodesById, relationships);

			NodeGraph = new NodeGraph<WorkflowNode>(parsedNodesById.Values, parsedConnections, parsedLinks);
			ConfigureNodeGraphSwapHooks();
		}

		private Dictionary<string, WorkflowNode> ParseNodes(MediaOpsPlanApi planApi, ICollection<StorageWorkflow.NodesSection> nodes)
		{
			var parsedNodesById = new Dictionary<string, WorkflowNode>();
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

		private WorkflowNode CreateNode(MediaOpsPlanApi planApi, StorageWorkflow.NodesSection nodeSecion)
		{
			switch (nodeSecion.NodeType.Value)
			{
				case StorageWorkflow.SlcWorkflowIds.Enums.Nodetype.Resource:
					return new WorkflowResourceNode(planApi, nodeSecion);
				case StorageWorkflow.SlcWorkflowIds.Enums.Nodetype.ResourcePool:
					return new WorkflowResourcePoolNode(planApi, nodeSecion);
				default:
					planApi.Logger.Warning(this, $"Node with ID {nodeSecion.NodeID} has unsupported node type {nodeSecion.NodeType.Value}. This node will be ignored.");
					return null;
			}
		}

		private List<NodeConnection<WorkflowNode>> ParseConnections(MediaOpsPlanApi planApi, IReadOnlyDictionary<string, WorkflowNode> parsedNodesById, ICollection<StorageWorkflow.ConnectionsSection> connections)
		{
			var parsedConnections = new List<NodeConnection<WorkflowNode>>();
			if (connections == null)
			{
				return parsedConnections;
			}

			foreach (var connectionSection in connections)
			{
				try
				{
					parsedConnections.Add(new NodeConnection<WorkflowNode>(connectionSection, id => parsedNodesById.TryGetValue(id, out var n) ? n : null));
				}
				catch (InvalidOperationException ex)
				{
					planApi.Logger.Warning(this, $"Connection with ID {connectionSection.ConnectionID} has invalid source or destination node. This connection will be ignored. Exception details: {ex}");
				}
			}

			return parsedConnections;
		}

		/// <summary>
		/// Configures the swap behavior of <see cref="NodeGraph"/> for the workflow context: retargets the workflow-level
		/// orchestration settings after a swap. The workflow-specific swap type rules are validated against the net
		/// original-to-final transition by <see cref="WorkflowNodeGraphValidator"/> when the workflow is saved.
		/// </summary>
		private void ConfigureNodeGraphSwapHooks()
		{
			NodeGraph.SetExternalReferenceRetargeter(nodeIdMap => OrchestrationSettingsCloner.RetargetReferences(OrchestrationSettings, nodeIdMap));
		}

		private List<KeyValuePair<WorkflowNode, WorkflowNode>> ParseLinks(MediaOpsPlanApi planApi, IReadOnlyDictionary<string, WorkflowNode> parsedNodesById, ICollection<StorageWorkflow.NodeRelationshipsSection> relationships)
		{
			var parsedLinks = new List<KeyValuePair<WorkflowNode, WorkflowNode>>();
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

				parsedLinks.Add(new KeyValuePair<WorkflowNode, WorkflowNode>(child, parent));
			}

			return parsedLinks;
		}
	}
}

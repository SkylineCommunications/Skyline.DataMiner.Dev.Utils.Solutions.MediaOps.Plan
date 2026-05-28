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
		private WorkflowPropertiesLoader propertiesLoader;
		private WorkflowPropertyValuesEditor propertyValuesEditor;

		/// <summary>
		/// Initializes a new instance of the <see cref="Workflow"/> class.
		/// </summary>
		public Workflow() : base()
		{
			IsNew = true;

			OrchestrationSettings = new WorkflowOrchestrationSettings();
			NodeGraph = new NodeGraph<WorkflowNode>();
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
		}

		internal Workflow(MediaOpsPlanApi planApi, StorageWorkflow.WorkflowsInstance instance) : base(instance.ID.Id)
		{
			ParseInstance(planApi, instance);

			propertiesLoader = new WorkflowPropertiesLoader(planApi, Id, NodeGraph.Nodes.Select(n => n.Id));
			foreach (var node in NodeGraph.Nodes)
			{
				node.SetPropertiesLoader(propertiesLoader);
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
		/// Gets the custom property values associated with this workflow.
		/// Property values are loaded lazily in a single batch together with the property values of all nodes.
		/// Use <see cref="AddCustomProperty"/>, <see cref="SetCustomProperty"/> and <see cref="RemoveCustomProperty"/> to modify them.
		/// </summary>
		public IReadOnlyCollection<CustomPropertyValue> CustomPropertyValues => PropertyValuesEditor.CustomPropertyValues;

		/// <summary>
		/// Gets the property values associated with this workflow.
		/// Property values are loaded lazily in a single batch together with the property values of all nodes.
		/// Use <see cref="AddProperty"/>, <see cref="SetProperty"/> and <see cref="RemoveProperty"/> to modify them.
		/// </summary>
		public IReadOnlyCollection<PropertyValue> PropertyValues => PropertyValuesEditor.PropertyValues;

		internal StorageWorkflow.WorkflowsInstance OriginalInstance => originalInstance;

		private WorkflowPropertyValuesEditor PropertyValuesEditor
			=> propertyValuesEditor ??= new WorkflowPropertyValuesEditor(
				() => propertiesLoader?.GetCustomPropertyValues(Id.ToString()),
				() => propertiesLoader?.GetPropertyValues(Id.ToString()));

		/// <summary>
		/// Adds a custom property value to this workflow.
		/// </summary>
		/// <param name="value">The custom property value to add.</param>
		public void AddCustomProperty(CustomPropertyValue value) => PropertyValuesEditor.AddCustomProperty(value);

		/// <summary>
		/// Replaces the entire collection of custom property values associated with this workflow with the specified values.
		/// </summary>
		/// <param name="values">The custom property values that should replace the current collection.</param>
		public void SetCustomProperties(IEnumerable<CustomPropertyValue> values) => PropertyValuesEditor.SetCustomProperties(values);

		/// <summary>
		/// Removes the specified custom property value from this workflow.
		/// </summary>
		/// <param name="value">The custom property value to remove.</param>
		public void RemoveCustomProperty(CustomPropertyValue value) => PropertyValuesEditor.RemoveCustomProperty(value);

		/// <summary>
		/// Adds a property value to this workflow.
		/// </summary>
		/// <param name="value">The property value to add.</param>
		public void AddProperty(PropertyValue value) => PropertyValuesEditor.AddProperty(value);

		/// <summary>
		/// Replaces the entire collection of property values associated with this workflow with the specified values.
		/// </summary>
		/// <param name="values">The property values that should replace the current collection.</param>
		public void SetProperties(IEnumerable<PropertyValue> values) => PropertyValuesEditor.SetProperties(values);

		/// <summary>
		/// Removes the specified property value from this workflow.
		/// </summary>
		/// <param name="value">The property value to remove.</param>
		public void RemoveProperty(PropertyValue value) => PropertyValuesEditor.RemoveProperty(value);

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

			ParseNodesAndConnections(planApi, instance.Nodes, instance.Connections);
		}

		private void ParseNodesAndConnections(MediaOpsPlanApi planApi, ICollection<StorageWorkflow.NodesSection> nodes, ICollection<StorageWorkflow.ConnectionsSection> connections)
		{
			if (nodes == null || nodes.Count == 0)
			{
				NodeGraph = new NodeGraph<WorkflowNode>();
				return;
			}

			var parsedNodesById = new Dictionary<string, WorkflowNode>();
			foreach (var nodeSecion in nodes)
			{
				WorkflowNode node = null;
				switch (nodeSecion.NodeType.Value)
				{
					case StorageWorkflow.SlcWorkflowIds.Enums.Nodetype.Resource:
						node = new WorkflowResourceNode(planApi, nodeSecion);
						break;
					case StorageWorkflow.SlcWorkflowIds.Enums.Nodetype.ResourcePool:
						node = new WorkflowResourcePoolNode(planApi, nodeSecion);
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
				NodeGraph = new NodeGraph<WorkflowNode>(parsedNodesById.Values);
				return;
			}

			var parsedConnections = new List<NodeConnection<WorkflowNode>>();
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

			NodeGraph = new NodeGraph<WorkflowNode>(parsedNodesById.Values, parsedConnections);
		}
	}
}

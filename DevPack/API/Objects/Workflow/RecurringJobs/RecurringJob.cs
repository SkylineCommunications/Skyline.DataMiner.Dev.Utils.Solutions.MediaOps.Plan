namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a recurring job in MediaOps Plan.
	/// </summary>
	public class RecurringJob : ApiNamedObject
	{
		private RecurringJobsInstance originalInstance;
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
				hash = (hash * 23) + (OrchestrationSettings != null ? OrchestrationSettings.GetHashCode() : 0);
				hash = (hash * 23) + (NodeGraph != null ? NodeGraph.GetHashCode() : 0);

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
				&& Equals(OrchestrationSettings, other.OrchestrationSettings)
				&& Equals(NodeGraph, other.NodeGraph);
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
	}
}

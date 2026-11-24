namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	using DomPartialObject = Skyline.DataMiner.Net.Apps.ManagerStore.Select.PartialObject<Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstance, Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstanceId>;
	using Nodes = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.Nodes;
	using Nodetype = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Enums.Nodetype;

	/// <summary>
	/// Holds what is known about one workflow during a partial read: the selected fields of the workflow DOM instance
	/// and, when needed, the full workflow DOM instance. Values of sections that occur multiple times are returned by DOM
	/// as one list per field, so they are combined per section by position. When the lists don't line up (e.g. a field
	/// that isn't filled in on every section), the full workflow DOM instance is used instead.
	/// </summary>
	internal sealed class WorkflowPartialReadContext
	{
		public WorkflowPartialReadContext(DomPartialObject partialObject)
		{
			PartialObject = partialObject ?? throw new ArgumentNullException(nameof(partialObject));
		}

		public DomPartialObject PartialObject { get; }

		public Guid WorkflowId => PartialObject.Id.Id;

		/// <summary>
		/// Gets or sets the full workflow DOM instance, only read when the selected multi-section values can't be combined.
		/// </summary>
		public WorkflowsInstance FullInstance { get; set; }

		public bool CanCombineNodeReferences()
		{
			return GetValues<string>(Nodes.NodeReferenceID).Count == GetValues<int>(Nodes.NodeType).Count;
		}

		public bool CanCombineNodeParentReferences()
		{
			var types = GetValues<int>(Nodes.NodeType);
			var parentCount = GetValues<string>(Nodes.NodeParentReferenceID).Count;

			return CanCombineNodeReferences()
				&& (parentCount == types.Count || parentCount == types.Count(x => x == (int)Nodetype.Resource));
		}

		/// <summary>
		/// Gets the resources referenced by the resource nodes, as <see cref="WorkflowExposers.Resources"/> returns them.
		/// </summary>
		public IEnumerable<Guid> GetResourceIds()
		{
			return GetNodes(includeParentReferences: false)
				.Where(x => x.Type == Nodetype.Resource)
				.Select(x => x.Reference);
		}

		/// <summary>
		/// Gets the resource pools referenced by the nodes, as <see cref="WorkflowExposers.ResourcePools"/> returns them.
		/// </summary>
		public IEnumerable<Guid> GetResourcePoolIds()
		{
			return GetNodes(includeParentReferences: true)
				.Select(x => x.Type == Nodetype.Resource ? x.ParentReference : x.Reference)
				.Where(x => x != Guid.Empty);
		}

		private static Guid ParseReference(string value)
		{
			return !String.IsNullOrEmpty(value) && Guid.TryParse(value, out var id) ? id : Guid.Empty;
		}

		private static bool IsSupportedNodeType(Nodetype type)
		{
			// Workflow ignores nodes of other types when it parses its node graph.
			return type == Nodetype.Resource || type == Nodetype.ResourcePool;
		}

		private List<T> GetValues<T>(FieldDescriptorID descriptor)
		{
			return PartialObject.TryGetValues<T>(descriptor, out var values) && values != null ? values : new List<T>();
		}

		private IEnumerable<NodeValues> GetNodes(bool includeParentReferences)
		{
			if (FullInstance != null)
			{
				return FullInstance.Nodes
					.Where(x => x.NodeType.HasValue && IsSupportedNodeType(x.NodeType.Value))
					.Select(x => new NodeValues(x.NodeType.Value, x.ReferenceId, x.ParentReferenceId))
					.ToList();
			}

			var types = GetValues<int>(Nodes.NodeType);
			var references = GetValues<string>(Nodes.NodeReferenceID);
			var parentReferences = includeParentReferences ? GetValues<string>(Nodes.NodeParentReferenceID) : null;

			// Parent references are only stored on resource nodes, so when not every node has one they belong to the resource nodes in order.
			var parentPerNode = parentReferences != null && parentReferences.Count == types.Count;
			var nextParent = 0;

			var nodes = new List<NodeValues>(types.Count);
			for (int i = 0; i < types.Count; i++)
			{
				var type = (Nodetype)types[i];
				var parentReference = Guid.Empty;

				if (parentReferences != null && type == Nodetype.Resource)
				{
					parentReference = ParseReference(parentPerNode ? parentReferences[i] : parentReferences[nextParent++]);
				}

				if (!IsSupportedNodeType(type))
				{
					continue;
				}

				nodes.Add(new NodeValues(type, ParseReference(references[i]), parentReference));
			}

			return nodes;
		}

		private sealed class NodeValues
		{
			public NodeValues(Nodetype type, Guid reference, Guid parentReference)
			{
				Type = type;
				Reference = reference;
				ParentReference = parentReference;
			}

			public Nodetype Type { get; }

			public Guid Reference { get; }

			public Guid ParentReference { get; }
		}
	}
}

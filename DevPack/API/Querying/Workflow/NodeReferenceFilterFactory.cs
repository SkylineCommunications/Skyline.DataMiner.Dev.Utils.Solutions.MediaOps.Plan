namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying
{
	using System;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	internal static class NodeReferenceFilterFactory
	{
		/// <summary>
		/// Creates a filter on the presence of a node referencing the provided resource. Nodes are stored in a
		/// repeating section, so a resource is referenced when a nodes section of type resource holds the ID of the
		/// resource as its reference.
		/// </summary>
		public static FilterElement<DomInstance> CreateResourceFilter(Comparer comparer, Guid resourceId, string fieldName)
		{
			var containsNode = CreateNodeFilter(SlcWorkflowIds.Enums.Nodetype.Resource, SlcWorkflowIds.Sections.Nodes.NodeReferenceID, resourceId);

			return Combine(comparer, containsNode, fieldName);
		}

		/// <summary>
		/// Creates a filter on the presence of a node referencing the provided resource pool. Nodes are stored in a
		/// repeating section, so a resource pool is referenced when a nodes section of type resource pool holds the ID
		/// of the resource pool as its reference, or when a nodes section of type resource holds the ID of the resource
		/// pool the resource belongs to as its parent reference.
		/// </summary>
		public static FilterElement<DomInstance> CreateResourcePoolFilter(Comparer comparer, Guid resourcePoolId, string fieldName)
		{
			var containsNode = new ORFilterElement<DomInstance>(
				CreateNodeFilter(SlcWorkflowIds.Enums.Nodetype.ResourcePool, SlcWorkflowIds.Sections.Nodes.NodeReferenceID, resourcePoolId),
				CreateNodeFilter(SlcWorkflowIds.Enums.Nodetype.Resource, SlcWorkflowIds.Sections.Nodes.NodeParentReferenceID, resourcePoolId));

			return Combine(comparer, containsNode, fieldName);
		}

		private static FilterElement<DomInstance> CreateNodeFilter(SlcWorkflowIds.Enums.Nodetype nodeType, Net.Sections.FieldDescriptorID referenceField, Guid referenceId)
		{
			return new ANDFilterElement<DomInstance>(
				DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeType).Equal((int)nodeType),
				DomInstanceExposers.FieldValues.DomInstanceField(referenceField).Equal(referenceId.ToString()));
		}

		private static FilterElement<DomInstance> Combine(Comparer comparer, FilterElement<DomInstance> containsNode, string fieldName)
		{
			switch (comparer)
			{
				case Comparer.Contains:
					return containsNode;
				case Comparer.NotContains:
					return new NOTFilterElement<DomInstance>(containsNode);
				default:
					throw new NotSupportedException($"Comparer {comparer} is not supported for {fieldName} checks");
			}
		}
	}
}

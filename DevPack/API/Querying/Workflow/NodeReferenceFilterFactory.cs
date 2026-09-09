namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying
{
	using System;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	internal static class NodeReferenceFilterFactory
	{
		/// <summary>
		/// Creates a filter on the presence of a node referencing the provided object. Nodes are stored in a repeating
		/// section, so an object references the node when a nodes section of the provided type with the provided
		/// reference is present on the DOM instance.
		/// </summary>
		public static FilterElement<DomInstance> Create(SlcWorkflowIds.Enums.Nodetype nodeType, Comparer comparer, Guid referenceId, string fieldName)
		{
			var containsNode = new ANDFilterElement<DomInstance>(
				DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeType).Equal((int)nodeType),
				DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeReferenceID).Equal(referenceId.ToString()));

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

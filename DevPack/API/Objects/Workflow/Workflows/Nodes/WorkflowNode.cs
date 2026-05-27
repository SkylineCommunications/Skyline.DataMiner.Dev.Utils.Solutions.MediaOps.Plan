namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System.Collections.Generic;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Base class for nodes within workflows.
	/// </summary>
	public abstract class WorkflowNode : NodeBase
	{
		private static readonly IReadOnlyCollection<CustomPropertyValue> EmptyCustomValues = [];
		private static readonly IReadOnlyCollection<PropertyValue> EmptyPropertyValues = [];

		private WorkflowPropertiesLoader propertiesLoader;

		private protected WorkflowNode() : base()
		{
		}

		private protected WorkflowNode(MediaOpsPlanApi planApi, StorageWorkflow.NodesSection section) : base(planApi, section)
		{
		}

		/// <summary>
		/// Gets the custom property values associated with this node.
		/// Property values are loaded lazily in a single batch together with the property values of the workflow and all other nodes.
		/// </summary>
		public IReadOnlyCollection<CustomPropertyValue> CustomPropertyValues
			=> propertiesLoader?.GetCustomPropertyValues(Id) ?? EmptyCustomValues;

		/// <summary>
		/// Gets the property values associated with this node.
		/// Property values are loaded lazily in a single batch together with the property values of the workflow and all other nodes.
		/// </summary>
		public IReadOnlyCollection<PropertyValue> PropertyValues
			=> propertiesLoader?.GetPropertyValues(Id) ?? EmptyPropertyValues;

		/// <summary>
		/// Determines whether this node represents a resource and, if so, returns it as a <see cref="WorkflowResourceNode"/>.
		/// </summary>
		/// <param name="resourceNode">When this method returns, contains the current node as a <see cref="WorkflowResourceNode"/> when it represents a resource; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this node represents a resource; otherwise, <c>false</c>.</returns>
		public bool IsResourceNode(out WorkflowResourceNode resourceNode)
		{
			resourceNode = this as WorkflowResourceNode;
			return resourceNode != null;
		}

		/// <summary>
		/// Determines whether this node represents a resource pool and, if so, returns it as a <see cref="WorkflowResourcePoolNode"/>.
		/// </summary>
		/// <param name="resourcePoolNode">When this method returns, contains the current node as a <see cref="WorkflowResourcePoolNode"/> when it represents a resource pool; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this node represents a resource pool; otherwise, <c>false</c>.</returns>
		public bool IsResourcePoolNode(out WorkflowResourcePoolNode resourcePoolNode)
		{
			resourcePoolNode = this as WorkflowResourcePoolNode;
			return resourcePoolNode != null;
		}

		internal void SetPropertiesLoader(WorkflowPropertiesLoader loader)
		{
			propertiesLoader = loader;
		}
	}
}

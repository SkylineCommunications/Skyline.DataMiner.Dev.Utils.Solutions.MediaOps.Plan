namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Base class for nodes within workflows.
	/// </summary>
	public abstract class WorkflowNode : NodeBase
	{
		private PropertyValuesContext propertiesContext;
		private PropertyValuesScope propertyValuesScope;

		private protected WorkflowNode() : base()
		{
		}

		private protected WorkflowNode(MediaOpsPlanApi planApi, StorageWorkflow.NodesSection section) : base(planApi, section)
		{
		}

		/// <summary>
		/// Gets the custom property values associated with this node.
		/// Property values are loaded lazily in a single batch together with the property values of the workflow and all other nodes.
		/// Use <see cref="AddCustomProperty"/>, <see cref="SetCustomProperty"/> and <see cref="RemoveCustomProperty"/> to modify them.
		/// </summary>
		public IReadOnlyCollection<CustomPropertyValue> CustomPropertyValues => PropertyValuesScope.CustomPropertyValues;

		/// <summary>
		/// Gets the property values associated with this node.
		/// Property values are loaded lazily in a single batch together with the property values of the workflow and all other nodes.
		/// Use <see cref="AddProperty"/>, <see cref="SetProperty"/> and <see cref="RemoveProperty"/> to modify them.
		/// </summary>
		public IReadOnlyCollection<PropertyValue> PropertyValues => PropertyValuesScope.PropertyValues;

		private PropertyValuesScope PropertyValuesScope
			=> propertyValuesScope ??= new PropertyValuesScope(() => propertiesContext, Id);

		internal PropertyValuesScope PropertyValuesScopeOrNull => propertyValuesScope;

		/// <summary>
		/// Adds a custom property value to this node.
		/// </summary>
		/// <param name="value">The custom property value to add.</param>
		public void AddCustomProperty(CustomPropertyValue value) => PropertyValuesScope.AddCustomProperty(value);

		/// <summary>
		/// Replaces the entire collection of custom property values associated with this node with the specified values.
		/// </summary>
		/// <param name="values">The custom property values that should replace the current collection.</param>
		public void SetCustomProperties(IEnumerable<CustomPropertyValue> values) => PropertyValuesScope.SetCustomProperties(values);

		/// <summary>
		/// Removes the specified custom property value from this node.
		/// </summary>
		/// <param name="value">The custom property value to remove.</param>
		public void RemoveCustomProperty(CustomPropertyValue value) => PropertyValuesScope.RemoveCustomProperty(value);

		/// <summary>
		/// Adds a property value to this node.
		/// </summary>
		/// <param name="value">The property value to add.</param>
		public void AddProperty(PropertyValue value) => PropertyValuesScope.AddProperty(value);

		/// <summary>
		/// Replaces the entire collection of property values associated with this node with the specified values.
		/// </summary>
		/// <param name="values">The property values that should replace the current collection.</param>
		public void SetProperties(IEnumerable<PropertyValue> values) => PropertyValuesScope.SetProperties(values);

		/// <summary>
		/// Removes the specified property value from this node.
		/// </summary>
		/// <param name="value">The property value to remove.</param>
		public void RemoveProperty(PropertyValue value) => PropertyValuesScope.RemoveProperty(value);

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

		internal void SetPropertiesContext(PropertyValuesContext context)
		{
			propertiesContext = context;
		}
	}
}

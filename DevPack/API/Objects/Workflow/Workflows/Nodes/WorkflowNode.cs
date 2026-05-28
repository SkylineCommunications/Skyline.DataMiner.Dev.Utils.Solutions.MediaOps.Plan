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
		private WorkflowPropertiesLoader propertiesLoader;
		private WorkflowPropertyValuesEditor propertyValuesEditor;

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
		public IReadOnlyCollection<CustomPropertyValue> CustomPropertyValues => PropertyValuesEditor.CustomPropertyValues;

		/// <summary>
		/// Gets the property values associated with this node.
		/// Property values are loaded lazily in a single batch together with the property values of the workflow and all other nodes.
		/// Use <see cref="AddProperty"/>, <see cref="SetProperty"/> and <see cref="RemoveProperty"/> to modify them.
		/// </summary>
		public IReadOnlyCollection<PropertyValue> PropertyValues => PropertyValuesEditor.PropertyValues;

		private WorkflowPropertyValuesEditor PropertyValuesEditor
			=> propertyValuesEditor ??= new WorkflowPropertyValuesEditor(
				() => propertiesLoader?.GetCustomPropertyValues(Id),
				() => propertiesLoader?.GetPropertyValues(Id));

		/// <summary>
		/// Adds a new custom property value to this node.
		/// </summary>
		/// <param name="value">The custom property value to add.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when the name of <paramref name="value"/> is null or empty.</exception>
		/// <exception cref="InvalidOperationException">Thrown when a custom property value with the same name already exists.</exception>
		public void AddCustomProperty(CustomPropertyValue value) => PropertyValuesEditor.AddCustomProperty(value);

		/// <summary>
		/// Adds the specified custom property value or replaces an existing one with the same name.
		/// </summary>
		/// <param name="value">The custom property value to set.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when the name of <paramref name="value"/> is null or empty.</exception>
		public void SetCustomProperty(CustomPropertyValue value) => PropertyValuesEditor.SetCustomProperty(value);

		/// <summary>
		/// Removes the custom property value with the specified name.
		/// </summary>
		/// <param name="name">The name of the custom property value to remove.</param>
		/// <returns><see langword="true"/> when a matching custom property value was removed; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
		public bool RemoveCustomProperty(string name) => PropertyValuesEditor.RemoveCustomProperty(name);

		/// <summary>
		/// Adds a new property value to this node.
		/// </summary>
		/// <param name="value">The property value to add.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when a property value for the same property already exists.</exception>
		public void AddProperty(PropertyValue value) => PropertyValuesEditor.AddProperty(value);

		/// <summary>
		/// Adds the specified property value or replaces an existing one for the same property.
		/// </summary>
		/// <param name="value">The property value to set.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
		public void SetProperty(PropertyValue value) => PropertyValuesEditor.SetProperty(value);

		/// <summary>
		/// Removes the property value linked to the property with the specified identifier.
		/// </summary>
		/// <param name="propertyId">The identifier of the property whose value should be removed.</param>
		/// <returns><see langword="true"/> when a matching property value was removed; otherwise, <see langword="false"/>.</returns>
		public bool RemoveProperty(Guid propertyId) => PropertyValuesEditor.RemoveProperty(propertyId);

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

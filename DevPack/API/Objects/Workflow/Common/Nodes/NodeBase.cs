namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Base class for all node implementations in workflows, jobs, and recurring jobs.
	/// This class represents common node properties used across different contexts.
	/// </summary>
	public abstract class NodeBase : TrackableObject, INode
	{
		private StorageWorkflow.NodesSection originalSection;
		private StorageWorkflow.NodesSection updatedSection;

		private PropertySettingsContext propertiesContext;
		private PropertySettingsScope propertySettingsScope;

		private protected NodeBase() : base()
		{
			Id = Guid.NewGuid().ToString();

			IsNew = true;

			OrchestrationSettings = new WorkflowOrchestrationSettings();
		}

		private protected NodeBase(MediaOpsPlanApi planApi, StorageWorkflow.NodesSection section)
		{
			ParseSection(planApi, section);
		}

		/// <inheritdoc/>
		public string Id { get; private set; }

		/// <summary>
		/// Gets or sets the alias or display name of the node.
		/// </summary>
		public string Alias { get; set; }

		/// <summary>
		/// Gets or sets the icon of the node.
		/// </summary>
		public string IconImage { get; set; }

		/// <summary>
		/// Gets the orchestration settings assigned to this node.
		/// </summary>
		public OrchestrationSettings OrchestrationSettings { get; private set; }

		/// <summary>
		/// Gets the custom property settings associated with this node.
		/// Property settings are loaded lazily in a single batch together with the property settings of the owning object and all other nodes.
		/// Use <see cref="AddCustomProperty"/>, <see cref="SetCustomProperties"/> and <see cref="RemoveCustomProperty"/> to modify them.
		/// </summary>
		public IReadOnlyCollection<CustomPropertySetting> CustomPropertySettings => GetOrCreateScope().CustomPropertySettings;

		/// <summary>
		/// Gets the property settings associated with this node.
		/// Property settings are loaded lazily in a single batch together with the property settings of the owning object and all other nodes.
		/// Use <see cref="AddProperty"/>, <see cref="SetProperties"/> and <see cref="RemoveProperty"/> to modify them.
		/// </summary>
		public IReadOnlyCollection<PropertySetting> PropertySettings => GetOrCreateScope().PropertySettings;

		internal PropertySettingsScope PropertySettingsScope => propertySettingsScope;

		/// <summary>
		/// Adds a custom property setting to this node.
		/// </summary>
		/// <param name="setting">The custom property setting to add.</param>
		/// <returns>The current <see cref="NodeBase"/> instance.</returns>
		public NodeBase AddCustomProperty(CustomPropertySetting setting)
		{
			GetOrCreateScope().AddCustomProperty(setting);
			return this;
		}

		/// <summary>
		/// Replaces the entire collection of custom property settings associated with this node with the specified settings.
		/// </summary>
		/// <param name="settings">The custom property settings that should replace the current collection.</param>
		/// <returns>The current <see cref="NodeBase"/> instance.</returns>
		public NodeBase SetCustomProperties(IEnumerable<CustomPropertySetting> settings)
		{
			GetOrCreateScope().SetCustomProperties(settings);
			return this;
		}

		/// <summary>
		/// Removes the specified custom property setting from this node.
		/// </summary>
		/// <param name="setting">The custom property setting to remove.</param>
		/// <returns>The current <see cref="NodeBase"/> instance.</returns>
		public NodeBase RemoveCustomProperty(CustomPropertySetting setting)
		{
			GetOrCreateScope().RemoveCustomProperty(setting);
			return this;
		}

		/// <summary>
		/// Adds a property setting to this node.
		/// </summary>
		/// <param name="setting">The property setting to add.</param>
		/// <returns>The current <see cref="NodeBase"/> instance.</returns>
		public NodeBase AddProperty(PropertySetting setting)
		{
			GetOrCreateScope().AddProperty(setting);
			return this;
		}

		/// <summary>
		/// Replaces the entire collection of property settings associated with this node with the specified settings.
		/// </summary>
		/// <param name="settings">The property settings that should replace the current collection.</param>
		/// <returns>The current <see cref="NodeBase"/> instance.</returns>
		public NodeBase SetProperties(IEnumerable<PropertySetting> settings)
		{
			GetOrCreateScope().SetProperties(settings);
			return this;
		}

		/// <summary>
		/// Removes the specified property setting from this node.
		/// </summary>
		/// <param name="setting">The property setting to remove.</param>
		/// <returns>The current <see cref="NodeBase"/> instance.</returns>
		public NodeBase RemoveProperty(PropertySetting setting)
		{
			GetOrCreateScope().RemoveProperty(setting);
			return this;
		}

		/// <summary>
		/// Copies all property settings and custom property settings from the specified source node into this node,
		/// replacing this node's current settings.
		/// </summary>
		/// <remarks>
		/// This is a convenience helper to take over the properties of another node (for example the node being
		/// swapped out). It is intentionally generic so it can be reused for any node, not just swap scenarios.
		/// </remarks>
		/// <param name="source">The node whose properties should be copied.</param>
		/// <returns>The current <see cref="NodeBase"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
		public NodeBase CopyPropertiesFrom(NodeBase source)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			SetProperties(source.PropertySettings);
			SetCustomProperties(source.CustomPropertySettings.Select(setting => new CustomPropertySetting(setting)));

			return this;
		}

		/// <summary>
		/// Copies the orchestration settings from the specified source node into this node, retargeting any
		/// node-scoped <see cref="DataReference"/>s from the source node to this node.
		/// </summary>
		/// <remarks>
		/// This is a convenience helper to take over the orchestration settings of another node (for example the node
		/// being swapped out). It is intentionally generic so it can be reused for any node, not just swap scenarios.
		/// </remarks>
		/// <param name="source">The node whose orchestration settings should be copied.</param>
		/// <returns>The current <see cref="NodeBase"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
		public NodeBase CopyOrchestrationSettingsFrom(NodeBase source)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			var nodeIdMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				[source.Id] = Id,
			};

			OrchestrationSettingsCloner.Clone(source.OrchestrationSettings, OrchestrationSettings, nodeIdMap);

			return this;
		}

		internal void SetPropertiesContext(PropertySettingsContext context)
		{
			propertiesContext = context;
		}

		private PropertySettingsScope GetOrCreateScope()
			=> propertySettingsScope ??= new PropertySettingsScope(() => propertiesContext, Id);

		/// <summary>
		/// Determines whether this node represents a resource and, if so, returns it as an <see cref="IResourceNode"/>.
		/// </summary>
		/// <param name="resourceNode">When this method returns, contains the current node as an <see cref="IResourceNode"/> when it represents a resource; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this node represents a resource; otherwise, <c>false</c>.</returns>
		public bool IsResourceNode(out IResourceNode resourceNode)
		{
			resourceNode = this as IResourceNode;
			return resourceNode != null;
		}

		/// <summary>
		/// Determines whether this node represents a resource pool and, if so, returns it as an <see cref="IResourcePoolNode"/>.
		/// </summary>
		/// <param name="resourcePoolNode">When this method returns, contains the current node as an <see cref="IResourcePoolNode"/> when it represents a resource pool; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this node represents a resource pool; otherwise, <c>false</c>.</returns>
		public bool IsResourcePoolNode(out IResourcePoolNode resourcePoolNode)
		{
			resourcePoolNode = this as IResourcePoolNode;
			return resourcePoolNode != null;
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not NodeBase other)
			{
				return false;
			}

			return Id == other.Id
				&& Alias == other.Alias
				&& IconImage == other.IconImage
				&& OrchestrationSettings == other.OrchestrationSettings;
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 17;
				hash = (hash * 23) + Id.GetHashCode();
				hash = (hash * 23) + (Alias != null ? Alias.GetHashCode() : 0);
				hash = (hash * 23) + (IconImage != null ? IconImage.GetHashCode() : 0);
				hash = (hash * 23) + (OrchestrationSettings != null ? OrchestrationSettings.GetHashCode() : 0);

				return hash;
			}
		}

		/// <summary>
		/// Applies changes from this node to the specified storage section.
		/// </summary>
		/// <param name="section">The storage workflow nodes section to apply changes to.</param>
		internal abstract void ApplyChanges(StorageWorkflow.NodesSection section);

		/// <summary>
		/// Gets or creates a section with the current changes applied.
		/// </summary>
		/// <returns>A <see cref="StorageWorkflow.NodesSection"/> containing the current state of the node.</returns>
		internal StorageWorkflow.NodesSection GetSectionWithChanges()
		{
			if (updatedSection == null)
			{
				updatedSection = IsNew
					? new StorageWorkflow.NodesSection()
					{
						NodeID = Id,
					}
					: originalSection.Clone();
			}

			updatedSection.NodeAlias = Alias;
			updatedSection.NodeIcon = IconImage;

			updatedSection.NodeConfiguration = OrchestrationSettings.Id;

			// Default values until correctly implemented. This will prevent some job integration tests from failing as the DOM CRUD is still adding these values in the background.
			updatedSection.Hidden = false;
			updatedSection.Billable = false;
			updatedSection.NodeConfigurationExecutionOrder = 0;

			ApplyChanges(updatedSection);

			return updatedSection;
		}

		private void ParseSection(MediaOpsPlanApi planApi, StorageWorkflow.NodesSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			Id = section.NodeID;
			Alias = section.NodeAlias;
			IconImage = section.NodeIcon;

			if (section.NodeConfiguration == null || section.NodeConfiguration == Guid.Empty)
			{
				OrchestrationSettings = new WorkflowOrchestrationSettings();
			}
			else
			{
				var domConfiguration = planApi.DomHelpers.SlcWorkflowHelper.GetConfigurations([section.NodeConfiguration.Value]).FirstOrDefault();
				if (domConfiguration != null)
				{
					OrchestrationSettings = new WorkflowOrchestrationSettings(planApi, domConfiguration);
				}
				else
				{
					OrchestrationSettings = new WorkflowOrchestrationSettings();
				}
			}
		}
	}
}

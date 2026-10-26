namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

	using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

	/// <summary>
	/// Represents a resource pool in MediaOps Plan.
	/// </summary>
	public class ResourcePool : ApiNamedObject
	{
		private readonly List<LinkedResourcePool> linkedResourcepools = [];
		private readonly List<ResourcePoolCapabilitySetting> capabilitySettings = [];

		private StorageResourceStudio.ResourcepoolInstance originalInstance;
		private StorageResourceStudio.ResourcepoolInstance updatedInstance;

		private Guid coreResourcePoolId;

		/// <summary>
		/// Initializes a new instance of the <see cref="ResourcePool"/> class.
		/// </summary>
		public ResourcePool() : base()
		{
			IsNew = true;

			OrchestrationSettings = new ResourceStudioOrchestrationSettings();
			FlowRules = new FlowRules(Id);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ResourcePool"/> class with a specific resource pool ID.
		/// </summary>
		/// <param name="resourcePoolId">The unique identifier of the resource pool.</param>
		public ResourcePool(Guid resourcePoolId) : base(resourcePoolId)
		{
			IsNew = true;
			HasUserDefinedId = true;

			OrchestrationSettings = new ResourceStudioOrchestrationSettings();
			FlowRules = new FlowRules(Id);
		}

		internal ResourcePool(MediaOpsPlanApi planApi, StorageResourceStudio.ResourcepoolInstance instance, IReadOnlyDictionary<Guid, StorageResourceStudio.FlowrulesInstance> flowRulesById) : base(instance.ID.Id)
		{
			if (flowRulesById == null)
			{
				throw new ArgumentNullException(nameof(flowRulesById));
			}

			ParseInstance(planApi, instance);
			ParseFlowRules(instance, flowRulesById);
			InitTracking();
		}

		/// <summary>
		/// Gets or sets the name of the resource pool.
		/// </summary>
		public override string Name { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the resource pool is managed by an external system.
		/// </summary>
		public bool IsExternallyManaged { get; set; }

		/// <summary>
		/// Gets the state of the resource pool.
		/// </summary>
		public ResourcePoolState State { get; private set; }

		/// <summary>
		/// Gets or sets the icon of the resource pool.
		/// </summary>
		public string IconImage { get; set; }

		/// <summary>
		/// Gets or sets the URL of the resource pool.
		/// </summary>
		public string Url { get; set; }

		/// <summary>
		/// Gets or sets the unique identifier of the associated category.
		/// </summary>
		public string CategoryId { get; set; }

		/// <summary>
		/// Gets the collection of links associated with this resource pool.
		/// </summary>
		public IReadOnlyCollection<LinkedResourcePool> LinkedResourcePools => linkedResourcepools;

		/// <summary>
		/// Gets the collection of capabilities assigned to this resource pool.
		/// </summary>
		public IReadOnlyCollection<CapabilitySettings> Capabilities => capabilitySettings;

		/// <summary>
		/// Gets the orchestration settings assigned to this resource pool.
		/// </summary>
		public OrchestrationSettings OrchestrationSettings { get; private set; }

		/// <summary>
		/// Gets the flow rules of this resource pool, which define what may be connected before and after it. The flow rules are persisted together with the resource pool.
		/// </summary>
		public FlowRules FlowRules { get; private set; }

		internal Guid CoreResourcePoolId => coreResourcePoolId;

		internal StorageResourceStudio.ResourcepoolInstance OriginalInstance => originalInstance;

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + Id.GetHashCode();
				hash = (hash * 23) + (Name != null ? Name.GetHashCode() : 0);
				hash = (hash * 23) + IsExternallyManaged.GetHashCode();
				hash = (hash * 23) + (IconImage != null ? IconImage.GetHashCode() : 0);
				hash = (hash * 23) + (Url != null ? Url.GetHashCode() : 0);
				hash = (hash * 23) + (CategoryId != null ? CategoryId.GetHashCode() : 0);
				hash = (hash * 23) + State.GetHashCode();
				hash = (hash * 23) + OrchestrationSettings.GetHashCode();
				hash = (hash * 23) + FlowRules.GetHashCode();

				foreach (var linkedResourcePool in LinkedResourcePools.OrderBy(x => x.LinkedResourcePoolId).ToArray())
				{
					hash = (hash * 23) + linkedResourcePool.GetHashCode();
				}

				foreach (var setting in Capabilities.OrderBy(x => x.Id).ToArray())
				{
					hash = (hash * 23) + setting.GetHashCode();
				}

				return hash;
			}
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current ResourcePool instance.
		/// </summary>
		/// <remarks>Equality is determined by comparing the Id, Name, IsExternallyManaged, IconImage,
		/// Url, CategoryId, State, FlowRules, LinkedResourcePools, and Capabilities properties. Two ResourcePool instances are
		/// considered equal if all these properties are equal.</remarks>
		/// <param name="obj">The object to compare with the current ResourcePool instance.</param>
		/// <returns>true if the specified object is a ResourcePool and has the same values for all properties as the current
		/// instance; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			if (obj is not ResourcePool other)
			{
				return false;
			}

			return Id == other.Id &&
				   Name == other.Name &&
				   IsExternallyManaged == other.IsExternallyManaged &&
				   IconImage == other.IconImage &&
				   Url == other.Url &&
				   CategoryId == other.CategoryId &&
				   State == other.State &&
				   OrchestrationSettings == other.OrchestrationSettings &&
				   Equals(FlowRules, other.FlowRules) &&
				   LinkedResourcePools.SequenceEqual(other.LinkedResourcePools) &&
				   Capabilities.SequenceEqual(other.Capabilities);
		}

		/// <summary>
		/// Adds a link to another resource pool.
		/// </summary>
		/// <param name="linkedResourcePool">The resource pool link to add.</param>
		/// <returns>The current <see cref="ResourcePool"/> instance.</returns>
		public ResourcePool AddLinkedResourcePool(LinkedResourcePool linkedResourcePool)
		{
			if (linkedResourcePool == null)
			{
				throw new ArgumentNullException(nameof(linkedResourcePool));
			}

			if (!linkedResourcePool.IsNew)
			{
				return this;
			}

			linkedResourcepools.Add(linkedResourcePool);

			return this;
		}

		/// <summary>
		/// Removes the specified resource pool link from the collection, if it exists.
		/// </summary>
		/// <param name="linkedResourcePool">The resource pool link to remove.</param>
		/// <returns>The current <see cref="ResourcePool"/> instance.</returns>
		public ResourcePool RemoveLinkedResourcePool(LinkedResourcePool linkedResourcePool)
		{
			if (linkedResourcePool == null)
			{
				throw new ArgumentNullException(nameof(linkedResourcePool));
			}

			linkedResourcepools.Remove(linkedResourcePool);
			return this;
		}

		/// <summary>
		/// Adds a new capability to the resource pool.
		/// </summary>
		/// <param name="capabilitySetting">The capability setting to add.</param>
		/// <returns>The current <see cref="ResourcePool"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="capabilitySetting"/> is <see langword="null"/>.</exception>
		public ResourcePool AddCapability(CapabilitySettings capabilitySetting)
		{
			if (capabilitySetting == null)
			{
				throw new ArgumentNullException(nameof(capabilitySetting));
			}

			capabilitySettings.Add(new ResourcePoolCapabilitySetting(capabilitySetting));

			return this;
		}

		/// <summary>
		/// Removes the specified capability from the resource pool.
		/// </summary>
		/// <param name="capabilitySetting">The capability to remove from the resource pool. Cannot be null.</param>
		/// <returns>The current <see cref="ResourcePool"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="capabilitySetting"/> is <see langword="null"/>.</exception>
		public ResourcePool RemoveCapability(CapabilitySettings capabilitySetting)
		{
			if (capabilitySetting == null)
			{
				throw new ArgumentNullException(nameof(capabilitySetting));
			}

			capabilitySettings.RemoveAll(x => x.Equals(capabilitySetting));
			return this;
		}

		/// <summary>
		/// Sets the specified collection of capability settings on the resource pool.
		/// </summary>
		/// <param name="capabilitySettings">The capability settings to apply on the resource pool. Cannot be null.</param>
		/// <returns>Resource pool with updated capability settings.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="capabilitySettings"/> is <see langword="null"/>.</exception>
		public ResourcePool SetCapabilities(IEnumerable<CapabilitySettings> capabilitySettings)
		{
			if (capabilitySettings == null)
			{
				throw new ArgumentNullException(nameof(capabilitySettings));
			}

			var settings = capabilitySettings.ToList();

			this.capabilitySettings.Clear();
			foreach (var setting in settings)
			{
				AddCapability(setting);
			}

			return this;
		}

		internal ResourcePool RemoveLinkedResourcePool(ResourcePool resourcePool)
		{
			if (resourcePool == null)
			{
				throw new ArgumentNullException(nameof(resourcePool));
			}

			if (resourcePool.IsNew)
			{
				return this;
			}

			var toRemove = linkedResourcepools.Where(x => x.LinkedResourcePoolId == resourcePool.Id).ToList();
			if (toRemove.Count > 0)
			{
				foreach (var item in toRemove)
				{
					linkedResourcepools.Remove(item);
				}
			}

			return this;
		}

		internal StorageResourceStudio.ResourcepoolInstance GetInstanceWithChanges()
		{
			if (updatedInstance == null)
			{
				updatedInstance = IsNew ? new StorageResourceStudio.ResourcepoolInstance(Id) : originalInstance.Clone();
			}

			updatedInstance.ResourcePoolInfo.Name = Name;
			updatedInstance.ResourcePoolInfo.Category = CategoryId;
			updatedInstance.ResourcePoolOther.IconImage = IconImage;
			updatedInstance.ResourcePoolOther.URL = Url;

			updatedInstance.ConfigurationInfo.PoolConfiguration = OrchestrationSettings.Id;
			updatedInstance.ConfigurationInfo.FlowRules = FlowRules.Id;

			// Setting to null will not create a DOM section in storage.
			updatedInstance.ExternalMetadata.ExternallyManaged = IsExternallyManaged ? true : null;

			updatedInstance.ResourcePoolLinks.Clear();
			foreach (var link in linkedResourcepools)
			{
				updatedInstance.ResourcePoolLinks.Add(link.GetSectionWithChanges());
			}

			updatedInstance.ResourcePoolCapabilities.Clear();
			foreach (var capability in capabilitySettings)
			{
				updatedInstance.ResourcePoolCapabilities.Add(capability.GetSectionWithChanges());
			}

			return updatedInstance;
		}

		internal static IReadOnlyDictionary<Guid, StorageResourceStudio.FlowrulesInstance> LoadFlowRules(MediaOpsPlanApi planApi, IEnumerable<StorageResourceStudio.ResourcepoolInstance> instances)
		{
			if (planApi == null)
			{
				throw new ArgumentNullException(nameof(planApi));
			}

			if (instances == null)
			{
				throw new ArgumentNullException(nameof(instances));
			}

			var flowRulesIds = instances
				.Where(x => x != null)
				.Select(x => x.ConfigurationInfo.FlowRules)
				.Where(x => x.HasValue && x.Value != Guid.Empty)
				.Select(x => x.Value)
				.Distinct()
				.ToList();

			if (flowRulesIds.Count == 0)
			{
				return new Dictionary<Guid, StorageResourceStudio.FlowrulesInstance>();
			}

			return planApi.DomHelpers.SlcResourceStudioHelper.GetFlowRules(flowRulesIds).ToDictionary(x => x.ID.Id);
		}

		private void ParseFlowRules(StorageResourceStudio.ResourcepoolInstance instance, IReadOnlyDictionary<Guid, StorageResourceStudio.FlowrulesInstance> flowRulesById)
		{
			var flowRulesId = instance.ConfigurationInfo.FlowRules;
			if (flowRulesId.HasValue && flowRulesById.TryGetValue(flowRulesId.Value, out var flowRulesInstance))
			{
				FlowRules = new FlowRules(flowRulesInstance, Id);
			}
			else
			{
				// Pools created before flow rules existed get their rule set on their next save.
				FlowRules = new FlowRules(Id);
			}
		}

		private void ParseInstance(MediaOpsPlanApi planApi, StorageResourceStudio.ResourcepoolInstance instance)
		{
			this.originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

			Name = instance.ResourcePoolInfo.Name;
			CategoryId = instance.ResourcePoolInfo.Category;
			IsExternallyManaged = instance.ExternalMetadata?.ExternallyManaged ?? false;
			State = EnumExtensions.MapEnum<StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.StatusesEnum, ResourcePoolState>(instance.Status);
			coreResourcePoolId = instance.ResourcePoolInternalProperties.ResourcePoolId;

			IconImage = instance.ResourcePoolOther.IconImage;
			Url = instance.ResourcePoolOther.URL;

			foreach (var section in instance.ResourcePoolLinks)
			{
				var link = new LinkedResourcePool(section);
				if (link.LinkedResourcePoolId != Guid.Empty)
					linkedResourcepools.Add(link);
			}

			foreach (var section in instance.ResourcePoolCapabilities)
			{
				var capability = new ResourcePoolCapabilitySetting(section);
				capabilitySettings.Add(capability);
			}

			if (instance.ConfigurationInfo.PoolConfiguration == null || instance.ConfigurationInfo.PoolConfiguration == Guid.Empty)
			{
				OrchestrationSettings = new ResourceStudioOrchestrationSettings();
			}
			else
			{
				var domConfiguration = planApi.DomHelpers.SlcResourceStudioHelper.GetConfigurations([instance.ConfigurationInfo.PoolConfiguration.Value]).FirstOrDefault();
				if (domConfiguration != null)
				{
					OrchestrationSettings = new ResourceStudioOrchestrationSettings(planApi, domConfiguration);
				}
				else
				{
					OrchestrationSettings = new ResourceStudioOrchestrationSettings();
				}
			}
		}
	}
}

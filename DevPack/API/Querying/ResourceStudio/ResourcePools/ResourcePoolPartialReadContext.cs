namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

	using Capabilities = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolCapabilities;
	using DomPartialObject = Skyline.DataMiner.Net.Apps.ManagerStore.Select.PartialObject<Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstance, Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstanceId>;
	using Links = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolLinks;
	using Resourceselectiontype = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Resourceselectiontype;

	/// <summary>
	/// Holds what is known about one resource pool during a partial read: the selected fields of the resource pool DOM
	/// instance and, when needed, the full instance. Values of sections that occur multiple times (the pool links and
	/// the capabilities) are returned by DOM as one list per field, so they are combined per section by position. When
	/// the lists don't line up (e.g. a field that isn't filled in on every section), the full resource pool DOM instance
	/// is used instead.
	/// </summary>
	internal sealed class ResourcePoolPartialReadContext
	{
		public ResourcePoolPartialReadContext(DomPartialObject partialObject)
		{
			PartialObject = partialObject ?? throw new ArgumentNullException(nameof(partialObject));
		}

		public DomPartialObject PartialObject { get; }

		public Guid ResourcePoolId => PartialObject.Id.Id;

		/// <summary>
		/// Gets or sets the full resource pool DOM instance, only read when the selected multi-section values can't be combined.
		/// </summary>
		public ResourcepoolInstance FullInstance { get; set; }

		public bool CanCombineLinkSelectionTypes()
		{
			return GetValues<Guid>(Links.LinkedResourcePool).Count == GetValues<int>(Links.ResourceSelectionType).Count;
		}

		public bool CanCombineCapabilityIds()
		{
			// A capability section without a profile parameter is still a capability (with an empty ID) in a full read.
			return GetValues<string>(Capabilities.ProfileParameterID).Count >= GetValues<string>(Capabilities.StringValue).Count;
		}

		/// <summary>
		/// Gets the linked resource pools, as <see cref="ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId"/> returns them:
		/// links without a resource pool are ignored, the same way <see cref="ResourcePool"/> parses them.
		/// </summary>
		public IEnumerable<Guid> GetLinkedResourcePoolIds()
		{
			if (FullInstance != null)
			{
				return FullInstance.ResourcePoolLinks
					.Select(x => x.LinkedResourcePool ?? Guid.Empty)
					.Where(x => x != Guid.Empty)
					.ToList();
			}

			return GetValues<Guid>(Links.LinkedResourcePool).Where(x => x != Guid.Empty).ToList();
		}

		/// <summary>
		/// Gets the selection type of every link, as <see cref="ResourcePoolExposers.LinkedResourcePools.SelectionType"/> returns them:
		/// links without a resource pool are ignored and a missing selection type is <see cref="ResourceSelectionType.Manual"/>.
		/// </summary>
		public IEnumerable<ResourceSelectionType> GetLinkSelectionTypes()
		{
			if (FullInstance != null)
			{
				return FullInstance.ResourcePoolLinks
					.Where(x => (x.LinkedResourcePool ?? Guid.Empty) != Guid.Empty)
					.Select(x => MapSelectionType(x.ResourceSelectionType))
					.ToList();
			}

			var poolIds = GetValues<Guid>(Links.LinkedResourcePool);
			var types = GetValues<int>(Links.ResourceSelectionType);

			return poolIds
				.Select((poolId, i) => (PoolId: poolId, Type: (Resourceselectiontype?)types[i]))
				.Where(x => x.PoolId != Guid.Empty)
				.Select(x => MapSelectionType(x.Type))
				.ToList();
		}

		/// <summary>
		/// Gets the profile parameter of every capability, as <see cref="ResourcePoolExposers.Capabilities.CapabilityId"/> returns them.
		/// </summary>
		public IEnumerable<Guid> GetCapabilityIds()
		{
			if (FullInstance != null)
			{
				return FullInstance.ResourcePoolCapabilities.Select(x => x.ProfileParameterId).ToList();
			}

			return GetValues<string>(Capabilities.ProfileParameterID).Select(ParseId).ToList();
		}

		/// <summary>
		/// Gets the discrete values of all capabilities, in the order of the capabilities, as
		/// <see cref="ResourcePoolExposers.Capabilities.Discretes"/> returns them.
		/// </summary>
		public IEnumerable<string> GetCapabilityDiscretes()
		{
			// The discretes of every capability are stored as one value, so they don't need to be matched to their section.
			// A capability keeps its discretes in a set, so duplicates within one capability are dropped.
			return GetValues<string>(Capabilities.StringValue)
				.SelectMany(x => new ResourcePoolCapabilitiesSection { StringValue = x }.DiscreteValues.Distinct())
				.ToList();
		}

		private static Guid ParseId(string value)
		{
			return !String.IsNullOrEmpty(value) && Guid.TryParse(value, out var id) ? id : Guid.Empty;
		}

		private static ResourceSelectionType MapSelectionType(Resourceselectiontype? type)
		{
			return type.HasValue
				? EnumExtensions.MapEnum<Resourceselectiontype, ResourceSelectionType>(type.Value)
				: ResourceSelectionType.Manual;
		}

		private List<T> GetValues<T>(FieldDescriptorID descriptor)
		{
			return PartialObject.TryGetValues<T>(descriptor, out var values) && values != null ? values : new List<T>();
		}
	}
}

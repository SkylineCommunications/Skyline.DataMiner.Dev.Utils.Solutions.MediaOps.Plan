namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

	using Capabilities = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapabilities;
	using Capacities = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapacities;
	using DomPartialObject = Skyline.DataMiner.Net.Apps.ManagerStore.Select.PartialObject<Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstance, Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstanceId>;
	using Properties = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceProperties;
	using ResourceInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceInfo;
	using ResourceInternalProperties = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceInternalProperties;
	using ResourceType = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type;

	/// <summary>
	/// Holds what is known about one resource during a partial read: the selected fields of the resource DOM instance
	/// and, when the selected fields need them, the data stored outside that instance. Values of sections that occur
	/// multiple times are returned by DOM as one list per field, so they are combined per section by position. When the
	/// lists don't line up (e.g. a section without a profile parameter), the full resource DOM instance is used instead.
	/// </summary>
	internal sealed class ResourcePartialReadContext
	{
		private ResourceMetadata metadata;

		public ResourcePartialReadContext(DomPartialObject partialObject)
		{
			PartialObject = partialObject ?? throw new ArgumentNullException(nameof(partialObject));
		}

		public DomPartialObject PartialObject { get; }

		public Guid ResourceId => PartialObject.Id.Id;

		/// <summary>
		/// Gets the type of the resource, or <see langword="null"/> when a full read wouldn't return the resource because its type is missing or unknown.
		/// </summary>
		public ResourceType? DomType
		{
			get
			{
				if (!PartialObject.TryGetValue<int>(ResourceInfo.Type, out var value))
				{
					return null;
				}

				var type = (ResourceType)value;
				return Enum.IsDefined(typeof(ResourceType), type) ? (ResourceType?)type : null;
			}
		}

		/// <summary>
		/// Gets or sets the full resource DOM instance, only read when the selected multi-section values can't be combined.
		/// </summary>
		public ResourceInstance FullInstance { get; set; }

		/// <summary>
		/// Gets or sets the capacities that are range capacities, the other capacities are number capacities.
		/// </summary>
		public ISet<Guid> RangeCapacityIds { get; set; } = new HashSet<Guid>();

		private ResourceMetadata Metadata
		{
			get
			{
				if (metadata == null)
				{
					// Same as ResourceInternalPropertiesSection.Metadata: invalid or missing metadata results in empty metadata.
					var json = PartialObject.TryGetValue<string>(ResourceInternalProperties.ResourceMetadata, out var value) ? value : null;
					metadata = ResourceMetadata.TryDeserialize(json, out var parsed) ? parsed : new ResourceMetadata();
				}

				return metadata;
			}
		}

		/// <summary>
		/// Classifies the capacities of the resources in a single batch, so the capacity IDs can be ordered the same way
		/// <see cref="Resource.Capacities"/> does: the number capacities first, followed by the range capacities.
		/// </summary>
		/// <param name="readRangeCapacityIds">Returns which of the given capacities are range capacities. Only called when the resources have capacities.</param>
		/// <param name="contexts">The resources to classify the capacities of.</param>
		public static void LoadCapacityTypes(Func<IReadOnlyCollection<Guid>, IEnumerable<Guid>> readRangeCapacityIds, IReadOnlyCollection<ResourcePartialReadContext> contexts)
		{
			var capacityIds = contexts
				.SelectMany(x => x.GetCapacitySectionIds())
				.Where(x => x != Guid.Empty)
				.Distinct()
				.ToList();

			if (capacityIds.Count == 0)
			{
				return;
			}

			var rangeCapacityIds = new HashSet<Guid>(readRangeCapacityIds(capacityIds));

			foreach (var context in contexts)
			{
				context.RangeCapacityIds = rangeCapacityIds;
			}
		}

		public static System.Type ToApiType(ResourceType type)
		{
			switch (type)
			{
				case ResourceType.Unmanaged:
					return typeof(UnmanagedResource);
				case ResourceType.Element:
					return typeof(ElementResource);
				case ResourceType.Service:
					return typeof(ServiceResource);
				case ResourceType.VirtualFunction:
					return typeof(VirtualFunctionResource);
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported resource type.");
			}
		}

		public bool CanCombineCapabilities()
		{
			// Every capability section created by the API has a string value, so fewer profile parameters means a section without one.
			return GetValues<string>(Capabilities.ProfileParameterID).Count >= GetValues<string>(Capabilities.StringValue).Count;
		}

		public bool CanCombineCapacities()
		{
			var idCount = GetValues<string>(Capacities.ProfileParameterID).Count;

			return idCount >= GetValues<double>(Capacities.DoubleMaxValue).Count
				&& idCount >= GetValues<double>(Capacities.DoubleMinValue).Count;
		}

		public bool CanCombineProperties()
		{
			return GetValues<Guid>(Properties.Property).Count >= GetValues<string>(Properties.PropertyValue).Count;
		}

		public IEnumerable<Guid> GetResourcePoolIds()
		{
			// Same as ResourceInternalPropertiesSection.PoolIds, kept unique like Resource.ResourcePoolIds.
			if (!PartialObject.TryGetValue<string>(ResourceInternalProperties.Pool_Ids, out var value) || String.IsNullOrWhiteSpace(value))
			{
				return Enumerable.Empty<Guid>();
			}

			return value.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).Distinct().ToList();
		}

		/// <summary>
		/// Gets the capability of every capability section, as <see cref="ResourceExposers.Capabilities.CapabilityId"/> returns them.
		/// </summary>
		public IEnumerable<Guid> GetCapabilityIds()
		{
			if (FullInstance != null)
			{
				return FullInstance.ResourceCapabilities.Select(x => x.ProfileParameterId).ToList();
			}

			return GetValues<string>(Capabilities.ProfileParameterID).Select(ParseProfileParameterId).ToList();
		}

		/// <summary>
		/// Gets the discrete values of all capability sections, in the order of the sections. The values of a section
		/// are kept unique, the same way <see cref="CapabilitySettings.Discretes"/> does.
		/// </summary>
		public IEnumerable<string> GetCapabilityDiscretes()
		{
			// Sections without a string value have no discretes, so the values don't need to be combined with other fields.
			return GetValues<string>(Capabilities.StringValue)
				.SelectMany(x => SplitDiscretes(x).Distinct())
				.ToList();
		}

		/// <summary>
		/// Gets the capacity of every capacity section, as <see cref="ResourceExposers.Capacities.CapacityId"/> returns them.
		/// </summary>
		public IEnumerable<Guid> GetCapacityIds()
		{
			var ids = GetCapacitySectionIds();

			return ids.Where(x => !RangeCapacityIds.Contains(x))
				.Concat(ids.Where(x => RangeCapacityIds.Contains(x)))
				.ToList();
		}

		/// <summary>
		/// Gets the property of every property section, as <see cref="ResourceExposers.Properties.PropertyId"/> returns them.
		/// </summary>
		public IEnumerable<Guid> GetPropertyIds()
		{
			if (FullInstance != null)
			{
				return FullInstance.ResourceProperties.Select(x => x.Property ?? Guid.Empty).ToList();
			}

			return GetValues<Guid>(Properties.Property);
		}

		/// <summary>
		/// Gets the property values, as <see cref="ResourceExposers.Properties.Value"/> returns them: sections without a value are skipped.
		/// </summary>
		public IEnumerable<string> GetPropertyValues()
		{
			return GetValues<string>(Properties.PropertyValue);
		}

		/// <summary>
		/// Gets the agent and element of an element resource the same way <see cref="ElementResource"/> parses them.
		/// </summary>
		public (int AgentId, int ElementId) GetElementResourceElement()
		{
			if (!String.IsNullOrEmpty(Metadata.LinkedElementInfo))
			{
				var elementId = new DmsElementId(Metadata.LinkedElementInfo);
				return (elementId.AgentId, elementId.ElementId);
			}

			if (PartialObject.TryGetValue<string>(ResourceInfo.Element, out var element) && !String.IsNullOrEmpty(element))
			{
				var elementId = new DmsElementId(element);
				return (elementId.AgentId, elementId.ElementId);
			}

			return (0, 0);
		}

		/// <summary>
		/// Gets the agent and service of a service resource the same way <see cref="ServiceResource"/> parses them.
		/// </summary>
		public (int AgentId, int ServiceId) GetServiceResourceService()
		{
			if (!String.IsNullOrWhiteSpace(Metadata.LinkedServiceInfo))
			{
				var serviceId = new DmsServiceId(Metadata.LinkedServiceInfo);
				return (serviceId.AgentId, serviceId.ServiceId);
			}

			return (0, 0);
		}

		/// <summary>
		/// Gets the agent and element of a virtual function resource the same way <see cref="VirtualFunctionResource"/> parses them.
		/// </summary>
		public (int AgentId, int ElementId) GetVirtualFunctionResourceElement()
		{
			if (!String.IsNullOrWhiteSpace(Metadata.LinkedElementInfo))
			{
				var elementId = new DmsElementId(Metadata.LinkedElementInfo);
				return (elementId.AgentId, elementId.ElementId);
			}

			return (0, 0);
		}

		public Guid GetVirtualFunctionId()
		{
			return Metadata.LinkedFunctionId;
		}

		public string GetVirtualFunctionTableIndex()
		{
			return Metadata.LinkedFunctionTableIndex;
		}

		private static Guid ParseProfileParameterId(string value)
		{
			// Same as ResourceCapabilitiesSection.ProfileParameterId and ResourceCapacitiesSection.ProfileParameterId.
			return !String.IsNullOrEmpty(value) && Guid.TryParse(value, out var id) ? id : Guid.Empty;
		}

		private static IEnumerable<string> SplitDiscretes(string value)
		{
			// Same as ResourceCapabilitiesSection.DiscreteValues.
			return String.IsNullOrWhiteSpace(value)
				? Enumerable.Empty<string>()
				: value.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
		}

		private List<Guid> GetCapacitySectionIds()
		{
			if (FullInstance != null)
			{
				return FullInstance.ResourceCapacities.Select(x => x.ProfileParameterId).ToList();
			}

			return GetValues<string>(Capacities.ProfileParameterID).Select(ParseProfileParameterId).ToList();
		}

		private List<T> GetValues<T>(FieldDescriptorID descriptor)
		{
			return PartialObject.TryGetValues<T>(descriptor, out var values) && values != null ? values : new List<T>();
		}
	}
}

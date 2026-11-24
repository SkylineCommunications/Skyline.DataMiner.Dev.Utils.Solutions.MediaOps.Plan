namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.ManagerStore.Select;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

	using Capabilities = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapabilities;
	using Capacities = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapacities;
	using DomPartialObject = Skyline.DataMiner.Net.Apps.ManagerStore.Select.PartialObject<Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstance, Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstanceId>;
	using Properties = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceProperties;
	using ResourceInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceInfo;
	using ResourceInternalProperties = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceInternalProperties;
	using ResourceType = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type;

	/// <summary>
	/// Maps the <see cref="ResourceExposers"/>, <see cref="ElementResourceExposers"/>, <see cref="ServiceResourceExposers"/>
	/// and <see cref="VirtualFunctionResourceExposers"/> that can be selected in a partial read to the data they are read
	/// from, and converts that data the same way <see cref="Resource"/> and its subclasses do when they parse a full DOM
	/// instance. Fields of sections that occur multiple times return a list of values.
	/// </summary>
	/// <remarks>
	/// The type of the resource is always read: a full read skips resources without a (known) type, so a partial read
	/// skips them as well, and the type specific fields return the same default value as their exposer for other types.
	/// </remarks>
	internal sealed class ResourcePartialFieldMapper : IDomPartialFieldMapper<Resource>
	{
		private const int NotOfType = -1;

		private ResourcePartialFieldMapper()
		{
		}

		public static ResourcePartialFieldMapper Instance { get; } = new ResourcePartialFieldMapper();

		private static readonly Dictionary<string, PartialField> Fields = new Dictionary<string, PartialField>
		{
			[ResourceExposers.Id.fieldName] = PartialField.Single(x => x.ResourceId),
			[ResourceExposers.Name.fieldName] = PartialField.Single(x => ReadString(x.PartialObject, ResourceInfo.Name), ResourceInfo.Name),
			[ResourceExposers.IsFavorite.fieldName] = PartialField.Single(x => x.PartialObject.TryGetValue<bool>(ResourceInfo.Favorite, out var value) && value, ResourceInfo.Favorite),
			[ResourceExposers.Concurrency.fieldName] = PartialField.Single(x => x.PartialObject.TryGetValue<long>(ResourceInfo.Concurrency, out var value) ? (int)value : 1, ResourceInfo.Concurrency),
			[ResourceExposers.State.fieldName] = new PartialField(x => ToValues(ReadState(x.PartialObject))) { SelectsStatus = true },
			[ResourceExposers.ResourcePoolIds.fieldName] = new PartialField(x => x.GetResourcePoolIds().Cast<object>(), ResourceInternalProperties.Pool_Ids),
			[ResourceExposers.Type.fieldName] = PartialField.Single(x => ResourcePartialReadContext.ToApiType(x.DomType.Value)),

			[ResourceExposers.Capabilities.CapabilityId.fieldName] = new PartialField(x => x.GetCapabilityIds().Cast<object>(), Capabilities.ProfileParameterID, Capabilities.StringValue)
			{
				CanCombineSections = x => x.CanCombineCapabilities(),
			},
			[ResourceExposers.Capabilities.Discretes.fieldName] = new PartialField(x => x.GetCapabilityDiscretes(), Capabilities.StringValue),
			[ResourceExposers.Capacities.CapacityId.fieldName] = new PartialField(x => x.GetCapacityIds().Cast<object>(), Capacities.ProfileParameterID, Capacities.DoubleMaxValue, Capacities.DoubleMinValue)
			{
				CanCombineSections = x => x.CanCombineCapacities(),
				RequiresCapacities = true,
			},
			[ResourceExposers.Properties.PropertyId.fieldName] = new PartialField(x => x.GetPropertyIds().Cast<object>(), Properties.Property, Properties.PropertyValue)
			{
				CanCombineSections = x => x.CanCombineProperties(),
			},
			[ResourceExposers.Properties.Value.fieldName] = new PartialField(x => x.GetPropertyValues(), Properties.PropertyValue),

			[ElementResourceExposers.AgentId.fieldName] = PartialField.ForType(ResourceType.Element, x => x.GetElementResourceElement().AgentId, NotOfType, ResourceInternalProperties.ResourceMetadata, ResourceInfo.Element),
			[ElementResourceExposers.ElementId.fieldName] = PartialField.ForType(ResourceType.Element, x => x.GetElementResourceElement().ElementId, NotOfType, ResourceInternalProperties.ResourceMetadata, ResourceInfo.Element),
			[ServiceResourceExposers.AgentId.fieldName] = PartialField.ForType(ResourceType.Service, x => x.GetServiceResourceService().AgentId, NotOfType, ResourceInternalProperties.ResourceMetadata),
			[ServiceResourceExposers.ServiceId.fieldName] = PartialField.ForType(ResourceType.Service, x => x.GetServiceResourceService().ServiceId, NotOfType, ResourceInternalProperties.ResourceMetadata),
			[VirtualFunctionResourceExposers.AgentId.fieldName] = PartialField.ForType(ResourceType.VirtualFunction, x => x.GetVirtualFunctionResourceElement().AgentId, NotOfType, ResourceInternalProperties.ResourceMetadata),
			[VirtualFunctionResourceExposers.ElementId.fieldName] = PartialField.ForType(ResourceType.VirtualFunction, x => x.GetVirtualFunctionResourceElement().ElementId, NotOfType, ResourceInternalProperties.ResourceMetadata),
			[VirtualFunctionResourceExposers.FunctionId.fieldName] = PartialField.ForType(ResourceType.VirtualFunction, x => x.GetVirtualFunctionId(), Guid.Empty, ResourceInternalProperties.ResourceMetadata),
			[VirtualFunctionResourceExposers.FunctionTableIndex.fieldName] = PartialField.ForType(ResourceType.VirtualFunction, x => x.GetVirtualFunctionTableIndex(), null, ResourceInternalProperties.ResourceMetadata),
		};

		/// <summary>
		/// Gets the names of the fields that can be selected.
		/// </summary>
		public static IReadOnlyCollection<string> SupportedFieldNames => Fields.Keys;

		/// <summary>
		/// Creates the DOM field selection needed to read the specified resource fields from the resource DOM instances.
		/// </summary>
		/// <exception cref="NotSupportedException">Thrown when one of the fields can't be read partially.</exception>
		public static SelectedFields<DomInstance> CreateSelectedFields(IEnumerable<FieldExposer> fields)
		{
			if (fields == null)
			{
				throw new ArgumentNullException(nameof(fields));
			}

			var selectedFields = new SelectedFields<DomInstance>();
			var selectsStatus = false;

			// The type is always needed: it determines whether a full read returns the resource.
			var descriptorIds = new HashSet<Guid> { ResourceInfo.Type.Id };
			selectedFields.Add(ResourceInfo.Type);

			foreach (var field in GetFields(fields).Select(x => x.Field))
			{
				selectsStatus |= field.SelectsStatus;

				foreach (var descriptor in field.Descriptors.Where(x => descriptorIds.Add(x.Id)))
				{
					selectedFields.Add(descriptor);
				}
			}

			if (selectsStatus)
			{
				selectedFields.Add(DomInstanceExposers.StatusId);
			}

			return selectedFields;
		}

		/// <summary>
		/// Converts partial resource DOM instances into partial resources containing the specified fields. Data stored
		/// outside the resource DOM instances is read once for all of them. Resources that a full read skips, because
		/// their type is missing or unknown, are skipped as well.
		/// </summary>
		/// <param name="planApi">The API used to read data stored outside the resource DOM instances. Only required when such fields are selected.</param>
		/// <param name="domPartialObjects">The partial resource DOM instances, read with <see cref="CreateSelectedFields"/>.</param>
		/// <param name="fields">The fields to return.</param>
		/// <exception cref="NotSupportedException">Thrown when one of the fields can't be read partially.</exception>
		public static IReadOnlyList<Skyline.DataMiner.SDM.PartialObject<Resource>> CreatePartialResources(MediaOpsPlanApi planApi, IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields)
		{
			return CreatePartialResources(
				planApi,
				domPartialObjects,
				fields,
				capacityIds => RequireApi(planApi).Capacities.Read(capacityIds).OfType<RangeCapacity>().Select(x => x.Id));
		}

		/// <summary>
		/// Converts partial resource DOM instances into partial resources, using <paramref name="readRangeCapacityIds"/> to
		/// determine which capacities are range capacities.
		/// </summary>
		internal static IReadOnlyList<Skyline.DataMiner.SDM.PartialObject<Resource>> CreatePartialResources(MediaOpsPlanApi planApi, IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields, Func<IReadOnlyCollection<Guid>, IEnumerable<Guid>> readRangeCapacityIds)
		{
			if (domPartialObjects == null)
			{
				throw new ArgumentNullException(nameof(domPartialObjects));
			}

			if (fields == null)
			{
				throw new ArgumentNullException(nameof(fields));
			}

			if (readRangeCapacityIds == null)
			{
				throw new ArgumentNullException(nameof(readRangeCapacityIds));
			}

			var selected = GetFields(fields).ToList();
			var contexts = domPartialObjects
				.Select(x => new ResourcePartialReadContext(x))
				.Where(x => x.DomType.HasValue)
				.ToList();

			if (contexts.Count == 0)
			{
				return Array.Empty<Skyline.DataMiner.SDM.PartialObject<Resource>>();
			}

			LoadFullInstancesWhereNeeded(planApi, contexts, selected.Select(x => x.Field).ToList());

			if (selected.Any(x => x.Field.RequiresCapacities))
			{
				ResourcePartialReadContext.LoadCapacityTypes(readRangeCapacityIds, contexts);
			}

			return contexts
				.Select(context => new Skyline.DataMiner.SDM.PartialObject<Resource>(
					context.ResourceId,
					selected.Select(x => new KeyValuePair<FieldExposer, IEnumerable<object>>(x.Exposer, x.Field.Read(context).ToList()))))
				.ToList();
		}

		SelectedFields<DomInstance> IDomPartialFieldMapper<Resource>.CreateSelectedFields(IEnumerable<FieldExposer> fields)
		{
			return CreateSelectedFields(fields);
		}

		IReadOnlyList<Skyline.DataMiner.SDM.PartialObject<Resource>> IDomPartialFieldMapper<Resource>.CreatePartialObjects(MediaOpsPlanApi planApi, IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields)
		{
			return CreatePartialResources(planApi, domPartialObjects, fields);
		}

		private static void LoadFullInstancesWhereNeeded(MediaOpsPlanApi planApi, IReadOnlyCollection<ResourcePartialReadContext> contexts, IReadOnlyCollection<PartialField> fields)
		{
			var checks = fields.Where(x => x.CanCombineSections != null).Select(x => x.CanCombineSections).ToList();
			if (checks.Count == 0)
			{
				return;
			}

			var toRead = contexts.Where(context => checks.Any(canCombine => !canCombine(context))).ToList();
			if (toRead.Count == 0)
			{
				return;
			}

			var instancesById = RequireApi(planApi).DomHelpers.SlcResourceStudioHelper.GetResources(toRead.Select(x => x.ResourceId).ToList())
				.ToDictionary(x => x.ID.Id);

			foreach (var context in toRead)
			{
				if (instancesById.TryGetValue(context.ResourceId, out var instance))
				{
					context.FullInstance = instance;
				}
			}
		}

		private static MediaOpsPlanApi RequireApi(MediaOpsPlanApi planApi)
		{
			return planApi ?? throw new InvalidOperationException("The selected fields require data that is stored outside the resource DOM instance.");
		}

		private static IEnumerable<(FieldExposer Exposer, PartialField Field)> GetFields(IEnumerable<FieldExposer> fields)
		{
			var seen = new HashSet<FieldExposer>(FieldExposerComparer.Instance);

			foreach (var exposer in fields)
			{
				if (exposer == null || !seen.Add(exposer))
				{
					continue;
				}

				if (exposer.FilterType != typeof(Resource) || !Fields.TryGetValue(exposer.fieldName, out var field))
				{
					throw new NotSupportedException($"Field '{exposer.fieldName}' can't be read partially. Supported fields: {String.Join(", ", Fields.Keys)}.");
				}

				yield return (exposer, field);
			}
		}

		private static IEnumerable<object> ToValues(object value)
		{
			return value == null ? Enumerable.Empty<object>() : new[] { value };
		}

		private static object ReadString(DomPartialObject partialObject, FieldDescriptorID descriptor)
		{
			return partialObject.TryGetValue<string>(descriptor, out var value) ? value : null;
		}

		private static object ReadState(DomPartialObject partialObject)
		{
			if (!partialObject.TryGetValue(DomInstanceExposers.StatusId, out var statusId) || String.IsNullOrEmpty(statusId))
			{
				return null;
			}

			return EnumExtensions.MapEnum<SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum, ResourceState>(SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.ToEnum(statusId));
		}

		private sealed class PartialField
		{
			public PartialField(Func<ResourcePartialReadContext, IEnumerable<object>> read, params FieldDescriptorID[] descriptors)
			{
				Read = read;
				Descriptors = descriptors;
			}

			public Func<ResourcePartialReadContext, IEnumerable<object>> Read { get; }

			public IReadOnlyCollection<FieldDescriptorID> Descriptors { get; }

			public bool SelectsStatus { get; set; }

			public bool RequiresCapacities { get; set; }

			/// <summary>
			/// Gets or sets a check whether the values of the sections that occur multiple times can be combined per section.
			/// When it fails for a resource, the field is read from the full resource DOM instance.
			/// </summary>
			public Func<ResourcePartialReadContext, bool> CanCombineSections { get; set; }

			public static PartialField Single(Func<ResourcePartialReadContext, object> read, params FieldDescriptorID[] descriptors)
			{
				return new PartialField(x => ToValues(read(x)), descriptors);
			}

			/// <summary>
			/// Creates a field of a resource subclass, which returns <paramref name="defaultValue"/> for resources of another type, like its exposer does.
			/// </summary>
			public static PartialField ForType(ResourceType type, Func<ResourcePartialReadContext, object> read, object defaultValue, params FieldDescriptorID[] descriptors)
			{
				return Single(x => x.DomType == type ? read(x) : defaultValue, descriptors);
			}
		}
	}
}

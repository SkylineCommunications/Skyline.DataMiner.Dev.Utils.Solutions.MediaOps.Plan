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

	using Capabilities = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolCapabilities;
	using DomPartialObject = Skyline.DataMiner.Net.Apps.ManagerStore.Select.PartialObject<Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstance, Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstanceId>;
	using Links = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolLinks;
	using ResourcePoolInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInfo;
	using ResourcePoolOther = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolOther;

	/// <summary>
	/// Maps the <see cref="ResourcePoolExposers"/> that can be selected in a partial read to the DOM fields they are read
	/// from, and converts those values the same way <see cref="ResourcePool"/> does when it parses a full DOM instance.
	/// Fields of the pool links and the capabilities, sections that occur multiple times, return a list of values.
	/// </summary>
	internal sealed class ResourcePoolPartialFieldMapper : IDomPartialFieldMapper<ResourcePool>
	{
		private ResourcePoolPartialFieldMapper()
		{
		}

		public static ResourcePoolPartialFieldMapper Instance { get; } = new ResourcePoolPartialFieldMapper();

		private static readonly Dictionary<string, PartialField> Fields = new Dictionary<string, PartialField>
		{
			[ResourcePoolExposers.Id.fieldName] = PartialField.Single(x => x.Id.Id),
			[ResourcePoolExposers.Name.fieldName] = PartialField.ForString(ResourcePoolInfo.Name),
			[ResourcePoolExposers.State.fieldName] = new PartialField(x => ToValues(ReadState(x.PartialObject))) { SelectsStatus = true },
			[ResourcePoolExposers.IconImage.fieldName] = PartialField.ForString(ResourcePoolOther.IconImage),
			[ResourcePoolExposers.HasIconImage.fieldName] = PartialField.HasValue(ResourcePoolOther.IconImage),
			[ResourcePoolExposers.Url.fieldName] = PartialField.ForString(ResourcePoolOther.URL),
			[ResourcePoolExposers.HasUrl.fieldName] = PartialField.HasValue(ResourcePoolOther.URL),
			[ResourcePoolExposers.CategoryId.fieldName] = PartialField.ForString(ResourcePoolInfo.Category),
			[ResourcePoolExposers.HasCategory.fieldName] = PartialField.HasValue(ResourcePoolInfo.Category),

			[ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId.fieldName] = new PartialField(x => x.GetLinkedResourcePoolIds().Cast<object>(), Links.LinkedResourcePool),
			[ResourcePoolExposers.LinkedResourcePools.SelectionType.fieldName] = new PartialField(x => x.GetLinkSelectionTypes().Cast<object>(), Links.LinkedResourcePool, Links.ResourceSelectionType)
			{
				CanCombineSections = x => x.CanCombineLinkSelectionTypes(),
			},
			[ResourcePoolExposers.Capabilities.CapabilityId.fieldName] = new PartialField(x => x.GetCapabilityIds().Cast<object>(), Capabilities.ProfileParameterID, Capabilities.StringValue)
			{
				CanCombineSections = x => x.CanCombineCapabilityIds(),
			},
			[ResourcePoolExposers.Capabilities.Discretes.fieldName] = new PartialField(x => x.GetCapabilityDiscretes().Cast<object>(), Capabilities.StringValue),
		};

		/// <summary>
		/// Gets the names of the fields that can be selected.
		/// </summary>
		public static IReadOnlyCollection<string> SupportedFieldNames => Fields.Keys;

		/// <summary>
		/// Creates the DOM field selection needed to read the specified resource pool fields from the resource pool DOM instances.
		/// </summary>
		/// <exception cref="NotSupportedException">Thrown when one of the fields can't be read partially.</exception>
		public static SelectedFields<DomInstance> CreateSelectedFields(IEnumerable<FieldExposer> fields)
		{
			if (fields == null)
			{
				throw new ArgumentNullException(nameof(fields));
			}

			var selectedFields = new SelectedFields<DomInstance>();
			var descriptorIds = new HashSet<Guid>();
			var selectsStatus = false;

			foreach (var field in GetFields(fields).Select(x => x.Field))
			{
				selectsStatus |= field.SelectsStatus;

				foreach (var descriptor in field.Descriptors.Where(x => descriptorIds.Add(x.Id)))
				{
					selectedFields.Add(descriptor);
				}
			}

			if (selectsStatus || descriptorIds.Count == 0)
			{
				// Also selected when only the ID is requested, so the read never selects no fields at all.
				selectedFields.Add(DomInstanceExposers.StatusId);
			}

			return selectedFields;
		}

		/// <summary>
		/// Converts partial resource pool DOM instances into partial resource pools containing the specified fields.
		/// </summary>
		/// <param name="planApi">The API used to read the full DOM instances of the resource pools of which the multi-section values can't be combined. Only required in that case.</param>
		/// <param name="domPartialObjects">The partial resource pool DOM instances, read with <see cref="CreateSelectedFields"/>.</param>
		/// <param name="fields">The fields to return.</param>
		/// <exception cref="NotSupportedException">Thrown when one of the fields can't be read partially.</exception>
		public static IReadOnlyList<Skyline.DataMiner.SDM.PartialObject<ResourcePool>> CreatePartialResourcePools(MediaOpsPlanApi planApi, IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields)
		{
			if (domPartialObjects == null)
			{
				throw new ArgumentNullException(nameof(domPartialObjects));
			}

			if (fields == null)
			{
				throw new ArgumentNullException(nameof(fields));
			}

			var selected = GetFields(fields).ToList();
			var contexts = domPartialObjects.Select(x => new ResourcePoolPartialReadContext(x)).ToList();

			if (contexts.Count == 0)
			{
				return Array.Empty<Skyline.DataMiner.SDM.PartialObject<ResourcePool>>();
			}

			LoadFullInstancesWhereNeeded(planApi, contexts, selected.Select(x => x.Field).ToList());

			return contexts
				.Select(context => new Skyline.DataMiner.SDM.PartialObject<ResourcePool>(
					context.ResourcePoolId,
					selected.Select(x => new KeyValuePair<FieldExposer, IEnumerable<object>>(x.Exposer, x.Field.Read(context).ToList()))))
				.ToList();
		}

		SelectedFields<DomInstance> IDomPartialFieldMapper<ResourcePool>.CreateSelectedFields(IEnumerable<FieldExposer> fields)
		{
			return CreateSelectedFields(fields);
		}

		IReadOnlyList<Skyline.DataMiner.SDM.PartialObject<ResourcePool>> IDomPartialFieldMapper<ResourcePool>.CreatePartialObjects(MediaOpsPlanApi planApi, IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields)
		{
			return CreatePartialResourcePools(planApi, domPartialObjects, fields);
		}

		private static void LoadFullInstancesWhereNeeded(MediaOpsPlanApi planApi, IReadOnlyCollection<ResourcePoolPartialReadContext> contexts, IReadOnlyCollection<PartialField> fields)
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

			var instancesById = RequireApi(planApi).DomHelpers.SlcResourceStudioHelper.GetResourcePools(toRead.Select(x => x.ResourcePoolId).ToList())
				.GroupBy(x => x.ID.Id)
				.ToDictionary(x => x.Key, x => x.First());

			foreach (var context in toRead)
			{
				if (instancesById.TryGetValue(context.ResourcePoolId, out var instance))
				{
					context.FullInstance = instance;
				}
			}
		}

		private static MediaOpsPlanApi RequireApi(MediaOpsPlanApi planApi)
		{
			return planApi ?? throw new InvalidOperationException("The selected fields require the full resource pool DOM instance.");
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

				if (exposer.FilterType != typeof(ResourcePool) || !Fields.TryGetValue(exposer.fieldName, out var field))
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

			return EnumExtensions.MapEnum<SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.StatusesEnum, ResourcePoolState>(SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Statuses.ToEnum(statusId));
		}

		private sealed class PartialField
		{
			public PartialField(Func<ResourcePoolPartialReadContext, IEnumerable<object>> read, params FieldDescriptorID[] descriptors)
			{
				Read = read;
				Descriptors = descriptors;
			}

			public Func<ResourcePoolPartialReadContext, IEnumerable<object>> Read { get; }

			public IReadOnlyCollection<FieldDescriptorID> Descriptors { get; }

			public bool SelectsStatus { get; set; }

			/// <summary>
			/// Gets or sets a check whether the values of the sections that occur multiple times can be combined per section.
			/// When it fails for a resource pool, the field is read from the full resource pool DOM instance.
			/// </summary>
			public Func<ResourcePoolPartialReadContext, bool> CanCombineSections { get; set; }

			public static PartialField Single(Func<DomPartialObject, object> read, params FieldDescriptorID[] descriptors)
			{
				return new PartialField(x => ToValues(read(x.PartialObject)), descriptors);
			}

			public static PartialField ForString(FieldDescriptorID descriptor)
			{
				return Single(x => ReadString(x, descriptor), descriptor);
			}

			/// <summary>
			/// Creates a field that tells whether a value is stored, the same way the filter on it checks whether the field exists.
			/// </summary>
			public static PartialField HasValue(FieldDescriptorID descriptor)
			{
				return Single(x => ReadString(x, descriptor) != null, descriptor);
			}
		}
	}
}

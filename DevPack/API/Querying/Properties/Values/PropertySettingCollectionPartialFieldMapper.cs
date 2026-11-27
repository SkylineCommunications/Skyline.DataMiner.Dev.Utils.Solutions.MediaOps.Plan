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

	using DomPartialObject = Skyline.DataMiner.Net.Apps.ManagerStore.Select.PartialObject<Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstance, Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstanceId>;
	using PropertyValue = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties.SlcPropertiesIds.Sections.PropertyValue;
	using PropertyValueInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties.SlcPropertiesIds.Sections.PropertyValueInfo;

	/// <summary>
	/// Maps the <see cref="PropertySettingCollectionExposers"/> that can be selected in a partial read to the DOM fields
	/// they are read from, and converts those values the same way <see cref="PropertySettingCollection"/> does when it
	/// parses a full DOM instance. Fields of the settings, which are stored in a section per setting, return a list of
	/// values. The type of a setting is defined by its property, so those fields read the properties once per batch.
	/// </summary>
	internal sealed class PropertySettingCollectionPartialFieldMapper : IDomPartialFieldMapper<PropertySettingCollection>
	{
		private static readonly Dictionary<string, PartialField> Fields = new Dictionary<string, PartialField>
		{
			[PropertySettingCollectionExposers.Id.fieldName] = PartialField.Single(x => x.Id.Id),
			[PropertySettingCollectionExposers.LinkedObjectId.fieldName] = PartialField.ForString(PropertyValueInfo.LinkedObjectID),
			[PropertySettingCollectionExposers.Scope.fieldName] = PartialField.ForString(PropertyValueInfo.Scope),
			[PropertySettingCollectionExposers.SubId.fieldName] = PartialField.ForString(PropertyValueInfo.SubID),

			[PropertySettingCollectionExposers.CustomPropertySettings.Name.fieldName] = PartialField.Collection(x => x.GetCustomNames(), PropertyValue.PropertyID, PropertyValue.PropertyName)
				.WithCanCombineSections(x => x.CanCombineCustomNames()),
			[PropertySettingCollectionExposers.CustomPropertySettings.Value.fieldName] = PartialField.Collection(x => x.GetCustomValues(), PropertyValue.PropertyID, PropertyValue.PropertyName, PropertyValue.Value)
				.WithCanCombineSections(x => x.CanCombineCustomValues()),
			[PropertySettingCollectionExposers.PropertySettings.PropertyId.fieldName] = PartialField.Collection(x => x.GetPropertyIds(), PropertyValue.PropertyID)
				.WithRequiresProperties(),
			[PropertySettingCollectionExposers.StringPropertySettings.Value.fieldName] = PartialField.ForPropertyValues(x => x.GetStringValues()),
			[PropertySettingCollectionExposers.BooleanPropertySettings.Value.fieldName] = PartialField.ForPropertyValues(x => x.GetBooleanValues()),
			[PropertySettingCollectionExposers.DiscretePropertySettings.Value.fieldName] = PartialField.ForPropertyValues(x => x.GetDiscreteValues()),
		};

		private PropertySettingCollectionPartialFieldMapper()
		{
		}

		public static PropertySettingCollectionPartialFieldMapper Instance { get; } = new PropertySettingCollectionPartialFieldMapper();

		/// <summary>
		/// Gets the names of the fields that can be selected.
		/// </summary>
		public static IReadOnlyCollection<string> SupportedFieldNames => Fields.Keys;

		/// <summary>
		/// Creates the DOM field selection needed to read the specified fields from the property setting collection DOM instances.
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

			foreach (var field in GetFields(fields).Select(x => x.Field))
			{
				foreach (var descriptor in field.Descriptors.Where(x => descriptorIds.Add(x.Id)))
				{
					selectedFields.Add(descriptor);
				}
			}

			if (descriptorIds.Count == 0)
			{
				// Selected when only the ID is requested, so the read never selects no fields at all.
				selectedFields.Add(DomInstanceExposers.StatusId);
			}

			return selectedFields;
		}

		/// <summary>
		/// Converts partial property setting collection DOM instances into partial collections containing the specified fields.
		/// </summary>
		/// <param name="planApi">
		/// The API used to read the properties the settings refer to and the full DOM instances of the collections of which the
		/// setting values can't be combined. Only required when such fields are selected.
		/// </param>
		/// <param name="domPartialObjects">The partial collection DOM instances, read with <see cref="CreateSelectedFields"/>.</param>
		/// <param name="fields">The fields to return.</param>
		/// <exception cref="NotSupportedException">Thrown when one of the fields can't be read partially.</exception>
		public static IReadOnlyList<Skyline.DataMiner.SDM.PartialObject<PropertySettingCollection>> CreatePartialCollections(MediaOpsPlanApi planApi, IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields)
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
			var contexts = domPartialObjects.Select(x => new PropertySettingCollectionPartialReadContext(x)).ToList();

			if (contexts.Count == 0)
			{
				return Array.Empty<Skyline.DataMiner.SDM.PartialObject<PropertySettingCollection>>();
			}

			LoadFullInstancesWhereNeeded(planApi, contexts, selected.Select(x => x.Field).ToList());

			if (selected.Any(x => x.Field.RequiresProperties))
			{
				PropertySettingCollectionPartialReadContext.LoadProperties(RequireApi(planApi), contexts);
			}

			return contexts
				.Select(context =>
				{
					var builder = Skyline.DataMiner.SDM.PartialObject<PropertySettingCollection>.CreateBuilder();
					foreach (var (exposer, field) in selected)
					{
						field.Include(builder, exposer, context);
					}

					return builder.Build();
				})
				.ToList();
		}

		SelectedFields<DomInstance> IDomPartialFieldMapper<PropertySettingCollection>.CreateSelectedFields(IEnumerable<FieldExposer> fields)
		{
			return CreateSelectedFields(fields);
		}

		IReadOnlyList<Skyline.DataMiner.SDM.PartialObject<PropertySettingCollection>> IDomPartialFieldMapper<PropertySettingCollection>.CreatePartialObjects(MediaOpsPlanApi planApi, IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields)
		{
			return CreatePartialCollections(planApi, domPartialObjects, fields);
		}

		private static void LoadFullInstancesWhereNeeded(MediaOpsPlanApi planApi, IReadOnlyCollection<PropertySettingCollectionPartialReadContext> contexts, IReadOnlyCollection<PartialField> fields)
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

			var instancesById = RequireApi(planApi).DomHelpers.SlcPropertiesHelper.GetPropertyValues(toRead.Select(x => x.CollectionId).ToList())
				.GroupBy(x => x.ID.Id)
				.ToDictionary(x => x.Key, x => x.First());

			foreach (var context in toRead)
			{
				if (instancesById.TryGetValue(context.CollectionId, out var instance))
				{
					context.FullInstance = instance;
				}
			}
		}

		private static MediaOpsPlanApi RequireApi(MediaOpsPlanApi planApi)
		{
			return planApi ?? throw new InvalidOperationException("The selected fields require data that isn't part of the selected property setting collection DOM fields.");
		}

		private static IEnumerable<(FieldExposer Exposer, PartialField Field)> GetFields(IEnumerable<FieldExposer> fields)
		{
			// Partial objects look up values by exposer instance, so the instances themselves are de-duplicated.
			var seen = new HashSet<FieldExposer>();

			foreach (var exposer in fields)
			{
				if (exposer == null || !seen.Add(exposer))
				{
					continue;
				}

				if (exposer.FilterType != typeof(PropertySettingCollection) || !Fields.TryGetValue(exposer.fieldName, out var field) || !field.ExposerType.IsInstanceOfType(exposer))
				{
					throw new NotSupportedException($"Field '{exposer.fieldName}' can't be read partially. Supported fields: {String.Join(", ", Fields.Keys)}.");
				}

				yield return (exposer, field);
			}
		}

		private static string ReadString(DomPartialObject partialObject, FieldDescriptorID descriptor)
		{
			return partialObject.TryGetValue<string>(descriptor, out var value) ? value : null;
		}

		private sealed class PartialField
		{
			private readonly Action<Skyline.DataMiner.SDM.PartialObject<PropertySettingCollection>.Builder, FieldExposer, PropertySettingCollectionPartialReadContext> include;

			private PartialField(Type exposerType, Action<Skyline.DataMiner.SDM.PartialObject<PropertySettingCollection>.Builder, FieldExposer, PropertySettingCollectionPartialReadContext> include, FieldDescriptorID[] descriptors)
			{
				ExposerType = exposerType;
				this.include = include;
				Descriptors = descriptors;
			}

			/// <summary>
			/// Gets the type the selected exposer must have, so the value can be included with the exposer's field type.
			/// </summary>
			public Type ExposerType { get; }

			public IReadOnlyCollection<FieldDescriptorID> Descriptors { get; }

			public bool RequiresProperties { get; private set; }

			/// <summary>
			/// Gets a check whether the values of the setting sections can be combined per section.
			/// When it fails for a collection, the field is read from the full collection DOM instance.
			/// </summary>
			public Func<PropertySettingCollectionPartialReadContext, bool> CanCombineSections { get; private set; }

			public static PartialField Scalar<TField>(Func<PropertySettingCollectionPartialReadContext, TField> read, params FieldDescriptorID[] descriptors)
			{
				return new PartialField(
					typeof(Exposer<PropertySettingCollection, TField>),
					(builder, exposer, context) => builder.Include((Exposer<PropertySettingCollection, TField>)exposer, read(context)),
					descriptors);
			}

			public static PartialField Single<TField>(Func<DomPartialObject, TField> read, params FieldDescriptorID[] descriptors)
			{
				return Scalar(x => read(x.PartialObject), descriptors);
			}

			public static PartialField ForString(FieldDescriptorID descriptor)
			{
				return Single(x => ReadString(x, descriptor), descriptor);
			}

			public static PartialField Collection<TField>(Func<PropertySettingCollectionPartialReadContext, IEnumerable<TField>> read, params FieldDescriptorID[] descriptors)
			{
				return new PartialField(
					typeof(DynamicListExposer<PropertySettingCollection, TField>),
					(builder, exposer, context) => builder.Include((DynamicListExposer<PropertySettingCollection, TField>)exposer, read(context).ToList()),
					descriptors);
			}

			/// <summary>
			/// Creates a field holding the values of the settings of one property type. The property name is selected to know
			/// whether there are custom settings, of which the values are in the same list.
			/// </summary>
			public static PartialField ForPropertyValues<TField>(Func<PropertySettingCollectionPartialReadContext, IEnumerable<TField>> read)
			{
				return Collection(read, PropertyValue.PropertyID, PropertyValue.PropertyName, PropertyValue.Value)
					.WithRequiresProperties()
					.WithCanCombineSections(x => x.CanCombinePropertyValues());
			}

			public PartialField WithRequiresProperties()
			{
				RequiresProperties = true;
				return this;
			}

			public PartialField WithCanCombineSections(Func<PropertySettingCollectionPartialReadContext, bool> canCombineSections)
			{
				CanCombineSections = canCombineSections;
				return this;
			}

			public void Include(Skyline.DataMiner.SDM.PartialObject<PropertySettingCollection>.Builder builder, FieldExposer exposer, PropertySettingCollectionPartialReadContext context)
			{
				include(builder, exposer, context);
			}
		}
	}
}

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
	using Layout = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties.SlcPropertiesIds.Sections.Layout;
	using PropertyInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties.SlcPropertiesIds.Sections.PropertyInfo;
	using Propertytype = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties.SlcPropertiesIds.Enums.Propertytype;

	/// <summary>
	/// Maps the <see cref="PropertyExposers"/> that can be selected in a partial read to the DOM fields they are read
	/// from, and converts those values the same way <see cref="Property"/> does when it parses a full DOM instance.
	/// Every exposed field is stored in the single property info or layout section and is shared by all property types,
	/// so every field holds one value. The property type is always read, because a full read skips instances without a
	/// property type and fails on an unknown one.
	/// </summary>
	internal sealed class PropertyPartialFieldMapper : IDomPartialFieldMapper<Property>
	{
		private static readonly Dictionary<string, PartialField> Fields = new Dictionary<string, PartialField>
		{
			[PropertyExposers.Id.fieldName] = PartialField.Create(x => x.Id.Id),
			[PropertyExposers.Name.fieldName] = PartialField.ForString(PropertyInfo.Name),
			[PropertyExposers.Scope.fieldName] = PartialField.ForString(PropertyInfo.Scope),
			[PropertyExposers.SectionName.fieldName] = PartialField.ForString(Layout.SectionName),
			[PropertyExposers.Order.fieldName] = PartialField.Create(x => x.TryGetValue<long>(Layout.Order, out var value) ? (int)value : 0, Layout.Order),
		};

		private PropertyPartialFieldMapper()
		{
		}

		public static PropertyPartialFieldMapper Instance { get; } = new PropertyPartialFieldMapper();

		/// <summary>
		/// Gets the names of the fields that can be selected.
		/// </summary>
		public static IReadOnlyCollection<string> SupportedFieldNames => Fields.Keys;

		/// <summary>
		/// Creates the DOM field selection needed to read the specified property fields from the property DOM instances.
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

			var descriptors = new[] { PropertyInfo.PropertyType }
				.Concat(GetFields(fields).Select(x => x.Field.Descriptor).Where(x => x != null));

			foreach (var descriptor in descriptors.Where(x => descriptorIds.Add(x.Id)))
			{
				selectedFields.Add(descriptor);
			}

			return selectedFields;
		}

		/// <summary>
		/// Converts partial property DOM instances into partial properties containing the specified fields. Instances that
		/// a full read skips, because they don't have a property type, are skipped as well.
		/// </summary>
		/// <param name="domPartialObjects">The partial property DOM instances, read with <see cref="CreateSelectedFields"/>.</param>
		/// <param name="fields">The fields to return.</param>
		/// <exception cref="NotSupportedException">Thrown when one of the fields can't be read partially.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when an instance has an unknown property type, like a full read.</exception>
		public static IReadOnlyList<Skyline.DataMiner.SDM.PartialObject<Property>> CreatePartialProperties(IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields)
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

			return domPartialObjects
				.Where(HasPropertyType)
				.Select(domPartialObject =>
				{
					var builder = Skyline.DataMiner.SDM.PartialObject<Property>.CreateBuilder();
					foreach (var (exposer, field) in selected)
					{
						field.Include(builder, exposer, domPartialObject);
					}

					return builder.Build();
				})
				.ToList();
		}

		SelectedFields<DomInstance> IDomPartialFieldMapper<Property>.CreateSelectedFields(IEnumerable<FieldExposer> fields)
		{
			return CreateSelectedFields(fields);
		}

		IReadOnlyList<Skyline.DataMiner.SDM.PartialObject<Property>> IDomPartialFieldMapper<Property>.CreatePartialObjects(MediaOpsPlanApi planApi, IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields)
		{
			return CreatePartialProperties(domPartialObjects, fields);
		}

		private static bool HasPropertyType(DomPartialObject domPartialObject)
		{
			if (!domPartialObject.TryGetValue<string>(PropertyInfo.PropertyType, out var value) || value == null)
			{
				return false;
			}

			// Same conversion as the full read, which creates the concrete property type from it.
			Propertytype.ToEnum(value);
			return true;
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

				if (exposer.FilterType != typeof(Property) || !Fields.TryGetValue(exposer.fieldName, out var field) || !field.ExposerType.IsInstanceOfType(exposer))
				{
					throw new NotSupportedException($"Field '{exposer.fieldName}' can't be read partially. Supported fields: {String.Join(", ", Fields.Keys)}.");
				}

				yield return (exposer, field);
			}
		}

		private sealed class PartialField
		{
			private readonly Action<Skyline.DataMiner.SDM.PartialObject<Property>.Builder, FieldExposer, DomPartialObject> include;

			private PartialField(Type exposerType, Action<Skyline.DataMiner.SDM.PartialObject<Property>.Builder, FieldExposer, DomPartialObject> include, FieldDescriptorID descriptor)
			{
				ExposerType = exposerType;
				this.include = include;
				Descriptor = descriptor;
			}

			/// <summary>
			/// Gets the type the selected exposer must have, so the value can be included with the exposer's field type.
			/// </summary>
			public Type ExposerType { get; }

			public FieldDescriptorID Descriptor { get; }

			public static PartialField Create<TField>(Func<DomPartialObject, TField> read, FieldDescriptorID descriptor = null)
			{
				return new PartialField(
					typeof(Exposer<Property, TField>),
					(builder, exposer, domPartialObject) => builder.Include((Exposer<Property, TField>)exposer, read(domPartialObject)),
					descriptor);
			}

			public static PartialField ForString(FieldDescriptorID descriptor)
			{
				return Create(x => x.TryGetValue<string>(descriptor, out var value) ? value : null, descriptor);
			}

			public void Include(Skyline.DataMiner.SDM.PartialObject<Property>.Builder builder, FieldExposer exposer, DomPartialObject domPartialObject)
			{
				include(builder, exposer, domPartialObject);
			}
		}
	}
}

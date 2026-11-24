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
			[PropertyExposers.Id.fieldName] = new PartialField(x => x.Id.Id),
			[PropertyExposers.Name.fieldName] = PartialField.ForString(PropertyInfo.Name),
			[PropertyExposers.Scope.fieldName] = PartialField.ForString(PropertyInfo.Scope),
			[PropertyExposers.SectionName.fieldName] = PartialField.ForString(Layout.SectionName),
			[PropertyExposers.Order.fieldName] = new PartialField(x => x.TryGetValue<long>(Layout.Order, out var value) ? (int)value : 0, Layout.Order),
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
				.Select(domPartialObject => new Skyline.DataMiner.SDM.PartialObject<Property>(
					domPartialObject.Id.Id,
					selected.Select(x => new KeyValuePair<FieldExposer, IEnumerable<object>>(x.Exposer, ToValues(x.Field.Read(domPartialObject))))))
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
			var seen = new HashSet<FieldExposer>(FieldExposerComparer.Instance);

			foreach (var exposer in fields)
			{
				if (exposer == null || !seen.Add(exposer))
				{
					continue;
				}

				if (exposer.FilterType != typeof(Property) || !Fields.TryGetValue(exposer.fieldName, out var field))
				{
					throw new NotSupportedException($"Field '{exposer.fieldName}' can't be read partially. Supported fields: {String.Join(", ", Fields.Keys)}.");
				}

				yield return (exposer, field);
			}
		}

		private static IReadOnlyList<object> ToValues(object value)
		{
			return value == null ? Array.Empty<object>() : new[] { value };
		}

		private sealed class PartialField
		{
			public PartialField(Func<DomPartialObject, object> read, FieldDescriptorID descriptor = null)
			{
				Read = read;
				Descriptor = descriptor;
			}

			public Func<DomPartialObject, object> Read { get; }

			public FieldDescriptorID Descriptor { get; }

			public static PartialField ForString(FieldDescriptorID descriptor)
			{
				return new PartialField(x => x.TryGetValue<string>(descriptor, out var value) ? value : null, descriptor);
			}
		}
	}
}

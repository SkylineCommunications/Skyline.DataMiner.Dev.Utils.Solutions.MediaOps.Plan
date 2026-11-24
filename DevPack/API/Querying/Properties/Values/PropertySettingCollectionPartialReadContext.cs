namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties;

	using DomPartialObject = Skyline.DataMiner.Net.Apps.ManagerStore.Select.PartialObject<Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstance, Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstanceId>;
	using PropertyValue = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties.SlcPropertiesIds.Sections.PropertyValue;

	/// <summary>
	/// Holds what is known about one property setting collection during a partial read: the selected fields of the
	/// collection DOM instance and, when needed, the full instance and the properties its settings refer to. Every
	/// setting is stored in its own property value section, which DOM returns as one list per field, so the values are
	/// combined per section by position. That is only possible when the lists line up: custom settings are the
	/// sections without a property ID (and always have a name), so the values can only be matched to their setting when
	/// all sections are custom, when no section is custom and every setting has a value, or when no section has a
	/// value. Otherwise the full collection DOM instance is used instead.
	/// </summary>
	internal sealed class PropertySettingCollectionPartialReadContext
	{
		private static readonly IReadOnlyDictionary<Guid, Property> NoProperties = new Dictionary<Guid, Property>();

		public PropertySettingCollectionPartialReadContext(DomPartialObject partialObject)
		{
			PartialObject = partialObject ?? throw new ArgumentNullException(nameof(partialObject));
		}

		public DomPartialObject PartialObject { get; }

		public Guid CollectionId => PartialObject.Id.Id;

		/// <summary>
		/// Gets or sets the full collection DOM instance, only read when the selected multi-section values can't be combined.
		/// </summary>
		public PropertyValuesInstance FullInstance { get; set; }

		/// <summary>
		/// Gets or sets the properties the settings refer to, which define the type of each setting.
		/// </summary>
		public IReadOnlyDictionary<Guid, Property> PropertiesById { get; set; } = NoProperties;

		/// <summary>
		/// Reads the properties the settings of the collections refer to in a single batch.
		/// </summary>
		public static void LoadProperties(MediaOpsPlanApi planApi, IReadOnlyCollection<PropertySettingCollectionPartialReadContext> contexts)
		{
			var propertyIds = contexts.SelectMany(x => x.GetPropertySections().Select(s => s.PropertyId)).Distinct().ToList();
			if (propertyIds.Count == 0)
			{
				return;
			}

			var propertiesById = planApi.Properties.Read(propertyIds)
				.GroupBy(x => x.Id)
				.ToDictionary(x => x.Key, x => x.First());

			foreach (var context in contexts)
			{
				context.PropertiesById = propertiesById;
			}
		}

		public bool CanCombineCustomNames()
		{
			return GetValues<Guid>(PropertyValue.PropertyID).Count == 0 || GetValues<string>(PropertyValue.PropertyName).Count == 0;
		}

		public bool CanCombineCustomValues()
		{
			var propertyIdCount = GetValues<Guid>(PropertyValue.PropertyID).Count;
			var valueCount = GetValues<string>(PropertyValue.Value).Count;

			return propertyIdCount == 0
				|| valueCount == 0
				|| (GetValues<string>(PropertyValue.PropertyName).Count == 0 && propertyIdCount == valueCount);
		}

		public bool CanCombinePropertyValues()
		{
			var valueCount = GetValues<string>(PropertyValue.Value).Count;

			return valueCount == 0
				|| (GetValues<string>(PropertyValue.PropertyName).Count == 0 && GetValues<Guid>(PropertyValue.PropertyID).Count == valueCount);
		}

		/// <summary>
		/// Gets the names of the custom settings, as <see cref="PropertySettingCollectionExposers.CustomPropertySettings.Name"/> returns them.
		/// </summary>
		public IEnumerable<string> GetCustomNames()
		{
			if (FullInstance != null)
			{
				return FullInstance.PropertyValue.Where(x => !x.PropertyID.HasValue).Select(x => x.PropertyName).Where(x => x != null).ToList();
			}

			// Only combined when all sections are custom or none has a name, so every name belongs to a custom setting.
			return GetValues<Guid>(PropertyValue.PropertyID).Count == 0
				? GetValues<string>(PropertyValue.PropertyName).Where(x => x != null).ToList()
				: new List<string>();
		}

		/// <summary>
		/// Gets the values of the custom settings, as <see cref="PropertySettingCollectionExposers.CustomPropertySettings.Value"/> returns them.
		/// </summary>
		public IEnumerable<string> GetCustomValues()
		{
			if (FullInstance != null)
			{
				return FullInstance.PropertyValue.Where(x => !x.PropertyID.HasValue).Select(x => x.Value).Where(x => x != null).ToList();
			}

			// Only combined when all sections are custom, or when the values all belong to the settings linked to a property.
			return GetValues<Guid>(PropertyValue.PropertyID).Count == 0
				? GetValues<string>(PropertyValue.Value).Where(x => x != null).ToList()
				: new List<string>();
		}

		/// <summary>
		/// Gets the properties of the settings, as <see cref="PropertySettingCollectionExposers.PropertySettings.PropertyId"/>
		/// returns them: grouped per type in the order of <see cref="PropertySettingCollection.PropertySettings"/>, and
		/// without the settings of which the property doesn't exist.
		/// </summary>
		public IEnumerable<Guid> GetPropertyIds()
		{
			var sections = GetTypedPropertySections().ToList();

			return sections.Where(x => x.Property is StringProperty)
				.Concat(sections.Where(x => x.Property is BooleanProperty))
				.Concat(sections.Where(x => x.Property is DiscreteProperty))
				.Concat(sections.Where(x => x.Property is FileProperty))
				.Select(x => x.Section.PropertyId)
				.ToList();
		}

		public IEnumerable<string> GetStringValues()
		{
			return GetTypedPropertySections().Where(x => x.Property is StringProperty).Select(x => x.Section.Value).Where(x => x != null).ToList();
		}

		public IEnumerable<bool> GetBooleanValues()
		{
			// Parsed the same way as InnerBooleanPropertySetting, so a missing value is false.
			return GetTypedPropertySections().Where(x => x.Property is BooleanProperty).Select(x => Convert.ToBoolean(x.Section.Value)).ToList();
		}

		public IEnumerable<string> GetDiscreteValues()
		{
			return GetTypedPropertySections().Where(x => x.Property is DiscreteProperty).Select(x => x.Section.Value).Where(x => x != null).ToList();
		}

		private IEnumerable<(PropertySection Section, Property Property)> GetTypedPropertySections()
		{
			foreach (var section in GetPropertySections())
			{
				if (PropertiesById.TryGetValue(section.PropertyId, out var property))
				{
					yield return (section, property);
				}
			}
		}

		/// <summary>
		/// Gets the sections of the settings that refer to a property, in the order they are stored. The values are only
		/// known when <see cref="PropertyValue.Value"/> was selected and <see cref="CanCombinePropertyValues"/> holds.
		/// </summary>
		private IReadOnlyList<PropertySection> GetPropertySections()
		{
			if (FullInstance != null)
			{
				return FullInstance.PropertyValue
					.Where(x => x.PropertyID.HasValue)
					.Select(x => new PropertySection(x.PropertyID.Value, x.Value))
					.ToList();
			}

			var propertyIds = GetValues<Guid>(PropertyValue.PropertyID);
			var values = GetValues<string>(PropertyValue.Value);
			var valuePerSection = values.Count == propertyIds.Count;

			return propertyIds.Select((id, i) => new PropertySection(id, valuePerSection ? values[i] : null)).ToList();
		}

		private List<T> GetValues<T>(FieldDescriptorID descriptor)
		{
			return PartialObject.TryGetValues<T>(descriptor, out var values) && values != null ? values : new List<T>();
		}

		private sealed class PropertySection
		{
			public PropertySection(Guid propertyId, string value)
			{
				PropertyId = propertyId;
				Value = value;
			}

			public Guid PropertyId { get; }

			public string Value { get; }
		}
	}
}

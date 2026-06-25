namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Helper;

	using StorageProperties = Storage.DOM.SlcProperties;

	/// <summary>
	/// Represents a collection of property settings grouped by type, linked to a specific object and scope.
	/// </summary>
	public class PropertySettingCollection : ApiObject, ICollection<PropertySettingBase>
	{
		private readonly List<InnerCustomPropertySetting> customSettings = [];
		private readonly List<InnerStringPropertySetting> stringSettings = [];
		private readonly List<InnerBooleanPropertySetting> booleanSettings = [];
		private readonly List<InnerDiscretePropertySetting> discreteSettings = [];

		private StorageProperties.PropertyValuesInstance originalInstance;
		private StorageProperties.PropertyValuesInstance updatedInstance;

		private string linkedObjectId;
		private string scope;
		private string subId;

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertySettingCollection"/> class.
		/// </summary>
		public PropertySettingCollection() : base()
		{
			IsNew = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertySettingCollection"/> class with the specified unique identifier.
		/// </summary>
		/// <param name="id">The unique identifier for the property setting collection.</param>
		public PropertySettingCollection(Guid id) : base(id)
		{
			IsNew = true;
			HasUserDefinedId = true;
		}

		internal PropertySettingCollection(MediaOpsPlanApi planApi, StorageProperties.PropertyValuesInstance instance) : base(instance.ID.Id)
		{
			ParseInstance(planApi, instance);
			InitTracking();
		}

		/// <summary>
		/// Gets the identifier of the object this collection is linked to.
		/// </summary>
		public string LinkedObjectId { get => linkedObjectId; init => linkedObjectId = value; }

		/// <summary>
		/// Gets the scope of this property setting collection.
		/// </summary>
		public string Scope { get => scope; init => scope = value; }

		/// <summary>
		/// Gets the sub-identifier for this property setting collection.
		/// </summary>
		public string SubId { get => subId; init => subId = value; }

		/// <summary>
		/// Gets the collection of custom property settings.
		/// </summary>
		public IReadOnlyCollection<CustomPropertySetting> CustomSettings => customSettings;

		/// <summary>
		/// Gets the collection of property settings linked to a property definition.
		/// </summary>
		public IReadOnlyCollection<PropertySetting> PropertySettings => stringSettings.Cast<PropertySetting>().Concat(booleanSettings).Concat(discreteSettings).ToList();

		/// <summary>
		/// Gets the collection of string property settings.
		/// </summary>
		public IReadOnlyCollection<StringPropertySetting> StringSettings => stringSettings;

		/// <summary>
		/// Gets the collection of boolean property settings.
		/// </summary>
		public IReadOnlyCollection<BooleanPropertySetting> BooleanSettings => booleanSettings;

		/// <summary>
		/// Gets the collection of discrete property settings.
		/// </summary>
		public IReadOnlyCollection<DiscretePropertySetting> DiscreteSettings => discreteSettings;

		/// <inheritdoc />
		public int Count => customSettings.Count + stringSettings.Count + booleanSettings.Count + discreteSettings.Count;

		/// <inheritdoc />
		public bool IsReadOnly => false;

		internal StorageProperties.PropertyValuesInstance OriginalInstance => originalInstance;

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + Id.GetHashCode();
				hash = (hash * 23) + (linkedObjectId != null ? linkedObjectId.GetHashCode() : 0);
				hash = (hash * 23) + (scope != null ? scope.GetHashCode() : 0);
				hash = (hash * 23) + (subId != null ? subId.GetHashCode() : 0);

				foreach (var value in customSettings.OrderBy(x => x.Name))
				{
					hash = (hash * 23) + value.GetHashCode();
				}

				foreach (var value in stringSettings.OrderBy(x => x.Id))
				{
					hash = (hash * 23) + value.GetHashCode();
				}

				foreach (var value in booleanSettings.OrderBy(x => x.Id))
				{
					hash = (hash * 23) + value.GetHashCode();
				}

				foreach (var value in discreteSettings.OrderBy(x => x.Id))
				{
					hash = (hash * 23) + value.GetHashCode();
				}

				return hash;
			}
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (obj is not PropertySettingCollection other)
			{
				return false;
			}

			return Id == other.Id
				&& linkedObjectId == other.linkedObjectId
				&& scope == other.scope
				&& subId == other.subId
				&& customSettings.ScrambledEquals(other.customSettings)
				&& stringSettings.ScrambledEquals(other.stringSettings)
				&& booleanSettings.ScrambledEquals(other.booleanSettings)
				&& discreteSettings.ScrambledEquals(other.discreteSettings);
		}

		/// <inheritdoc />
		public void Add(PropertySettingBase item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item));
			}

			switch (item)
			{
				case CustomPropertySetting custom:
					customSettings.Add(new InnerCustomPropertySetting(custom));
					break;
				case StringPropertySetting stringVal:
					stringSettings.Add(new InnerStringPropertySetting(stringVal));
					break;
				case BooleanPropertySetting boolVal:
					booleanSettings.Add(new InnerBooleanPropertySetting(boolVal));
					break;
				case DiscretePropertySetting discreteVal:
					discreteSettings.Add(new InnerDiscretePropertySetting(discreteVal));
					break;
				default:
					throw new ArgumentException($"Unsupported property setting type '{item.GetType().Name}'.", nameof(item));
			}
		}

		/// <inheritdoc />
		public void Clear()
		{
			customSettings.Clear();
			stringSettings.Clear();
			booleanSettings.Clear();
			discreteSettings.Clear();
		}

		/// <summary>
		/// Replaces all custom property settings in this collection with the specified settings, leaving the
		/// property-definition-linked settings and the collection's identity (<see cref="LinkedObjectId"/>,
		/// <see cref="Scope"/> and <see cref="SubId"/>) untouched.
		/// </summary>
		/// <param name="settings">The custom property settings that should replace the current custom settings. May be <see langword="null"/> to clear them.</param>
		public void SetCustomSettings(IEnumerable<CustomPropertySetting> settings)
		{
			customSettings.Clear();

			if (settings == null)
			{
				return;
			}

			foreach (var setting in settings)
			{
				Add(setting);
			}
		}

		/// <summary>
		/// Replaces all property-definition-linked settings in this collection with the specified settings, leaving the
		/// custom property settings and the collection's identity <see cref="LinkedObjectId"/>,
		/// <see cref="Scope"/> and <see cref="SubId"/>) untouched.
		/// </summary>
		/// <param name="settings">The property settings that should replace the current property settings. May be <see langword="null"/> to clear them.</param>
		public void SetPropertySettings(IEnumerable<PropertySetting> settings)
		{
			stringSettings.Clear();
			booleanSettings.Clear();
			discreteSettings.Clear();

			if (settings == null)
			{
				return;
			}

			foreach (var setting in settings)
			{
				Add(setting);
			}
		}

		/// <inheritdoc />
		public bool Contains(PropertySettingBase item)
		{
			if (item == null)
			{
				return false;
			}

			return item switch
			{
				CustomPropertySetting custom => customSettings.Contains(custom),
				StringPropertySetting stringVal => stringSettings.Contains(stringVal),
				BooleanPropertySetting boolVal => booleanSettings.Contains(boolVal),
				DiscretePropertySetting discreteVal => discreteSettings.Contains(discreteVal),
				_ => false,
			};
		}

		/// <inheritdoc />
		public void CopyTo(PropertySettingBase[] array, int arrayIndex)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (arrayIndex < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));
			}

			if (array.Length - arrayIndex < Count)
			{
				throw new ArgumentException("The destination array is too small.");
			}

			foreach (var item in this)
			{
				array[arrayIndex++] = item;
			}
		}

		/// <inheritdoc />
		public bool Remove(PropertySettingBase item)
		{
			if (item == null)
			{
				return false;
			}

			return item switch
			{
				CustomPropertySetting custom => customSettings.RemoveAll(x => x.Equals(custom)) > 0,
				StringPropertySetting stringVal => stringSettings.RemoveAll(x => x.Equals(stringVal)) > 0,
				BooleanPropertySetting boolVal => booleanSettings.RemoveAll(x => x.Equals(boolVal)) > 0,
				DiscretePropertySetting discreteVal => discreteSettings.RemoveAll(x => x.Equals(discreteVal)) > 0,
				_ => false,
			};
		}

		/// <inheritdoc />
		public IEnumerator<PropertySettingBase> GetEnumerator()
		{
			return customSettings
				.Cast<PropertySettingBase>()
				.Concat(stringSettings)
				.Concat(booleanSettings)
				.Concat(discreteSettings)
				.GetEnumerator();
		}

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		internal StorageProperties.PropertyValuesInstance GetInstanceWithChanges()
		{
			if (updatedInstance == null)
			{
				updatedInstance = IsNew ? new StorageProperties.PropertyValuesInstance(Id) : originalInstance.Clone();
			}

			updatedInstance.PropertyValueInfo.LinkedObjectID = linkedObjectId;
			updatedInstance.PropertyValueInfo.Scope = scope;
			updatedInstance.PropertyValueInfo.SubID = subId;

			updatedInstance.PropertyValue.Clear();
			foreach (var customSetting in customSettings)
			{
				updatedInstance.PropertyValue.Add(customSetting.GetSectionWithChanges());
			}

			foreach (var stringSetting in stringSettings)
			{
				updatedInstance.PropertyValue.Add(stringSetting.GetSectionWithChanges());
			}

			foreach (var booleanSetting in booleanSettings)
			{
				updatedInstance.PropertyValue.Add(booleanSetting.GetSectionWithChanges());
			}

			foreach (var discreteSetting in discreteSettings)
			{
				updatedInstance.PropertyValue.Add(discreteSetting.GetSectionWithChanges());
			}

			return updatedInstance;
		}

		private void ParseInstance(MediaOpsPlanApi planApi, StorageProperties.PropertyValuesInstance instance)
		{
			originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

			linkedObjectId = instance.PropertyValueInfo.LinkedObjectID;
			scope = instance.PropertyValueInfo.Scope;
			subId = instance.PropertyValueInfo.SubID;

			ParsePropertyValues(planApi, instance.PropertyValue);
		}

		private void ParsePropertyValues(MediaOpsPlanApi planApi, IList<StorageProperties.PropertyValueSection> propertyValues)
		{
			if (propertyValues == null || propertyValues.Count == 0)
			{
				return;
			}

			var propertyIds = propertyValues.Where(pv => pv.PropertyID.HasValue).Select(pv => pv.PropertyID.Value).Distinct();
			var propertiesById = planApi.Properties.Read(propertyIds).ToDictionary(p => p.Id);

			foreach (var section in propertyValues)
			{
				Property property = null;
				if (!section.PropertyID.HasValue)
				{
					customSettings.Add(new InnerCustomPropertySetting(section));
				}
				else if (!propertiesById.TryGetValue(section.PropertyID.Value, out property))
				{
					planApi.Logger.Information(this, $"Property with ID '{section.PropertyID.Value}' not found.");
				}

				if (property == null)
				{
					continue;
				}

				if (property is StringProperty)
				{
					stringSettings.Add(new InnerStringPropertySetting(section));
				}
				else if (property is BooleanProperty)
				{
					booleanSettings.Add(new InnerBooleanPropertySetting(section));
				}
				else if (property is DiscreteProperty)
				{
					discreteSettings.Add(new InnerDiscretePropertySetting(section));
				}
			}
		}
	}
}

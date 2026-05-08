namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Helper;

	using StorageProperties = Storage.DOM.SlcProperties;

	/// <summary>
	/// Represents a collection of property values grouped by type, linked to a specific object and scope.
	/// </summary>
	public class PropertyValueCollection : ApiObject, ICollection<PropertyValueBase>
	{
		private readonly List<InnerCustomPropertyValue> customValues = [];
		private readonly List<InnerStringPropertyValue> stringValues = [];
		private readonly List<InnerBooleanPropertyValue> booleanValues = [];
		private readonly List<InnerDiscretePropertyValue> discreteValues = [];

		private StorageProperties.PropertyValuesInstance originalInstance;
		private StorageProperties.PropertyValuesInstance updatedInstance;

		private string linkedObjectId;
		private string scope;
		private string subId;

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyValueCollection"/> class.
		/// </summary>
		public PropertyValueCollection() : base()
		{
			IsNew = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyValueCollection"/> class with the specified unique identifier.
		/// </summary>
		/// <param name="id">The unique identifier for the property value collection.</param>
		public PropertyValueCollection(Guid id) : base(id)
		{
			IsNew = true;
			HasUserDefinedId = true;
		}

		internal PropertyValueCollection(MediaOpsPlanApi planApi, StorageProperties.PropertyValuesInstance instance) : base(instance.ID.Id)
		{
			ParseInstance(planApi, instance);
			InitTracking();
		}

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		public override string Name { get; set; }

		/// <summary>
		/// Gets the identifier of the object this collection is linked to.
		/// </summary>
		public string LinkedObjectId { get => linkedObjectId; init => linkedObjectId = value; }

		/// <summary>
		/// Gets the scope of this property value collection.
		/// </summary>
		public string Scope { get => scope; init => scope = value; }

		/// <summary>
		/// Gets the sub-identifier for this property value collection.
		/// </summary>
		public string SubId { get => subId; init => subId = value; }

		/// <summary>
		/// Gets the collection of custom property values.
		/// </summary>
		public IReadOnlyCollection<CustomPropertyValue> CustomValues => customValues;

		/// <summary>
		/// Gets the collection of property values linked to a property definition.
		/// </summary>
		public IReadOnlyCollection<PropertyValue> PropertyValues => stringValues.Cast<PropertyValue>().Concat(booleanValues).Concat(discreteValues).ToList();

		/// <summary>
		/// Gets the collection of string property values.
		/// </summary>
		public IReadOnlyCollection<StringPropertyValue> StringValues => stringValues;

		/// <summary>
		/// Gets the collection of boolean property values.
		/// </summary>
		public IReadOnlyCollection<BooleanPropertyValue> BooleanValues => booleanValues;

		/// <summary>
		/// Gets the collection of discrete property values.
		/// </summary>
		public IReadOnlyCollection<DiscretePropertyValue> DiscreteValues => discreteValues;

		/// <inheritdoc />
		public int Count => customValues.Count + stringValues.Count + booleanValues.Count + discreteValues.Count;

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

				foreach (var value in customValues.OrderBy(x => x.Name))
				{
					hash = (hash * 23) + value.GetHashCode();
				}

				foreach (var value in stringValues.OrderBy(x => x.Id))
				{
					hash = (hash * 23) + value.GetHashCode();
				}

				foreach (var value in booleanValues.OrderBy(x => x.Id))
				{
					hash = (hash * 23) + value.GetHashCode();
				}

				foreach (var value in discreteValues.OrderBy(x => x.Id))
				{
					hash = (hash * 23) + value.GetHashCode();
				}

				return hash;
			}
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (obj is not PropertyValueCollection other)
			{
				return false;
			}

			return Id == other.Id
				&& linkedObjectId == other.linkedObjectId
				&& scope == other.scope
				&& subId == other.subId
				&& customValues.ScrambledEquals(other.customValues)
				&& stringValues.ScrambledEquals(other.stringValues)
				&& booleanValues.ScrambledEquals(other.booleanValues)
				&& discreteValues.ScrambledEquals(other.discreteValues);
		}

		/// <inheritdoc />
		public void Add(PropertyValueBase item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item));
			}

			switch (item)
			{
				case CustomPropertyValue custom:
					customValues.Add(new InnerCustomPropertyValue(custom));
					break;
				case StringPropertyValue stringVal:
					stringValues.Add(new InnerStringPropertyValue(stringVal));
					break;
				case BooleanPropertyValue boolVal:
					booleanValues.Add(new InnerBooleanPropertyValue(boolVal));
					break;
				case DiscretePropertyValue discreteVal:
					discreteValues.Add(new InnerDiscretePropertyValue(discreteVal));
					break;
				default:
					throw new ArgumentException($"Unsupported property value type '{item.GetType().Name}'.", nameof(item));
			}
		}

		/// <inheritdoc />
		public void Clear()
		{
			customValues.Clear();
			stringValues.Clear();
			booleanValues.Clear();
			discreteValues.Clear();
		}

		/// <inheritdoc />
		public bool Contains(PropertyValueBase item)
		{
			if (item == null)
			{
				return false;
			}

			return item switch
			{
				CustomPropertyValue custom => customValues.Contains(custom),
				StringPropertyValue stringVal => stringValues.Contains(stringVal),
				BooleanPropertyValue boolVal => booleanValues.Contains(boolVal),
				DiscretePropertyValue discreteVal => discreteValues.Contains(discreteVal),
				_ => false,
			};
		}

		/// <inheritdoc />
		public void CopyTo(PropertyValueBase[] array, int arrayIndex)
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
		public bool Remove(PropertyValueBase item)
		{
			if (item == null)
			{
				return false;
			}

			return item switch
			{
				CustomPropertyValue custom => customValues.RemoveAll(x => x.Equals(custom)) > 0,
				StringPropertyValue stringVal => stringValues.RemoveAll(x => x.Equals(stringVal)) > 0,
				BooleanPropertyValue boolVal => booleanValues.RemoveAll(x => x.Equals(boolVal)) > 0,
				DiscretePropertyValue discreteVal => discreteValues.RemoveAll(x => x.Equals(discreteVal)) > 0,
				_ => false,
			};
		}

		/// <inheritdoc />
		public IEnumerator<PropertyValueBase> GetEnumerator()
		{
			return customValues
				.Cast<PropertyValueBase>()
				.Concat(stringValues)
				.Concat(booleanValues)
				.Concat(discreteValues)
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
			foreach (var customValue in customValues)
			{
				updatedInstance.PropertyValue.Add(customValue.GetSectionWithChanges());
			}

			foreach (var stringValue in stringValues)
			{
				updatedInstance.PropertyValue.Add(stringValue.GetSectionWithChanges());
			}

			foreach (var booleanValue in booleanValues)
			{
				updatedInstance.PropertyValue.Add(booleanValue.GetSectionWithChanges());
			}

			foreach (var discreteValue in discreteValues)
			{
				updatedInstance.PropertyValue.Add(discreteValue.GetSectionWithChanges());
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
					customValues.Add(new InnerCustomPropertyValue(section));
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
					stringValues.Add(new InnerStringPropertyValue(section));
				}
				else if (property is BooleanProperty)
				{
					booleanValues.Add(new InnerBooleanPropertyValue(section));
				}
				else if (property is DiscreteProperty)
				{
					discreteValues.Add(new InnerDiscretePropertyValue(section));
				}
			}
		}
	}
}

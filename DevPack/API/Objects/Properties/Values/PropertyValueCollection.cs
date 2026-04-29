namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using StorageProperties = Storage.DOM.SlcProperties;

	/// <summary>
	/// Represents a collection of property values grouped by type, linked to a specific object and scope.
	/// </summary>
	public class PropertyValueCollection : ApiObject, ICollection<PropertyValueBase>
	{
		private readonly List<CustomPropertyValue> customValues = [];
		private readonly List<StringPropertyValue> stringValues = [];
		private readonly List<BooleanPropertyValue> booleanValues = [];
		private readonly List<DiscretePropertyValue> discreteValues = [];

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
					customValues.Add(custom);
					break;
				case StringPropertyValue stringVal:
					stringValues.Add(stringVal);
					break;
				case BooleanPropertyValue boolVal:
					booleanValues.Add(boolVal);
					break;
				case DiscretePropertyValue discreteVal:
					discreteValues.Add(discreteVal);
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
				CustomPropertyValue custom => customValues.Remove(custom),
				StringPropertyValue stringVal => stringValues.Remove(stringVal),
				BooleanPropertyValue boolVal => booleanValues.Remove(boolVal),
				DiscretePropertyValue discreteVal => discreteValues.Remove(discreteVal),
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
					customValues.Add(new CustomPropertyValue(section));
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
					stringValues.Add(new StringPropertyValue(section));
				}
				else if (property is BooleanProperty)
				{
					booleanValues.Add(new BooleanPropertyValue(section));
				}
				else if (property is DiscreteProperty)
				{
					discreteValues.Add(new DiscretePropertyValue(section));
				}
			}
		}
	}
}

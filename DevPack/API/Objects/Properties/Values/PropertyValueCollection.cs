namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Represents a collection of property values grouped by type, linked to a specific object and scope.
	/// </summary>
	public class PropertyValueCollection : ApiObject, ICollection<PropertyValueBase>
	{
		protected readonly List<CustomPropertyValue> customValues = [];
		protected readonly List<StringPropertyValue> stringValues = [];
		protected readonly List<BooleanPropertyValue> booleanValues = [];
		protected readonly List<DiscretePropertyValue> discreteValues = [];

		protected string linkedObjectId;
		protected string scope;
		protected string subId;

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyValueCollection"/> class.
		/// </summary>
		public PropertyValueCollection() : base()
		{
			IsNew = true;
		}

		private protected PropertyValueCollection(Guid id) : base(id)
		{
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
	}
}

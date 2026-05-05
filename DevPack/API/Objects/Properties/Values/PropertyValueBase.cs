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

	/// <summary>
	/// Provides a base class for property values.
	/// </summary>
	public abstract class PropertyValueBase : TrackableObject
	{
		private protected PropertyValueBase()
		{
			IsNew = true;
		}

		/// <summary>
		/// Gets the name of the property value.
		/// </summary>
		public string Name { get; protected set; }
	}

	/// <summary>
	/// Represents a custom property value that is not linked to a predefined property definition.
	/// </summary>
	public class CustomPropertyValue : PropertyValueBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CustomPropertyValue"/> class with the specified name.
		/// </summary>
		/// <param name="name">The name of the custom property. Cannot be <see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="name"/> is <see langword="null"/>.</exception>
		public CustomPropertyValue(string name)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}

		internal CustomPropertyValue()
		{
		}

		/// <summary>
		/// Gets or sets the string value of this custom property.
		/// </summary>
		public string Value { get; set; }
	}

	/// <summary>
	/// Represents a property value that is linked to a specific <see cref="Property"/> definition.
	/// </summary>
	public abstract class PropertyValue : PropertyValueBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyValue"/> class linked to the specified property.
		/// </summary>
		/// <param name="property">The property definition to link to. Cannot be <see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="property"/> is <see langword="null"/>.</exception>
		public PropertyValue(Property property)
		{
			PropertyId = property?.Id ?? throw new ArgumentNullException(nameof(property));
		}

		internal PropertyValue()
		{
		}

		/// <summary>
		/// Gets the unique identifier of the linked property definition.
		/// </summary>
		public Guid PropertyId { get; internal set; }
	}

	/// <summary>
	/// Represents a property value that holds a string value.
	/// </summary>
	public class StringPropertyValue : PropertyValue
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StringPropertyValue"/> class linked to the specified string property.
		/// </summary>
		/// <param name="property">The <see cref="StringProperty"/> definition to link to.</param>
		public StringPropertyValue(StringProperty property) : base(property)
		{
		}

		internal StringPropertyValue()
		{
		}

		/// <summary>
		/// Gets or sets the string value of this property.
		/// </summary>
		public string Value { get; set; }
	}

	/// <summary>
	/// Represents a property value that holds a boolean value.
	/// </summary>
	public class BooleanPropertyValue : PropertyValue
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BooleanPropertyValue"/> class linked to the specified boolean property.
		/// </summary>
		/// <param name="property">The <see cref="BooleanProperty"/> definition to link to.</param>
		public BooleanPropertyValue(BooleanProperty property) : base(property)
		{
		}

		internal BooleanPropertyValue()
		{
		}

		/// <summary>
		/// Gets or sets the boolean value of this property.
		/// </summary>
		public bool Value { get; set; }
	}

	/// <summary>
	/// Represents a property value that holds a discrete (predefined) string value.
	/// </summary>
	public class DiscretePropertyValue : PropertyValue
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DiscretePropertyValue"/> class linked to the specified discrete property.
		/// </summary>
		/// <param name="property">The <see cref="DiscreteProperty"/> definition to link to.</param>
		public DiscretePropertyValue(DiscreteProperty property) : base(property)
		{
		}

		internal DiscretePropertyValue()
		{
		}

		/// <summary>
		/// Gets or sets the discrete value of this property.
		/// </summary>
		public string Value { get; set; }
	}
}

namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class PropertyValueCollection : ApiObject
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

		public string LinkedObjectId { get => linkedObjectId; init => linkedObjectId = value; }

		public string Scope { get => scope; init => scope = value; }

		public string SubId { get => subId; init => subId = value; }

		public IReadOnlyCollection<CustomPropertyValue> CustomValues => customValues;

		public IReadOnlyCollection<StringPropertyValue> StringValues => stringValues;

		public IReadOnlyCollection<BooleanPropertyValue> BooleanValues => booleanValues;

		public IReadOnlyCollection<DiscretePropertyValue> DiscreteValues => discreteValues;

		public IReadOnlyCollection<PropertyValue> PropertyValues => stringValues.Cast<PropertyValue>().Concat(booleanValues).Concat(discreteValues).ToList();
	}

	public abstract class PropertyValueBase : TrackableObject
	{
		private protected PropertyValueBase()
		{
			IsNew = true;
		}

		public string Name { get; protected set; }
	}

	public class CustomPropertyValue : PropertyValueBase
	{
		public CustomPropertyValue(string name)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}

		internal CustomPropertyValue()
		{
		}

		public string Value { get; set; }
	}

	public abstract class PropertyValue : PropertyValueBase
	{
		public PropertyValue(Property property)
		{
			PropertyId = property?.Id ?? throw new ArgumentNullException(nameof(property));
		}

		internal PropertyValue()
		{
		}

		public Guid PropertyId { get; internal set; }
	}

	public class StringPropertyValue : PropertyValue
	{
		public StringPropertyValue(StringProperty property) : base(property)
		{
		}

		internal StringPropertyValue()
		{
		}

		public string Value { get; set; }
	}

	public class BooleanPropertyValue : PropertyValue
	{
		public BooleanPropertyValue(BooleanProperty property) : base(property)
		{
		}

		internal BooleanPropertyValue()
		{
		}

		public bool Value { get; set; }
	}

	public class DiscretePropertyValue : PropertyValue
	{
		public DiscretePropertyValue(DiscreteProperty property) : base(property)
		{
		}

		internal DiscretePropertyValue()
		{
		}

		public string Value { get; set; }
	}
}

namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents a property value that holds a boolean value.
	/// </summary>
	public class BooleanPropertyValue : PropertyValue
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BooleanPropertyValue"/> class linked to the specified boolean property.
		/// </summary>
		/// <param name="property">The <see cref="BooleanProperty"/> definition to link to.</param>
		public BooleanPropertyValue(BooleanProperty property)
			: base(property)
		{
		}

		internal BooleanPropertyValue()
		{
		}

		internal BooleanPropertyValue(BooleanPropertyValue booleanPropertyValue)
			: base(booleanPropertyValue)
		{
			Value = booleanPropertyValue.Value;
		}

		/// <summary>
		/// Gets or sets the boolean value of this property.
		/// </summary>
		public bool Value { get; set; }

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = base.GetHashCode();
				hash = hash * 23 + Value.GetHashCode();

				return hash;
			}
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (obj is not BooleanPropertyValue other)
			{
				return false;
			}

			return base.Equals(obj)
				&& Value == other.Value;
		}
	}
}

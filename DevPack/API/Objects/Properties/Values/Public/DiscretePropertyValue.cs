namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents a property value that holds a discrete (predefined) string value.
	/// </summary>
	public class DiscretePropertyValue : PropertyValue
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DiscretePropertyValue"/> class linked to the specified discrete property.
		/// </summary>
		/// <param name="property">The <see cref="DiscreteProperty"/> definition to link to.</param>
		public DiscretePropertyValue(DiscreteProperty property)
			: base(property)
		{
		}

		internal DiscretePropertyValue()
		{
		}

		internal DiscretePropertyValue(DiscretePropertyValue discretePropertyValue)
			: base(discretePropertyValue)
		{
			Value = discretePropertyValue.Value;
		}

		/// <summary>
		/// Gets or sets the discrete value of this property.
		/// </summary>
		public string Value { get; set; }

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = base.GetHashCode();
				hash = hash * 23 + (Value?.GetHashCode() ?? 0);

				return hash;
			}
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (obj is not DiscretePropertyValue other)
			{
				return false;
			}

			return base.Equals(obj)
				&& Value == other.Value;
		}
	}
}

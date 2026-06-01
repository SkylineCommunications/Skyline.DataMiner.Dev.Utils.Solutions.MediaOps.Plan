namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents a property value that holds a discrete (predefined) string value.
	/// </summary>
	public class DiscretePropertySetting : PropertySetting
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DiscretePropertySetting"/> class linked to the specified discrete property.
		/// </summary>
		/// <param name="property">The <see cref="DiscreteProperty"/> definition to link to.</param>
		public DiscretePropertySetting(DiscreteProperty property)
			: base(property)
		{
		}

		internal DiscretePropertySetting()
		{
		}

		internal DiscretePropertySetting(DiscretePropertySetting discretePropertyValue)
			: base(discretePropertyValue)
		{
			Value = discretePropertyValue.Value;
		}

		/// <summary>
		/// Gets or sets the discrete value of this property.
		/// </summary>
		public string Value { get; set; }

		/// <inheritdoc/>
		public override bool HasValue => !string.IsNullOrWhiteSpace(Value);

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
			if (obj is not DiscretePropertySetting other)
			{
				return false;
			}

			return base.Equals(obj)
				&& Value == other.Value;
		}
	}
}

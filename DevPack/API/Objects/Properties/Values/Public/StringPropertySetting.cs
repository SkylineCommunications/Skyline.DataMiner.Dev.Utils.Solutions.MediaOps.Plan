namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents a property value that holds a string value.
	/// </summary>
	public class StringPropertySetting : PropertySetting
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StringPropertySetting"/> class linked to the specified string property.
		/// </summary>
		/// <param name="property">The <see cref="StringProperty"/> definition to link to.</param>
		public StringPropertySetting(StringProperty property)
			: base(property)
		{
		}

		internal StringPropertySetting()
		{
		}

		internal StringPropertySetting(StringPropertySetting stringPropertyValue)
			: base(stringPropertyValue)
		{
			Value = stringPropertyValue.Value;
		}

		/// <summary>
		/// Gets or sets the string value of this property.
		/// </summary>
		public string Value { get; set; }

		/// <inheritdoc/>
		public override bool HasValue => !string.IsNullOrWhiteSpace(Value);

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				int hash  = base.GetHashCode();
				hash = hash * 23 + (Value?.GetHashCode() ?? 0);

				return hash;
			}
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (obj is not StringPropertySetting other)
			{
				return false;
			}

			return base.Equals(obj)
				&& Value == other.Value;
		}
	}
}

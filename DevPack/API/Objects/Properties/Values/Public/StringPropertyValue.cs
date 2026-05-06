namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents a property value that holds a string value.
	/// </summary>
	public class StringPropertyValue : PropertyValue
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StringPropertyValue"/> class linked to the specified string property.
		/// </summary>
		/// <param name="property">The <see cref="StringProperty"/> definition to link to.</param>
		public StringPropertyValue(StringProperty property)
			: base(property)
		{
		}

		internal StringPropertyValue()
		{
		}

		internal StringPropertyValue(StringPropertyValue stringPropertyValue)
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
			if (obj is not StringPropertyValue other)
			{
				return false;
			}

			return base.Equals(obj)
				&& Value == other.Value;
		}
	}
}

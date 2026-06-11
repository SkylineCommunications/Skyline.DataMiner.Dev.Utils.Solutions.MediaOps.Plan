namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents a property value that holds a boolean value.
	/// </summary>
	public class BooleanPropertySetting : PropertySetting
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BooleanPropertySetting"/> class linked to the specified boolean property.
		/// </summary>
		/// <param name="property">The <see cref="BooleanProperty"/> definition to link to.</param>
		public BooleanPropertySetting(BooleanProperty property)
			: base(property)
		{
		}

		internal BooleanPropertySetting()
		{
		}

		internal BooleanPropertySetting(BooleanPropertySetting booleanPropertySetting)
			: base(booleanPropertySetting)
		{
			Value = booleanPropertySetting.Value;
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
			if (obj is not BooleanPropertySetting other)
			{
				return false;
			}

			return base.Equals(obj)
				&& Value == other.Value;
		}
	}
}

namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents a custom property value that is not linked to a predefined property definition.
	/// </summary>
	public class CustomPropertySetting : PropertySettingBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CustomPropertySetting"/> class with the specified name.
		/// </summary>
		/// <param name="name">The name of the custom property. Cannot be <see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="name"/> is <see langword="null"/>.</exception>
		public CustomPropertySetting(string name)
			: base(true)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}

		internal CustomPropertySetting()
		{
		}

		internal CustomPropertySetting(CustomPropertySetting customPropertyValue)
			: base(customPropertyValue)
		{
			Name = customPropertyValue.Name;
			Value = customPropertyValue.Value;
		}

		/// <summary>
		/// Gets or sets the name of this custom property.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the string value of this custom property.
		/// </summary>
		public string Value { get; set; }

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + (Name?.GetHashCode() ?? 0);
				hash = hash * 23 + (Value?.GetHashCode() ?? 0);

				return hash;
			}
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (obj is not CustomPropertySetting other)
			{
				return false;
			}

			return Name == other.Name
				&& Value == other.Value;
		}
	}
}

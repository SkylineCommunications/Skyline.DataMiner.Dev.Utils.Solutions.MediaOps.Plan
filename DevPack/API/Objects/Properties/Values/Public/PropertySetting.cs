namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents a property value that is linked to a specific <see cref="Property"/> definition.
	/// </summary>
	/// <seealso cref="BooleanPropertySetting"/>
	/// <seealso cref="DiscretePropertySetting"/>
	/// <seealso cref="FilePropertySetting"/>
	/// <seealso cref="StringPropertySetting"/>
	public abstract class PropertySetting : PropertySettingBase
	{
		private protected PropertySetting(Property property)
			: this(property?.Id ?? throw new ArgumentNullException(nameof(property)))
		{
		}

		private protected PropertySetting(Guid propertyId)
			: base(true)
		{
			if (propertyId == Guid.Empty)
			{
				throw new ArgumentException(nameof(propertyId));
			}

			Id = propertyId;
		}

		private protected PropertySetting()
		{
		}

		private protected PropertySetting(PropertySetting propertySetting)
			: base(propertySetting)
		{
			Id = propertySetting.Id;
		}

		/// <summary>
		/// Gets the unique identifier of the property.
		/// </summary>
		public Guid Id { get; internal set; }

		/// <summary>
		/// Determines whether this property setting is a boolean property setting and, if so, returns it as a <see cref="BooleanPropertySetting"/>.
		/// </summary>
		/// <param name="setting">When this method returns, contains the current property setting as a <see cref="BooleanPropertySetting"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this property setting is a <see cref="BooleanPropertySetting"/>; otherwise, <c>false</c>.</returns>
		public bool IsBooleanPropertySetting(out BooleanPropertySetting setting)
		{
			setting = this as BooleanPropertySetting;
			return setting != null;
		}

		/// <summary>
		/// Determines whether this property setting is a discrete property setting and, if so, returns it as a <see cref="DiscretePropertySetting"/>.
		/// </summary>
		/// <param name="setting">When this method returns, contains the current property setting as a <see cref="DiscretePropertySetting"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this property setting is a <see cref="DiscretePropertySetting"/>; otherwise, <c>false</c>.</returns>
		public bool IsDiscretePropertySetting(out DiscretePropertySetting setting)
		{
			setting = this as DiscretePropertySetting;
			return setting != null;
		}

		/// <summary>
		/// Determines whether this property setting is a file property setting and, if so, returns it as a <see cref="FilePropertySetting"/>.
		/// </summary>
		/// <param name="setting">When this method returns, contains the current property setting as a <see cref="FilePropertySetting"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this property setting is a <see cref="FilePropertySetting"/>; otherwise, <c>false</c>.</returns>
		public bool IsFilePropertySetting(out FilePropertySetting setting)
		{
			setting = this as FilePropertySetting;
			return setting != null;
		}

		/// <summary>
		/// Determines whether this property setting is a string property setting and, if so, returns it as a <see cref="StringPropertySetting"/>.
		/// </summary>
		/// <param name="setting">When this method returns, contains the current property setting as a <see cref="StringPropertySetting"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this property setting is a <see cref="StringPropertySetting"/>; otherwise, <c>false</c>.</returns>
		public bool IsStringPropertySetting(out StringPropertySetting setting)
		{
			setting = this as StringPropertySetting;
			return setting != null;
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + Id.GetHashCode();

				return hash;
			}
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (obj is not PropertySetting other)
			{
				return false;
			}

			return Id == other.Id;
		}
	}
}

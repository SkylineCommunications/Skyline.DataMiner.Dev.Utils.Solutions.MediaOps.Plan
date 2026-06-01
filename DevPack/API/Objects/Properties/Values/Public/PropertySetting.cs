namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents a property value that is linked to a specific <see cref="Property"/> definition.
	/// </summary>
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

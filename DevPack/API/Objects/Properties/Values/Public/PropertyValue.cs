namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents a property value that is linked to a specific <see cref="Property"/> definition.
	/// </summary>
	public abstract class PropertyValue : PropertyValueBase
	{
		private protected PropertyValue(Property property)
			: this(property?.Id ?? throw new ArgumentNullException(nameof(property)))
		{
		}

		private protected PropertyValue(Guid propertyId)
			: base(true)
		{
			if (propertyId == Guid.Empty)
			{
				throw new ArgumentException(nameof(propertyId));
			}

			Id = propertyId;
		}

		private protected PropertyValue()
		{
		}

		private protected PropertyValue(PropertyValue propertyValue)
			: base(propertyValue)
		{
			Id = propertyValue.Id;
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
			if (obj is not PropertyValue other)
			{
				return false;
			}

			return Id == other.Id;
		}
	}
}

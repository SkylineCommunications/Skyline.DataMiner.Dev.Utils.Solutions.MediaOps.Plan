namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents a capacity setting that defines a range with minimum and maximum values.
	/// </summary>
	public class RangeCapacitySetting : CapacitySetting
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RangeCapacitySetting"/> class using the specified capacity.
		/// </summary>
		/// <param name="capacity">The capacity to use for initializing the settings. Cannot be null.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="capacity"/> is <see langword="null"/>.</exception>
		public RangeCapacitySetting(RangeCapacity capacity)
			: base(capacity)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RangeCapacitySetting"/> class with the specified capacity ID.
		/// </summary>
		/// <param name="capacityId">The unique identifier for the capacity. Must not be an empty GUID.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="capacityId"/> is an empty GUID.</exception>
		public RangeCapacitySetting(Guid capacityId)
			: base(capacityId)
		{
		}

		internal RangeCapacitySetting() : base()
		{
		}

		internal RangeCapacitySetting(RangeCapacitySetting rangeCapacitySetting)
			: base(rangeCapacitySetting)
		{
			MinValue = rangeCapacitySetting.MinValue;
			MaxValue = rangeCapacitySetting.MaxValue;
		}

		/// <summary>
		/// Gets or sets the minimum capacity value.
		/// </summary>
		public decimal? MinValue { get; set; }

		/// <summary>
		/// Gets or sets the maximum capacity value.
		/// </summary>
		public decimal? MaxValue { get; set; }

		/// <inheritdoc/>
		public override bool HasValue => MinValue.HasValue && MaxValue.HasValue;

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 17;
				hash = (hash * 23) + Id.GetHashCode();
				hash = (hash * 23) + MinValue.GetHashCode();
				hash = (hash * 23) + MaxValue.GetHashCode();
				hash = (hash * 23) + (OriginalSection != null ? OriginalSection.ID.Id.GetHashCode() : 0);
				hash = (hash * 23) + (Reference != null ? Reference.GetHashCode() : 0);

				return hash;
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not RangeCapacitySetting other)
			{
				return false;
			}

			return Id == other.Id &&
				   MinValue == other.MinValue &&
				   MaxValue == other.MaxValue &&
				   Reference == other.Reference;
		}
	}
}

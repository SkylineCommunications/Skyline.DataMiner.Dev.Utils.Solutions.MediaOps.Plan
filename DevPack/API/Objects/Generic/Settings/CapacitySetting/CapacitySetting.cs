namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents an abstract base class for settings associated with a specific capacity.
	/// </summary>
	public abstract class CapacitySetting : Setting
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CapacitySetting"/> class using the specified capacity.
		/// </summary>
		/// <param name="capacity">The capacity to use for initializing the settings. Cannot be null.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="capacity"/> is <see langword="null"/>.</exception>
		private protected CapacitySetting(Capacity capacity)
			: this(capacity?.Id ?? throw new ArgumentNullException(nameof(capacity)))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CapacitySetting"/> class with the specified capacity ID.
		/// </summary>
		/// <param name="capacityId">The unique identifier for the capacity. Must not be an empty GUID.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="capacityId"/> is an empty GUID.</exception>
		private protected CapacitySetting(Guid capacityId)
			: base(capacityId)
		{
		}

		private protected CapacitySetting()
		{
		}

		private protected CapacitySetting(CapacitySetting capacitySetting)
			: base(capacitySetting)
		{
		}

		/// <summary>
		/// Gets the unique identifier of the capacity.
		/// </summary>
		public new Guid Id { get => base.Id; internal set => base.Id = value; }

		/// <summary>
		/// Determines whether this capacity setting represents a numeric capacity and, if so, returns it as a <see cref="NumberCapacitySetting"/>.
		/// </summary>
		/// <param name="setting">When this method returns, contains the current capacity setting as a <see cref="NumberCapacitySetting"/> when it represents a numeric capacity; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this capacity setting represents a numeric capacity; otherwise, <c>false</c>.</returns>
		public bool IsNumberCapacity(out NumberCapacitySetting setting)
		{
			setting = this as NumberCapacitySetting;
			return setting != null;
		}

		/// <summary>
		/// Determines whether this capacity setting represents a range capacity and, if so, returns it as a <see cref="RangeCapacitySetting"/>.
		/// </summary>
		/// <param name="setting">When this method returns, contains the current capacity setting as a <see cref="RangeCapacitySetting"/> when it represents a range capacity; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this capacity setting represents a range capacity; otherwise, <c>false</c>.</returns>
		public bool IsRangeCapacity(out RangeCapacitySetting setting)
		{
			setting = this as RangeCapacitySetting;
			return setting != null;
		}
	}
}

namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents a difference between a capacity configured in DOM and the corresponding capacity in CORE.
	/// </summary>
	public sealed class CapacityDifference : SynchronizationDifference
	{
		internal CapacityDifference(SynchronizationDifferenceKind kind, Guid capacityId)
			: base(kind)
		{
			CapacityId = capacityId;
		}

		/// <summary>
		/// Gets the identifier of the profile parameter representing the capacity.
		/// </summary>
		public Guid CapacityId { get; }

		/// <summary>
		/// Gets the name of the capacity, or <see langword="null"/> when the capacity no longer exists.
		/// </summary>
		public string Name { get; internal set; }

		/// <summary>
		/// Gets a value indicating whether the capacity is a range capacity.
		/// </summary>
		public bool IsRange { get; internal set; }

		/// <summary>
		/// Gets the minimum value configured in DOM, or <see langword="null"/> when the capacity is not configured in DOM or is not a range capacity.
		/// </summary>
		public decimal? DomMinValue { get; internal set; }

		/// <summary>
		/// Gets the maximum value configured in DOM, or <see langword="null"/> when the capacity is not configured in DOM.
		/// </summary>
		public decimal? DomMaxValue { get; internal set; }

		/// <summary>
		/// Gets the minimum value configured in CORE, or <see langword="null"/> when the capacity is not configured in CORE or is not a range capacity.
		/// </summary>
		public decimal? CoreMinValue { get; internal set; }

		/// <summary>
		/// Gets the maximum value configured in CORE, or <see langword="null"/> when the capacity is not configured in CORE.
		/// </summary>
		public decimal? CoreMaxValue { get; internal set; }
	}
}

namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents usage of a capacity assigned to a resource.
	/// </summary>
	/// <seealso cref="NumberCapacityUsage"/>
	/// <seealso cref="RangeCapacityUsage"/>
	public abstract class CapacityUsage
	{
		private protected CapacityUsage(Guid capacityId)
		{
			CapacityId = capacityId;
		}

		/// <summary>
		/// Gets the capacity identifier.
		/// </summary>
		public Guid CapacityId { get; }
	}
}
namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a capability discrete value is referenced by one or multiple resources.
	/// </summary>
	public class CapabilityDiscreteValueInUseByResourcesError : CapabilityDiscreteValueInUseError
	{
		/// <summary>
		/// Ids of the resources referencing the capability discrete value.
		/// </summary>
		public IReadOnlyCollection<Guid> ResourceIds { get; internal set; } = new List<Guid>();
	}
}

namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Represents the resources that are eligible for a request and their current usage.
	/// </summary>
	public sealed class EligibleResourcesResult
	{
		internal EligibleResourcesResult(IEnumerable<EligibleResource> eligibleResources)
		{
			EligibleResources = eligibleResources.ToList().AsReadOnly();
		}

		/// <summary>
		/// Gets the eligible resources and their usage.
		/// </summary>
		public IReadOnlyCollection<EligibleResource> EligibleResources { get; }
	}
}
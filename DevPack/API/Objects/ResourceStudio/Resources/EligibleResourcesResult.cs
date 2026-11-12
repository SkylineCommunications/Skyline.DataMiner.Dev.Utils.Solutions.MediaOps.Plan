namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Represents the resources that are eligible for a request and their current usage.
	/// </summary>
	public sealed class EligibleResourcesResult
	{
		internal EligibleResourcesResult(Guid contextId, IEnumerable<EligibleResource> eligibleResources)
		{
			ContextId = contextId;
			EligibleResources = (eligibleResources ?? throw new ArgumentNullException(nameof(eligibleResources))).ToList().AsReadOnly();
		}

		/// <summary>
		/// Gets the identifier of the <see cref="EligibleResourcesContext"/> this result is for.
		/// </summary>
		public Guid ContextId { get; }

		/// <summary>
		/// Gets the eligible resources and their usage.
		/// </summary>
		public IReadOnlyCollection<EligibleResource> EligibleResources { get; }
	}
}
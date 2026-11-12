namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents an eligible resource and its usage during the requested time range.
	/// </summary>
	public sealed class EligibleResource
	{
		internal EligibleResource(Resource resource, ResourceUsage usage)
		{
			Resource = resource ?? throw new ArgumentNullException(nameof(resource));
			Usage = usage ?? throw new ArgumentNullException(nameof(usage));
		}

		/// <summary>
		/// Gets the eligible resource.
		/// </summary>
		public Resource Resource { get; }

		/// <summary>
		/// Gets the resource usage during the requested time range.
		/// </summary>
		public ResourceUsage Usage { get; }
	}
}
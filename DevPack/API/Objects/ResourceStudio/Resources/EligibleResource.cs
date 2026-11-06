namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents an eligible resource and its usage during the requested time range.
	/// </summary>
	public sealed class EligibleResource
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EligibleResource"/> class.
		/// </summary>
		/// <param name="resource">The eligible resource.</param>
		/// <param name="usage">The usage of the resource during the requested time range.</param>
		public EligibleResource(Resource resource, ResourceUsage usage)
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
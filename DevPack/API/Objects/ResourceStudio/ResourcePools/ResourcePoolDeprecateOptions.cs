namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents options for deprecating a resource pool.
	/// </summary>
	public class ResourcePoolDeprecateOptions
	{
		/// <summary>
		/// Gets or sets a value indicating whether resource deprecation is allowed.
		/// Resources that are part of multiple resource pools will not be deprecated.
		/// </summary>
		/// <value><c>true</c> if resource deprecation is allowed; otherwise, <c>false</c>.</value>
		public bool AllowResourceDeprecation { get; set; } = false;

		internal static ResourcePoolDeprecateOptions GetDefaults()
		{
			return new ResourcePoolDeprecateOptions
			{
				AllowResourceDeprecation = false,
			};
		}
	}
}

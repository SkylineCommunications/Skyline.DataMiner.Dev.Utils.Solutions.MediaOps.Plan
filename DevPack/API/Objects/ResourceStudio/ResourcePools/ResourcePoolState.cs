namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents the state of a resource pool.
	/// </summary>
	public enum ResourcePoolState
	{
		/// <summary>
		/// The resource pool is in draft state.
		/// </summary>
		Draft,

		/// <summary>
		/// The resource pool is complete.
		/// </summary>
		Complete,

		/// <summary>
		/// The resource pool is deprecated.
		/// </summary>
		Deprecated,
	}
}

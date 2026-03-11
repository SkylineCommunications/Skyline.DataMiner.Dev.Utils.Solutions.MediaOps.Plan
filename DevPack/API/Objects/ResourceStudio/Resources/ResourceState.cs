namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents the state of a resource.
	/// </summary>
	public enum ResourceState
	{
		/// <summary>
		/// The resource is in draft state.
		/// </summary>
		Draft,

		/// <summary>
		/// The resource is complete.
		/// </summary>
		Complete,

		/// <summary>
		/// The resource is deprecated.
		/// </summary>
		Deprecated,
	}
}

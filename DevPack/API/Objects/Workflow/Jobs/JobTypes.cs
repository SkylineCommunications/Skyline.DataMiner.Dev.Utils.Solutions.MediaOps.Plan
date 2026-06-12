namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Provides identifiers for predefined job types.
	/// </summary>
	public class JobTypes
	{
		/// <summary>
		/// The identifier for the 'Scheduled' job type.
		/// This is the default type assigned to a job when no type is explicitly specified.
		/// </summary>
		public static readonly Guid Scheduled = new Guid("48476964-8d27-4dbf-a40d-959d40e5a33d");
	}
}
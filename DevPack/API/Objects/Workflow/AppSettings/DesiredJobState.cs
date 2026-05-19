namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Specifies the desired state in which a job is created.
	/// </summary>
	public enum DesiredJobState
	{
		/// <summary>
		/// Creates the job in draft state.
		/// </summary>
		Draft = 0,

		/// <summary>
		/// Creates the job in tentative state.
		/// </summary>
		Tentative = 1,
	}
}

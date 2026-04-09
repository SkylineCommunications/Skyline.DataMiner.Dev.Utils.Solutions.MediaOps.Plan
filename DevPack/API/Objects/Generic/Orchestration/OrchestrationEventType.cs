namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents the types of orchestration events that can occur during job handling.
	/// </summary>
	public enum OrchestrationEventType
	{
		/// <summary>
		/// Indicates the start of the preroll phase.
		/// </summary>
		PrerollStart,

		/// <summary>
		/// Indicates the end of the preroll phase.
		/// </summary>
		PrerollStop,

		/// <summary>
		/// Indicates the start of the postroll phase.
		/// </summary>
		PostrollStart,

		/// <summary>
		/// Indicates the end of the postroll phase.
		/// </summary>
		PostrollStop,
	}
}

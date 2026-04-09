namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents the details required to update the state of an orchestration event.
	/// </summary>
	public class OrchestrationUpdateDetails
	{
		/// <summary>
		/// Gets or sets the orchestration event to update.
		/// </summary>
		public OrchestrationEventType Event { get; set; }

		/// <summary>
		/// Gets or sets the new state for the orchestration event.
		/// </summary>
		public OrchestrationEventState EventState { get; set; }

		/// <summary>
		/// Gets or sets an optional message providing additional context about the state change.
		/// </summary>
		public string Message { get; set; }
	}
}

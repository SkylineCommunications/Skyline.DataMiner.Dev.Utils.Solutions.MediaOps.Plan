namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a reference configured on an orchestration event cannot be resolved to an actual value.
	/// </summary>
	public sealed class OrchestrationSettingsUnresolvedReferenceError : OrchestrationSettingsError
	{
		/// <summary>
		/// Gets the human-readable description of the reference that could not be resolved.
		/// </summary>
		public string Reference { get; internal set; }
	}
}

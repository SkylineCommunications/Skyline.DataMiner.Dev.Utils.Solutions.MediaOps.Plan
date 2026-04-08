namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a resource studio orchestration setting with invalid configuration.
	/// </summary>
	public class OrchestrationSettingsError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the resource studio orchestration settings.
		/// </summary>
		public Guid Id { get; set; }
	}
}

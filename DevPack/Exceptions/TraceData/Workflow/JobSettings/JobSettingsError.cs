namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a job setting with invalid configuration.
	/// </summary>
	public class JobSettingsError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the job settings.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}

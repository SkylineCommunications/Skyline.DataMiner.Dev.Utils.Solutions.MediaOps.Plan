namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Provides access to global configuration settings.
	/// </summary>
	public interface IGlobalSettings
	{
		/// <summary>
		/// Gets the settings used to configure the job.
		/// </summary>
		JobSettings GetJobSettings();

		/// <summary>
		/// Updates the job settings with the specified values and returns the updated settings.
		/// </summary>
		/// <param name="apiJobSetting">The job settings to apply. Cannot be null. The properties of this object determine the new configuration for the
		/// job.</param>
		/// <returns>The updated job settings after applying the changes.</returns>
		JobSettings UpdateJobSettings(JobSettings apiJobSetting);
	}
}

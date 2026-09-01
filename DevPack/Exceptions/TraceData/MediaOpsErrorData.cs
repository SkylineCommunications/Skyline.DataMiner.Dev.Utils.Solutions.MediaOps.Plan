namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Parent class for all ErrorData types.
	/// </summary>
	/// <seealso cref="CapabilityError"/>
	/// <seealso cref="CapacityError"/>
	/// <seealso cref="ConfigurationError"/>
	/// <seealso cref="JobError"/>
	/// <seealso cref="JobSettingsError"/>
	/// <seealso cref="OrchestrationSettingsError"/>
	/// <seealso cref="PropertyError"/>
	/// <seealso cref="PropertySettingCollectionError"/>
	/// <seealso cref="RecurringJobError"/>
	/// <seealso cref="ResourceError"/>
	/// <seealso cref="ResourcePoolError"/>
	/// <seealso cref="ResourcePropertyError"/>
	/// <seealso cref="SchedulingPropertyError"/>
	/// <seealso cref="WorkflowError"/>
	public class MediaOpsErrorData
	{
		/// <summary>
		/// Gets or sets the message that describes the error.
		/// </summary>
		public string ErrorMessage { get; internal set; }

		/// <summary>
		/// Returns the message that describes the error.
		/// </summary>
		/// <returns>The error message, or the type name when no message is available.</returns>
		public override string ToString()
		{
			return string.IsNullOrEmpty(ErrorMessage) ? GetType().Name : ErrorMessage;
		}
	}
}

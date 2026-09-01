namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a resource studio orchestration setting with invalid configuration.
	/// </summary>
	/// <seealso cref="OrchestrationSettingsInvalidCapabilitySettingsError"/>
	/// <seealso cref="OrchestrationSettingsInvalidCapacitySettingsError"/>
	/// <seealso cref="OrchestrationSettingsInvalidConfigurationSettingsError"/>
	/// <seealso cref="OrchestrationSettingsNotFoundError"/>
	/// <seealso cref="OrchestrationSettingsUnresolvedReferenceError"/>
	/// <seealso cref="OrchestrationSettingsValueAlreadyChangedError"/>
	public class OrchestrationSettingsError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the resource studio orchestration settings.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}

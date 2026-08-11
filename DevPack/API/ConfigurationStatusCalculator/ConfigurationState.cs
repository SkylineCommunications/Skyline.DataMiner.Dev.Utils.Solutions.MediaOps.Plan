namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents the calculated configuration status of the orchestration settings of a job, recurring job or one of their nodes.
	/// </summary>
	/// <remarks>
	/// This status is not stored on the DOM instances, it is always calculated based on the current orchestration settings.
	/// It is intended to be used as a visual indication only.
	/// </remarks>
	public enum ConfigurationState
	{
		/// <summary>
		/// The configuration status is unknown, for example because the node is not yet initialized.
		/// </summary>
		Unknown = -1,

		/// <summary>
		/// No capabilities, capacities, configurations or orchestration events are defined.
		/// </summary>
		NoParametersDefined = 0,

		/// <summary>
		/// At least one mandatory parameter has no value or reference, or an orchestration event is missing mandatory input.
		/// </summary>
		MandatoryValuesMissing = 1,

		/// <summary>
		/// All mandatory parameters are provided, but at least one optional parameter has no value or reference.
		/// </summary>
		NonMandatoryValuesMissing = 2,

		/// <summary>
		/// All defined parameters and orchestration events have a value or a reference.
		/// </summary>
		AllValuesProvided = 3,
	}
}

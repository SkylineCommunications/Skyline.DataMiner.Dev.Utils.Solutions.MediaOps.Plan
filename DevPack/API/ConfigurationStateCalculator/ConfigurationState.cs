namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents the calculated configuration state of the orchestration settings of a job, recurring job or one of their nodes.
	/// </summary>
	/// <remarks>
	/// This state is calculated and stored whenever the job is created or updated. It is intended to be used as a visual
	/// indication only and does not necessarily reflect the current orchestration settings.
	/// </remarks>
	public enum ConfigurationState
	{
		/// <summary>
		/// The configuration state is unknown, for example because it was never calculated.
		/// </summary>
		Unknown = 0,

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

		/// <summary>
		/// No capabilities, capacities, configurations or orchestration events are defined.
		/// </summary>
		NoParametersDefined = 4,
	}
}

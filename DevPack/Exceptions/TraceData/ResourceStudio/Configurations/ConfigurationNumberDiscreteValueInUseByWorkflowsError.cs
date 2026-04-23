namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a configuration number discrete value is referenced by one or multiple workflows.
	/// </summary>
	public sealed class ConfigurationNumberDiscreteValueInUseByWorkflowsError : ConfigurationNumberDiscreteValueInUseError
	{
		/// <summary>
		/// Ids of the workflows referencing the configuration number discrete value.
		/// </summary>
		public IReadOnlyCollection<Guid> WorkflowIds { get; internal set; } = new List<Guid>();
	}
}

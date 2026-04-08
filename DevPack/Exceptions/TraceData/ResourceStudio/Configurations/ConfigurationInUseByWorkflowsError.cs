namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a configuration is referenced by one or multiple workflows.
	/// </summary>
	public class ConfigurationInUseByWorkflowsError : ConfigurationInUseError
	{
		/// <summary>
		/// Ids of the workflows referencing the configuration.
		/// </summary>
		public IReadOnlyCollection<Guid> WorkflowIds { get; internal set; } = new List<Guid>();
	}
}

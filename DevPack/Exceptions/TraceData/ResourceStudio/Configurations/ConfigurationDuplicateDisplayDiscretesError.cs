namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a configuration contains duplicate discrete display values.
	/// </summary>
	public sealed class ConfigurationDuplicateDisplayDiscretesError : ConfigurationInvalidDiscretesError
	{
		/// <summary>
		/// Gets the collection of duplicate discrete display values.
		/// </summary>
		public IReadOnlyList<string> DisplayValues { get; internal set; } = Array.Empty<string>();
	}
}

namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a discrete text configuration contains duplicate raw discrete values.
	/// </summary>
	public sealed class ConfigurationDuplicateTextDiscretesError : ConfigurationDuplicateDiscretesError
	{
		/// <summary>
		/// Gets the collection of duplicate discrete string values.
		/// </summary>
		public IReadOnlyList<string> Discretes { get; internal set; } = Array.Empty<string>();
	}
}

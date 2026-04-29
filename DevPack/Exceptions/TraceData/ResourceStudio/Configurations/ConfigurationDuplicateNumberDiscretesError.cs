namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a discrete number configuration contains duplicate raw discrete values.
	/// </summary>
	public sealed class ConfigurationDuplicateNumberDiscretesError : ConfigurationDuplicateDiscretesError
	{
		/// <summary>
		/// Gets the collection of duplicate discrete decimal values.
		/// </summary>
		public IReadOnlyList<decimal> Discretes { get; internal set; } = Array.Empty<decimal>();
	}
}

namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a discrete number configuration contains duplicate raw discrete values.
	/// </summary>
	public sealed class ConfigurationDuplicateNumberDiscretesError : ConfigurationDuplicateDiscretesError
	{
		/// <summary>
		/// Gets or sets the collection of duplicate discrete decimal values.
		/// </summary>
		public List<decimal> Discretes { get; internal set; }
	}
}

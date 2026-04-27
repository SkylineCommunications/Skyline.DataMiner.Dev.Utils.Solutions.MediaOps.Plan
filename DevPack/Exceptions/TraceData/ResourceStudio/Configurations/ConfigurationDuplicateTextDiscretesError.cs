namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a discrete text configuration contains duplicate raw discrete values.
	/// </summary>
	public sealed class ConfigurationDuplicateTextDiscretesError : ConfigurationDuplicateDiscretesError
	{
		/// <summary>
		/// Gets or sets the collection of duplicate discrete string values.
		/// </summary>
		public List<string> Discretes { get; internal set; }
	}
}

namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a discrete property configuration contains duplicate discrete values.
	/// </summary>
	public sealed class PropertyDuplicateDiscretesError : PropertyInvalidDiscretesError
	{
		/// <summary>
		/// Gets or sets the collection of discrete string values.
		/// </summary>
		public List<string> Discretes { get; internal set; }
	}
}

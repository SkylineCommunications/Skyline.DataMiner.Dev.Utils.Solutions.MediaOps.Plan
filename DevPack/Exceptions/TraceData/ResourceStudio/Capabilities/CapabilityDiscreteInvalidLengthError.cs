namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Represents an error that occurs when a capability specifies one or multiple discrete values with an invalid length.
	/// </summary>
	public class CapabilityDiscreteInvalidLengthError : CapabilityInvalidDiscretesError
	{
		/// <summary>
		/// Gets the collection of discrete values that have an invalid length.
		/// </summary>
		public IReadOnlyCollection<string> InvalidDiscretes { get; internal set; }

		/// <summary>
		/// Gets the maximum allowed length for discrete values.
		/// </summary>
		public int MaxLength { get; internal set; } = InputValidator.DefaultMaxTextLength;
	}
}

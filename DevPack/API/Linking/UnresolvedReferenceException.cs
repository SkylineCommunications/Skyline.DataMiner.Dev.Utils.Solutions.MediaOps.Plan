namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

	/// <summary>
	/// Represents an exception that is thrown when a data reference cannot be resolved.
	/// </summary>
	public sealed class UnresolvedReferenceException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UnresolvedReferenceException"/> class.
		/// </summary>
		/// <param name="reference">The reference that could not be resolved.</param>
		public UnresolvedReferenceException(DataReference reference)
			: base($"Unable to resolve reference of Type '{reference?.Type}'.")
		{
			Reference = reference;
		}

		/// <summary>
		/// Gets the reference that could not be resolved.
		/// </summary>
		public DataReference Reference { get; }
	}
}

namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents the result of validating all references in a <see cref="Job"/>.
	/// </summary>
	public class JobReferenceValidationResult
	{
		private readonly ICollection<DataReference> unresolvedReferences;

		internal JobReferenceValidationResult(ICollection<DataReference> unresolvedReferences)
		{
			this.unresolvedReferences = unresolvedReferences ?? throw new ArgumentNullException(nameof(unresolvedReferences));
		}

		/// <summary>
		/// Gets a value indicating whether all references in the job could be resolved.
		/// </summary>
		public bool IsValid => unresolvedReferences.Count == 0;

		/// <summary>
		/// Gets the collection of references that could not be resolved to an actual value.
		/// </summary>
		public IReadOnlyCollection<DataReference> UnresolvedReferences => new List<DataReference>(unresolvedReferences).AsReadOnly();
	}
}

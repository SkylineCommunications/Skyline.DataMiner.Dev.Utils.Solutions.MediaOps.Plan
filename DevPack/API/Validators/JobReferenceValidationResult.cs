namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents the result of validating all references in a <see cref="Job"/>.
	/// </summary>
	public class JobReferenceValidationResult
	{
		private JobReferenceValidationResult(IEnumerable<DataReference> unresolvedReferences)
		{
			if (unresolvedReferences is null)
			{
				throw new ArgumentNullException(nameof(unresolvedReferences));
			}

			UnresolvedReferences = new List<DataReference>(unresolvedReferences).AsReadOnly();
		}

		/// <summary>
		/// Gets the collection of references that could not be resolved to an actual value.
		/// </summary>
		public IReadOnlyCollection<DataReference> UnresolvedReferences { get; }

		/// <summary>
		/// Gets a value indicating whether all references in the job could be resolved.
		/// </summary>
		public bool IsValid => UnresolvedReferences.Count == 0;

		/// <summary>
		/// Creates a <see cref="JobReferenceValidationResult"/> from a collection of unresolved references.
		/// </summary>
		/// <param name="unresolvedReferences">The collection of unresolved references.</param>
		/// <returns>A new instance of <see cref="JobReferenceValidationResult"/>.</returns>
		public static JobReferenceValidationResult Create(IEnumerable<DataReference> unresolvedReferences)
		{
			if (unresolvedReferences is null)
			{
				throw new ArgumentNullException(nameof(unresolvedReferences));
			}

			return new JobReferenceValidationResult(unresolvedReferences);
		}
	}
}

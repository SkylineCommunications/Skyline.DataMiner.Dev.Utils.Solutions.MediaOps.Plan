namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents the outcome of resolving the settings references of a <see cref="Job"/>.
	/// </summary>
	internal sealed class JobReferenceResolution
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JobReferenceResolution"/> class.
		/// </summary>
		/// <param name="unresolvedReferences">The references that could not be resolved to a value.</param>
		/// <param name="resolvedReferences">The references that resolved to a value, mapped to their resolved value.</param>
		public JobReferenceResolution(
			IReadOnlyCollection<DataReference> unresolvedReferences,
			IReadOnlyDictionary<DataReference, ResolvedValue> resolvedReferences)
		{
			UnresolvedReferences = unresolvedReferences ?? throw new ArgumentNullException(nameof(unresolvedReferences));
			ResolvedReferences = resolvedReferences ?? throw new ArgumentNullException(nameof(resolvedReferences));
		}

		/// <summary>
		/// Gets the references that could not be resolved to a value.
		/// </summary>
		public IReadOnlyCollection<DataReference> UnresolvedReferences { get; }

		/// <summary>
		/// Gets the references that resolved to a value, mapped to their resolved value.
		/// </summary>
		public IReadOnlyDictionary<DataReference, ResolvedValue> ResolvedReferences { get; }

		/// <summary>
		/// Gets a value indicating whether all references resolved to a value.
		/// </summary>
		public bool IsValid => UnresolvedReferences.Count == 0;
	}
}

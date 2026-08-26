namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	/// <summary>
	/// Represents the outcome of resolving the settings references of a <see cref="Job"/>.
	/// </summary>
	internal sealed class JobReferenceResolution
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JobReferenceResolution"/> class.
		/// </summary>
		/// <param name="unresolvedReferences">The references that could not be resolved to a value.</param>
		/// <param name="resolvedReferences">The values that were resolved, per node and reference.</param>
		public JobReferenceResolution(
			IReadOnlyCollection<DataReference> unresolvedReferences,
			ResolvedReferenceCache resolvedReferences)
		{
			UnresolvedReferences = unresolvedReferences ?? throw new ArgumentNullException(nameof(unresolvedReferences));
			ResolvedReferences = resolvedReferences ?? throw new ArgumentNullException(nameof(resolvedReferences));
		}

		/// <summary>
		/// Gets the references that could not be resolved to a value.
		/// </summary>
		public IReadOnlyCollection<DataReference> UnresolvedReferences { get; }

		/// <summary>
		/// Gets the values that were resolved, per node and reference.
		/// </summary>
		public ResolvedReferenceCache ResolvedReferences { get; }

		/// <summary>
		/// Gets a value indicating whether all references resolved to a value.
		/// </summary>
		public bool IsValid => UnresolvedReferences.Count == 0;
	}
}

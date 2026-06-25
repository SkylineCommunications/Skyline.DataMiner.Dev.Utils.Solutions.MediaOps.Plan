namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Holds the references that were resolved for a job during a create-or-update operation, keyed by the
	/// <see cref="DataReference"/> that produced them, so the resolved values are available later in the pipeline
	/// (for example when building the core resource usages) without resolving them again.
	/// </summary>
	internal sealed class ResolvedReferenceCache
	{
		private IReadOnlyDictionary<DataReference, ResolvedValue> resolvedReferences = new Dictionary<DataReference, ResolvedValue>();

		/// <summary>
		/// Gets the resolved references keyed by the reference that produced them.
		/// </summary>
		public IReadOnlyDictionary<DataReference, ResolvedValue> ResolvedReferences => resolvedReferences;

		/// <summary>
		/// Replaces the cached resolved references with the specified collection.
		/// </summary>
		/// <param name="references">The resolved references keyed by the reference that produced them.</param>
		public void SetCache(IReadOnlyDictionary<DataReference, ResolvedValue> references)
		{
			resolvedReferences = references ?? throw new ArgumentNullException(nameof(references));
		}

		/// <summary>
		/// Tries to get the resolved value for the specified reference.
		/// </summary>
		/// <param name="reference">The reference to look up.</param>
		/// <param name="value">When this method returns, contains the resolved value if found; otherwise <see langword="null"/>.</param>
		/// <returns><see langword="true"/> if a resolved value was found; otherwise <see langword="false"/>.</returns>
		public bool TryGetValue(DataReference reference, out ResolvedValue value)
		{
			if (reference == null)
			{
				value = null;
				return false;
			}

			return resolvedReferences.TryGetValue(reference, out value);
		}
	}
}

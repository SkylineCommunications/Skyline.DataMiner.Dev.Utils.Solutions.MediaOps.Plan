namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Holds the references that were resolved for a job during a create-or-update operation, keyed by the node they
	/// were resolved for and the <see cref="DataReference"/> that produced them, so the resolved values are available
	/// later in the pipeline (for example when building the core resource usages) without resolving them again.
	/// </summary>
	internal sealed class ResolvedReferenceCache
	{
		private readonly Dictionary<(string NodeId, DataReference Reference), ResolvedValue> resolvedReferences = new Dictionary<(string, DataReference), ResolvedValue>();
		
		/// <summary>
		/// Gets the number of resolved references held by this cache.
		/// </summary>
		public int Count => resolvedReferences.Count;

		/// <summary>
		/// Stores the value that the specified reference resolved to.
		/// </summary>
		/// <param name="nodeId">The identifier of the node the reference was resolved for, or <see langword="null"/> for the job level.</param>
		/// <param name="reference">The reference that produced the value.</param>
		/// <param name="value">The resolved value.</param>
		public void Set(string nodeId, DataReference reference, ResolvedValue value)
		{
			if (reference == null)
			{
				throw new ArgumentNullException(nameof(reference));
			}

			resolvedReferences[(nodeId, reference)] = value;
		}

		/// <summary>
		/// Stores every resolved reference held by the specified cache.
		/// </summary>
		/// <param name="references">The cache to take the resolved references from.</param>
		public void Set(ResolvedReferenceCache references)
		{
			if (references == null)
			{
				throw new ArgumentNullException(nameof(references));
			}

			foreach (var entry in references.resolvedReferences)
			{
				resolvedReferences[entry.Key] = entry.Value;
			}
		}

		/// <summary>
		/// Removes every resolved reference from this cache.
		/// </summary>
		public void Clear()
		{
			resolvedReferences.Clear();
		}

		/// <summary>
		/// Determines whether a value was already resolved for the specified reference.
		/// </summary>
		/// <param name="nodeId">The identifier of the node the reference was resolved for, or <see langword="null"/> for the job level.</param>
		/// <param name="reference">The reference to look up.</param>
		public bool Contains(string nodeId, DataReference reference)
		{
			return reference != null && resolvedReferences.ContainsKey((nodeId, reference));
		}

		/// <summary>
		/// Tries to get the resolved value for the specified reference.
		/// </summary>
		/// <param name="nodeId">The identifier of the node the reference was resolved for, or <see langword="null"/> for the job level.</param>
		/// <param name="reference">The reference to look up.</param>
		/// <param name="value">When this method returns, contains the resolved value if found; otherwise <see langword="null"/>.</param>
		/// <returns><see langword="true"/> if a resolved value was found; otherwise <see langword="false"/>.</returns>
		public bool TryGetValue(string nodeId, DataReference reference, out ResolvedValue value)
		{
			if (reference == null)
			{
				value = null;
				return false;
			}

			return resolvedReferences.TryGetValue((nodeId, reference), out value);
		}
	}
}

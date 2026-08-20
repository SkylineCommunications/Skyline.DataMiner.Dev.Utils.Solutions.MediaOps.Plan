namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	/// <summary>
	/// Represents a Resource Studio item whose DOM configuration is not in sync with its CORE counterpart.
	/// </summary>
	public abstract class SynchronizationItem
	{
		private protected SynchronizationItem(Guid id, string name, bool coreObjectExists, IEnumerable<SynchronizationDifference> differences, IEnumerable<MediaOpsErrorData> blockers)
		{
			if (id == Guid.Empty)
			{
				throw new ArgumentException("Id cannot be empty.", nameof(id));
			}

			Id = id;
			Name = name;
			CoreObjectExists = coreObjectExists;
			Differences = new List<SynchronizationDifference>(differences ?? []).AsReadOnly();
			Blockers = new List<MediaOpsErrorData>(blockers ?? []).AsReadOnly();
		}

		/// <summary>
		/// Gets the identifier of the DOM item.
		/// </summary>
		public Guid Id { get; }

		/// <summary>
		/// Gets the name of the item as configured in DOM.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets a value indicating whether a CORE counterpart exists for this item.
		/// </summary>
		public bool CoreObjectExists { get; }

		/// <summary>
		/// Gets the differences between the DOM configuration and the CORE configuration.
		/// </summary>
		public IReadOnlyCollection<SynchronizationDifference> Differences { get; }

		/// <summary>
		/// Gets the problems that prevent this item from being synchronized, such as a duplicate name or an invalid link.
		/// </summary>
		public IReadOnlyCollection<MediaOpsErrorData> Blockers { get; }

		/// <summary>
		/// Gets a value indicating whether this item can be synchronized.
		/// </summary>
		public bool CanSynchronize => Blockers.Count == 0;
	}
}

namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	/// <summary>
	/// Represents a resource pool whose DOM configuration is not in sync with its CORE counterpart.
	/// </summary>
	public sealed class ResourcePoolSynchronizationItem : SynchronizationItem
	{
		internal ResourcePoolSynchronizationItem(Guid id, string name, bool coreObjectExists, IEnumerable<SynchronizationDifference> differences, IEnumerable<MediaOpsErrorData> blockers)
			: base(id, name, coreObjectExists, differences, blockers)
		{
		}
	}
}

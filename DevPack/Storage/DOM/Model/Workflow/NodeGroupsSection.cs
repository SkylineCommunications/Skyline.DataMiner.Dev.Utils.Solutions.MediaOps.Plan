namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow
{
	using System;

	internal partial class NodeGroupsSection
	{
		/// <summary>
		/// Gets the identifier of the group. The ID is stored as a string; groups that were stored before the group ID
		/// field existed, or whose stored value is not a valid GUID, fall back to the ID of their DOM section, which is
		/// stable across reads and is persisted as the group ID on the next save.
		/// </summary>
		internal Guid EffectiveGroupId => Guid.TryParse(GroupID, out var id) && id != Guid.Empty ? id : ID.Id;
	}
}

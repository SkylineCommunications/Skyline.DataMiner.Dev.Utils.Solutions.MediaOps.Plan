namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow
{
	using System;

	internal partial class NodeGroupsSection
	{
		/// <summary>
		/// Gets the identifier of the group. Groups that were stored before the group ID field existed fall back to the
		/// ID of their DOM section, which is stable across reads; it is persisted as the group ID on the next save.
		/// </summary>
		internal string EffectiveGroupId => String.IsNullOrWhiteSpace(GroupID) ? ID.Id.ToString() : GroupID;
	}
}

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

		/// <summary>
		/// Gets the identifier of the group, given the identifiers already used by other groups of the same instance.
		/// </summary>
		/// <remarks>
		/// When the <see cref="EffectiveGroupId"/> is already used, the section ID is used instead because it is unique
		/// within the instance and stable across reads. A random ID is only used when that one is taken as well.
		/// </remarks>
		/// <param name="isInUse">Determines whether an identifier is already used by another group.</param>
		/// <returns>An identifier that is not in use.</returns>
		internal Guid GetUniqueGroupId(Func<Guid, bool> isInUse)
		{
			if (!isInUse(EffectiveGroupId))
			{
				return EffectiveGroupId;
			}

			return isInUse(ID.Id) ? Guid.NewGuid() : ID.Id;
		}
	}
}

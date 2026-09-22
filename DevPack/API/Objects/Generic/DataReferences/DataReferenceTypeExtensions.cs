namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Extension methods for <see cref="DataReferenceType"/>.
	/// </summary>
	public static class DataReferenceTypeExtensions
	{
		/// <summary>
		/// Returns <c>true</c> when the reference type is scoped to a specific workflow node
		/// (i.e. it carries resource/scheduling information that differs per node).
		/// </summary>
		public static bool IsNodeScoped(this DataReferenceType type)
		{
			switch (type)
			{
				case DataReferenceType.ResourceName:
				case DataReferenceType.ResourceProperty:
				case DataReferenceType.ResourceLinkedObjectID:
				case DataReferenceType.CapabilityParameter:
				case DataReferenceType.CapacityParameter:
				case DataReferenceType.ConfigurationParameter:
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Returns <c>true</c> when the reference type can target a specific workflow node. This is broader than
		/// <see cref="IsNodeScoped"/>: a job property can be defined on the job itself or on one of its nodes, so it
		/// can target a node without carrying the resource/scheduling information that differs per node.
		/// </summary>
		/// <remarks>
		/// A type that is node-scoped is always resolved against a node, so a reference that does not name one falls
		/// back to the node holding it. A job property does not: an empty node identifier deliberately means the
		/// property is read from the job itself.
		/// </remarks>
		public static bool SupportsNodeScope(this DataReferenceType type)
		{
			return type.IsNodeScoped() || type == DataReferenceType.JobProperty;
		}
	}
}

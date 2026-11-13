namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a virtual signal group exists but has a role that does not match its usage on the resource:
	/// the input virtual signal group must be a destination and the output virtual signal group must be a source.
	/// </summary>
	public sealed class ResourceInvalidVirtualSignalGroupRoleError : ResourceError
	{
		/// <summary>
		/// Gets the unique identifier for the associated virtual signal group.
		/// </summary>
		public Guid VirtualSignalGroupId { get; internal set; }
	}
}

namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a virtual signal group is invalid.
	/// </summary>
	public sealed class ResourceInvalidVirtualSignalGroupError : ResourceError
	{
		/// <summary>
		/// Gets or sets the unique identifier for the associated virtual signal group.
		/// </summary>
		public Guid VirtualSignalGroupId { get; internal set; }
	}
}

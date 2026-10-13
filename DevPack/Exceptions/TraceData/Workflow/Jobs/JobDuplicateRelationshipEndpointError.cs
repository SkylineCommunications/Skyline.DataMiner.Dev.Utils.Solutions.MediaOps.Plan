namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when more than one link of a job or recurring job points at the same object.
	/// </summary>
	public sealed class JobDuplicateRelationshipEndpointError : JobError
	{
		/// <summary>
		/// Gets the unique identifier of the type of the object that more than one link points at.
		/// </summary>
		public Guid ObjectTypeId { get; internal set; }

		/// <summary>
		/// Gets the identifier of the object that more than one link points at.
		/// </summary>
		public string ObjectId { get; internal set; }
	}
}

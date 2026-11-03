namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a reference configured on a job cannot be resolved to an actual value.
	/// </summary>
	public sealed class JobUnresolvedReferenceError : JobError
	{
		/// <summary>
		/// Gets the human-readable description of the reference that could not be resolved.
		/// </summary>
		public string Reference { get; internal set; }

		/// <summary>
		/// Gets the ID of the node whose settings hold the reference, or <see langword="null"/> when the job itself holds it.
		/// </summary>
		public string NodeId { get; internal set; }

		/// <summary>
		/// Gets the ID of the capability, capacity or configuration setting that holds the reference, or <see langword="null"/> when
		/// the reference is used by an orchestration event.
		/// </summary>
		public Guid? SettingId { get; internal set; }
	}
}

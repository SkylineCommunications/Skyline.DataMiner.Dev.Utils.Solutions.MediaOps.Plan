namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a scheduling property with invalid configuration.
	/// </summary>
	public class SchedulingPropertyError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the scheduling property.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}

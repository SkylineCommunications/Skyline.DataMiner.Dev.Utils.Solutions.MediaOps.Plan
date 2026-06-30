namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a job cannot be confirmed because one of its nodes is missing mandatory
	/// configuration values.
	/// </summary>
	public sealed class JobMandatoryConfigurationMissingError : JobError
	{
		/// <summary>
		/// Gets the unique identifier of the job node that is missing mandatory configuration values.
		/// </summary>
		public string NodeId { get; internal set; }
	}
}

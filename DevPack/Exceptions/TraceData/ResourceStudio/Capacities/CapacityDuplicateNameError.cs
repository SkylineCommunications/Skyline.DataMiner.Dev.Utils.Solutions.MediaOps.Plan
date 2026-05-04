namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a capacity configuration contains a duplicate capacity name.
	/// </summary>
	/// <remarks>This can only occur when capacities with the same name are provided to a bulk operation.</remarks>
	public sealed class CapacityDuplicateNameError : CapacityError
	{
		/// <summary>
		/// Gets the name of the capacity.
		/// </summary>
		public string Name { get; internal set; }
	}
}

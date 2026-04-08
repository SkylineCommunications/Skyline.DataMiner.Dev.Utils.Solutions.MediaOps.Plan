namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a capability configuration name already exists.
	/// </summary>
	public class CapabilityNameExistsError : CapabilityError
	{
		/// <summary>
		/// Gets the name of the capability.
		/// </summary>
		public string Name { get; set; }
	}
}

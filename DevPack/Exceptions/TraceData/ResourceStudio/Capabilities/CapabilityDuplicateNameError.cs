namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a capability configuration contains a duplicate capability name.
	/// </summary>
	/// <remarks>This can only occur when capabilities with the same name are provided to a bulk operation.</remarks>
	public sealed class CapabilityDuplicateNameError : CapabilityError
	{
		/// <summary>
		/// Gets the name of the capability.
		/// </summary>
		public string Name { get; internal set; }
	}
}

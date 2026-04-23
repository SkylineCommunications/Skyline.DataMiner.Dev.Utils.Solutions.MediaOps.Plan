namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Represents an error that occurs when attempting to delete a configuration text discrete value that is currently in use.
	/// </summary>
	public class ConfigurationTextDiscreteValueInUseError : ConfigurationError
	{
		/// <summary>
		/// The discrete value that is in use.
		/// </summary>
		public TextDiscreet DiscreteValue { get; internal set; }
	}
}

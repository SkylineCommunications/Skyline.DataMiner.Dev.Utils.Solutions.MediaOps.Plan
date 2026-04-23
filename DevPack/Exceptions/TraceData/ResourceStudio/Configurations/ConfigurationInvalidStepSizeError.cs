namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when the specified step size in a configuration configuration is invalid.
	/// </summary>
	public sealed class ConfigurationInvalidStepSizeError : ConfigurationError
	{
		/// <summary>
		/// Gets or sets the step size.
		/// </summary>
		public decimal StepSize { get; internal set; }
	}
}

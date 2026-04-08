namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a resource property is configured with invalid or unsupported settings.
	/// </summary>
	public class ResourceInvalidPropertySettingsError : ResourceError
	{
		/// <summary>
		/// Gets the unique identifier of the property with the invalid settings.
		/// </summary>
		public Guid PropertyId { get; set; }
	}
}

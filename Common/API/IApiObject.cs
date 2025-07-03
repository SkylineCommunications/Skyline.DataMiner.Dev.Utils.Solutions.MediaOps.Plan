namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

	/// <summary>
	/// Represents a base interface for API objects with a unique identifier.
	/// </summary>
	public interface IApiObject
	{
		/// <summary>
		/// Gets the unique identifier of the API object.
		/// </summary>
		Guid Id { get; }
	}
}

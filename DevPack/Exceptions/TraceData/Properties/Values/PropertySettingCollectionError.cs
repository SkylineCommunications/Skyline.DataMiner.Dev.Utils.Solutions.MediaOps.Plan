namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a property setting collection with invalid configuration.
	/// </summary>
	public class PropertySettingCollectionError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the property setting collection.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}

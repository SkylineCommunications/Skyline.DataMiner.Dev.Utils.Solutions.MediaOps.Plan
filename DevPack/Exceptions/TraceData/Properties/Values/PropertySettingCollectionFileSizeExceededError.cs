namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a file of a file property setting exceeds the maximum document size configured in DataMiner.
	/// </summary>
	public sealed class PropertySettingCollectionFileSizeExceededError : PropertySettingCollectionError
	{
		/// <summary>
		/// Gets the unique identifier for the property.
		/// </summary>
		public Guid PropertyId { get; internal set; }

		/// <summary>
		/// Gets the name of the file that could not be stored.
		/// </summary>
		public string FileName { get; internal set; }

		/// <summary>
		/// Gets the size of the file, in bytes.
		/// </summary>
		public long FileSize { get; internal set; }

		/// <summary>
		/// Gets the maximum document size allowed by DataMiner, in bytes, or 0 when the limit could not be retrieved.
		/// </summary>
		public long MaxFileSize { get; internal set; }
	}
}

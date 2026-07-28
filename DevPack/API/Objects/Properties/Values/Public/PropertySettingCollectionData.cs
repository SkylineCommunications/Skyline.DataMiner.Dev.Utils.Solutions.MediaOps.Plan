namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Input data used to initialize a new <see cref="PropertySettingCollection"/>.
	/// </summary>
	public sealed class PropertySettingCollectionData
	{
		/// <summary>
		/// Gets or sets the identifier of the object this collection is linked to.
		/// </summary>
		public string LinkedObjectId { get; set; }

		/// <summary>
		/// Gets or sets the scope of this property setting collection.
		/// </summary>
		public string Scope { get; set; }

		/// <summary>
		/// Gets or sets the optional sub-identifier for this property setting collection.
		/// </summary>
		public string SubId { get; set; }
	}
}

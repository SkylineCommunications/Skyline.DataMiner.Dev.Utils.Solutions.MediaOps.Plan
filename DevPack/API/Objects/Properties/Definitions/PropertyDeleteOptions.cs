namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents options for deleting a property.
	/// </summary>
	public class PropertyDeleteOptions
	{
		/// <summary>
		/// Gets or sets a value indicating whether to force delete the property.
		/// Values linked to this property will be silently deleted when updating the collections of which these property values are part.
		/// </summary>
		public bool ForceDelete { get; set; } = false;

		internal static PropertyDeleteOptions GetDefaults()
		{
			return new PropertyDeleteOptions
			{
				ForceDelete = false,
			};
		}
	}
}

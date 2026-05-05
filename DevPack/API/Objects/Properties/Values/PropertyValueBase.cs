namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Provides a base class for property values.
	/// </summary>
	public abstract class PropertyValueBase : TrackableObject
	{
		private protected PropertyValueBase()
		{
			IsNew = true;
		}

		/// <summary>
		/// Gets the name of the property value.
		/// </summary>
		public string Name { get; protected set; }
	}
}

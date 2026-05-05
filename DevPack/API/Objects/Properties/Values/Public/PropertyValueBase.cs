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
	}
}

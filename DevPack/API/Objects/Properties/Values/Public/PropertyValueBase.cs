namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Provides a base class for property values.
	/// </summary>
	public abstract class PropertyValueBase : TrackableObject
	{
		private protected PropertyValueBase(bool isNew = false)
		{
			IsNew = isNew;
		}

		private protected PropertyValueBase(PropertyValueBase propertyValueBase)
		{
			IsNew = true;
		}

		/// <summary>
		/// Gets a value indicating whether this setting has a value defined.
		/// </summary>
		public virtual bool HasValue { get; }

		internal virtual Storage.DOM.DomSectionBase OriginalSection { get; }
	}
}

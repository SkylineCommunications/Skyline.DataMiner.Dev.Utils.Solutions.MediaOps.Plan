namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Provides a base class for property settings.
	/// </summary>
	public abstract class PropertySettingBase : TrackableObject
	{
		private protected PropertySettingBase(bool isNew = false)
		{
			IsNew = isNew;
		}

		private protected PropertySettingBase(PropertySettingBase propertySettingBase)
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

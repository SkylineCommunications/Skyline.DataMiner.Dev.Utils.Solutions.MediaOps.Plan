namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Provides a base class for property settings.
	/// </summary>
	/// <seealso cref="CustomPropertySetting"/>
	/// <seealso cref="PropertySetting"/>
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

		/// <summary>
		/// Determines whether this setting is a custom property setting and, if so, returns it as a <see cref="CustomPropertySetting"/>.
		/// </summary>
		/// <param name="setting">When this method returns, contains the current setting as a <see cref="CustomPropertySetting"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this setting is a <see cref="CustomPropertySetting"/>; otherwise, <c>false</c>.</returns>
		public bool IsCustomPropertySetting(out CustomPropertySetting setting)
		{
			setting = this as CustomPropertySetting;
			return setting != null;
		}

		/// <summary>
		/// Determines whether this setting is linked to a <see cref="Property"/> definition and, if so, returns it as a <see cref="PropertySetting"/>.
		/// </summary>
		/// <param name="setting">When this method returns, contains the current setting as a <see cref="PropertySetting"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this setting is a <see cref="PropertySetting"/>; otherwise, <c>false</c>.</returns>
		public bool IsPropertySetting(out PropertySetting setting)
		{
			setting = this as PropertySetting;
			return setting != null;
		}
	}
}

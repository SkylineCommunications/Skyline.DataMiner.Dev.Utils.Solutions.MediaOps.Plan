namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents an abstract base class for all settings that can hold either a direct value or a data reference.
	/// </summary>
	/// <seealso cref="CapabilitySetting"/>
	/// <seealso cref="CapacitySetting"/>
	/// <seealso cref="ConfigurationSetting"/>
	public abstract class Setting : TrackableObject
	{
		private protected Setting()
		{
		}

		private protected Setting(Guid id)
		{
			if (id == Guid.Empty)
			{
				throw new ArgumentException(nameof(id));
			}

			Id = id;

			IsNew = true;
		}

		private protected Setting(Setting setting)
		{
			Id = setting.Id;
			Reference = setting.Reference;

			IsNew = true;
		}

		/// <summary>
		/// Gets the unique identifier of the setting.
		/// </summary>
		public Guid Id { get; internal set; }

		/// <summary>
		/// Gets or sets a reference to a data source that provides the value for this setting.
		/// </summary>
		public DataReference Reference { get; set; }

		/// <summary>
		/// Gets a value indicating whether this setting has a reference defined.
		/// </summary>
		public bool HasReference => Reference != null;

		/// <summary>
		/// Gets a value indicating whether this setting has a value defined.
		/// </summary>
		public abstract bool HasValue { get; }

		internal virtual Storage.DOM.DomSectionBase OriginalSection { get; }

		/// <summary>
		/// Determines whether this setting is a capability setting and, if so, returns it as a <see cref="CapabilitySetting"/>.
		/// </summary>
		/// <param name="setting">When this method returns, contains the current setting as a <see cref="CapabilitySetting"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this setting is a <see cref="CapabilitySetting"/>; otherwise, <c>false</c>.</returns>
		public bool IsCapabilitySetting(out CapabilitySetting setting)
		{
			setting = this as CapabilitySetting;
			return setting != null;
		}

		/// <summary>
		/// Determines whether this setting is a capacity setting and, if so, returns it as a <see cref="CapacitySetting"/>.
		/// </summary>
		/// <param name="setting">When this method returns, contains the current setting as a <see cref="CapacitySetting"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this setting is a <see cref="CapacitySetting"/>; otherwise, <c>false</c>.</returns>
		public bool IsCapacitySetting(out CapacitySetting setting)
		{
			setting = this as CapacitySetting;
			return setting != null;
		}

		/// <summary>
		/// Determines whether this setting is a configuration setting and, if so, returns it as a <see cref="ConfigurationSetting"/>.
		/// </summary>
		/// <param name="setting">When this method returns, contains the current setting as a <see cref="ConfigurationSetting"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this setting is a <see cref="ConfigurationSetting"/>; otherwise, <c>false</c>.</returns>
		public bool IsConfigurationSetting(out ConfigurationSetting setting)
		{
			setting = this as ConfigurationSetting;
			return setting != null;
		}
	}
}

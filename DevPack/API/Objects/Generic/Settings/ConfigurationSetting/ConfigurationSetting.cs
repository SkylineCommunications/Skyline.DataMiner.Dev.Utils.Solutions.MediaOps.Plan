namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents an abstract base class for settings associated with a specific configuration.
	/// </summary>
	public abstract class ConfigurationSetting : Setting
	{
		private protected ConfigurationSetting(Configuration configuration)
			: this(configuration?.Id ?? throw new ArgumentNullException(nameof(configuration)))
		{
		}

		private protected ConfigurationSetting(Guid configurationId)
			: base(configurationId)
		{
		}

		private protected ConfigurationSetting()
		{
		}

		private protected ConfigurationSetting(ConfigurationSetting configurationSetting)
			: base(configurationSetting)
		{
		}

		/// <summary>
		/// Gets the unique identifier of the configuration.
		/// </summary>
		public new Guid Id { get => base.Id; internal set => base.Id = value; }

		/// <summary>
		/// Determines whether this configuration setting represents a numeric configuration and, if so, returns it as a <see cref="NumberConfigurationSetting"/>.
		/// </summary>
		/// <param name="setting">When this method returns, contains the current configuration setting as a <see cref="NumberConfigurationSetting"/> when it represents a numeric configuration; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this configuration setting represents a numeric configuration; otherwise, <c>false</c>.</returns>
		public bool IsNumberConfiguration(out NumberConfigurationSetting setting)
		{
			setting = this as NumberConfigurationSetting;
			return setting != null;
		}

		/// <summary>
		/// Determines whether this configuration setting represents a discrete numeric configuration and, if so, returns it as a <see cref="DiscreteNumberConfigurationSetting"/>.
		/// </summary>
		/// <param name="setting">When this method returns, contains the current configuration setting as a <see cref="DiscreteNumberConfigurationSetting"/> when it represents a discrete numeric configuration; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this configuration setting represents a discrete numeric configuration; otherwise, <c>false</c>.</returns>
		public bool IsDiscreteNumberConfiguration(out DiscreteNumberConfigurationSetting setting)
		{
			setting = this as DiscreteNumberConfigurationSetting;
			return setting != null;
		}

		/// <summary>
		/// Determines whether this configuration setting represents a text configuration and, if so, returns it as a <see cref="TextConfigurationSetting"/>.
		/// </summary>
		/// <param name="setting">When this method returns, contains the current configuration setting as a <see cref="TextConfigurationSetting"/> when it represents a text configuration; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this configuration setting represents a text configuration; otherwise, <c>false</c>.</returns>
		public bool IsTextConfiguration(out TextConfigurationSetting setting)
		{
			setting = this as TextConfigurationSetting;
			return setting != null;
		}

		/// <summary>
		/// Determines whether this configuration setting represents a discrete text configuration and, if so, returns it as a <see cref="DiscreteTextConfigurationSetting"/>.
		/// </summary>
		/// <param name="setting">When this method returns, contains the current configuration setting as a <see cref="DiscreteTextConfigurationSetting"/> when it represents a discrete text configuration; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this configuration setting represents a discrete text configuration; otherwise, <c>false</c>.</returns>
		public bool IsDiscreteTextConfiguration(out DiscreteTextConfigurationSetting setting)
		{
			setting = this as DiscreteTextConfigurationSetting;
			return setting != null;
		}
	}
}

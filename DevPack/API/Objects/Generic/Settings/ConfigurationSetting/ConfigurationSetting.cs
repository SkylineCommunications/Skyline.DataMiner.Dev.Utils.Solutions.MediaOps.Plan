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
	}
}

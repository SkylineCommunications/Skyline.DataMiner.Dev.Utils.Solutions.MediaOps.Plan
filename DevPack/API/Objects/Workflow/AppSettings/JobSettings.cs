namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Defines job-related settings.
	/// </summary>
	public sealed class JobSettings : ApiObject
	{
		private StorageWorkflow.AppSettingsInstance originalInstance;
		private StorageWorkflow.AppSettingsInstance updatedInstance;

		internal JobSettings(StorageWorkflow.AppSettingsInstance instance) : base(instance.ID.Id)
		{
			ParseInstance(instance);
			InitTracking();
		}

		/// <summary>
		/// Gets or sets the prefix to apply to all generated keys.
		/// </summary>
		public string KeyPrefix { get; set; }

		/// <summary>
		/// Gets or sets the minimum number of digits required for the key value.
		/// </summary>
		public int KeyMinimumDigits { get; set; }

		/// <summary>
		/// Gets or sets the initial seed value used for key generation.
		/// </summary>
		public int KeyStartingSeed { get; set; }

		/// <summary>
		/// Gets or sets the value by which the key is incremented for each new entry.
		/// </summary>
		public int KeyIncrement { get; set; }

		/// <summary>
		/// Gets or sets the default pre-roll duration.
		/// </summary>
		/// <remarks>This setting is only applicable to the UI application.</remarks>
		public TimeSpan DefaultPreRoll { get; set; }

		/// <summary>
		/// Gets or sets the default post-roll duration.
		/// </summary>
		/// <remarks>This setting is only applicable to the UI application.</remarks>
		public TimeSpan DefaultPostRoll { get; set; }

		/// <summary>
		/// Gets or sets the desired state in which a job is created.
		/// </summary>
		/// <remarks>This setting is only applicable to the UI application.</remarks>
		public DesiredJobState DesiredJobState { get; set; }

		internal StorageWorkflow.AppSettingsInstance OriginalInstance => originalInstance;

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + Id.GetHashCode();
				hash = (hash * 23) + (KeyPrefix != null ? KeyPrefix.GetHashCode() : 0);
				hash = (hash * 23) + KeyMinimumDigits.GetHashCode();
				hash = (hash * 23) + KeyStartingSeed.GetHashCode();
				hash = (hash * 23) + KeyIncrement.GetHashCode();
				hash = (hash * 23) + DefaultPreRoll.GetHashCode();
				hash = (hash * 23) + DefaultPostRoll.GetHashCode();
				hash = (hash * 23) + DesiredJobState.GetHashCode();

				return hash;
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not JobSettings other)
			{
				return false;
			}

			return Id == other.Id &&
				   KeyPrefix == other.KeyPrefix &&
				   KeyMinimumDigits == other.KeyMinimumDigits &&
				   KeyStartingSeed == other.KeyStartingSeed &&
				   KeyIncrement == other.KeyIncrement &&
				   DefaultPreRoll == other.DefaultPreRoll &&
				   DefaultPostRoll == other.DefaultPostRoll &&
				   DesiredJobState == other.DesiredJobState;
		}

		internal StorageWorkflow.AppSettingsInstance GetInstanceWithChanges()
		{
			if (updatedInstance == null)
			{
				updatedInstance = originalInstance.Clone();
			}

			updatedInstance.JobSettings.JobIDPrefix = KeyPrefix;
			updatedInstance.JobSettings.JobIDMinimumDigits = KeyMinimumDigits;
			updatedInstance.JobSettings.JobIDStartingSeed = KeyStartingSeed;
			updatedInstance.JobSettings.JobIDIncrement = KeyIncrement;

			updatedInstance.JobSettings.DefaultPreroll = DefaultPreRoll;
			updatedInstance.JobSettings.DefaultPostroll = DefaultPostRoll;

			updatedInstance.JobSettings.DesiredJobStatus = EnumExtensions.MapEnum<DesiredJobState, StorageWorkflow.SlcWorkflowIds.Enums.Desiredjobstatus>(DesiredJobState);

			return updatedInstance;
		}

		private void ParseInstance(StorageWorkflow.AppSettingsInstance instance)
		{
			originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

			KeyPrefix = instance.JobSettings.JobIDPrefix;
			KeyMinimumDigits = (int)instance.JobSettings.JobIDMinimumDigits;
			KeyStartingSeed = (int)instance.JobSettings.JobIDStartingSeed;
			KeyIncrement = (int)instance.JobSettings.JobIDIncrement;

			DefaultPreRoll = instance.JobSettings.DefaultPreroll ?? TimeSpan.Zero;
			DefaultPostRoll = instance.JobSettings.DefaultPostroll ?? TimeSpan.Zero;

			DesiredJobState = instance.JobSettings.DesiredJobStatus.HasValue
				? EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Enums.Desiredjobstatus, DesiredJobState>(instance.JobSettings.DesiredJobStatus.Value)
				: DesiredJobState.Draft;
		}
	}
}

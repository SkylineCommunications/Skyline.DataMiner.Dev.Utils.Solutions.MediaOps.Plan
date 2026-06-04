namespace RT_MediaOps.Plan.RegressionTests
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Captures the full set of values from a job settings DOM instance so they can be
	/// restored later by integration tests. The DOM instance (<see cref="AppSettingsInstance"/>)
	/// is used as the source because it exposes the complete state, including fields
	/// such as <c>JobIDNextSequence</c> that are not surfaced by the public API model.
	/// </summary>
	internal sealed class JobSettingsSnapshot
	{
		private JobSettingsSnapshot()
		{
		}

		public string? JobIDPrefix { get; private set; }

		public long? JobIDMinimumDigits { get; private set; }

		public long? JobIDStartingSeed { get; private set; }

		public long? JobIDIncrement { get; private set; }

		public long? JobIDNextSequence { get; private set; }

		public TimeSpan? DefaultPreroll { get; private set; }

		public TimeSpan? DefaultPostroll { get; private set; }

		public SlcWorkflowIds.Enums.Desiredjobstatus? DesiredJobStatus { get; private set; }

		internal static JobSettingsSnapshot FromDom(AppSettingsInstance instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			var section = instance.JobSettings;
			return new JobSettingsSnapshot
			{
				JobIDPrefix = section.JobIDPrefix,
				JobIDMinimumDigits = section.JobIDMinimumDigits,
				JobIDStartingSeed = section.JobIDStartingSeed,
				JobIDIncrement = section.JobIDIncrement,
				JobIDNextSequence = section.JobIDNextSequence,
				DefaultPreroll = section.DefaultPreroll,
				DefaultPostroll = section.DefaultPostroll,
				DesiredJobStatus = section.DesiredJobStatus,
			};
		}

		internal void ApplyTo(AppSettingsInstance instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			var section = instance.JobSettings;
			section.JobIDPrefix = JobIDPrefix;
			section.JobIDMinimumDigits = JobIDMinimumDigits;
			section.JobIDStartingSeed = JobIDStartingSeed;
			section.JobIDIncrement = JobIDIncrement;
			section.JobIDNextSequence = JobIDNextSequence;
			section.DefaultPreroll = DefaultPreroll;
			section.DefaultPostroll = DefaultPostroll;
			section.DesiredJobStatus = DesiredJobStatus;
		}
	}
}

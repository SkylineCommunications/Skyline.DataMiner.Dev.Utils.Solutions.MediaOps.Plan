namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Linq;

	internal class GlobalSettings : IGlobalSettings
	{
		private readonly MediaOpsPlanApi planApi;

		internal GlobalSettings(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		public JobSettings GetJobSettings()
		{
			if (!DomJobSettingHandler.TryGetOrCreate(planApi, out var result))
			{
				result.ThrowSingleException(DomJobSettingHandler.JobSettingId);
			}

			return new JobSettings(result.SuccessfulItems.Single());
		}

		public JobSettings UpdateJobSettings(JobSettings apiJobSetting)
		{
			if (!DomJobSettingHandler.TryUpdate(planApi, apiJobSetting, out var result))
			{
				result.ThrowSingleException(apiJobSetting.Id);
			}

			return new JobSettings(result.SuccessfulItems.Single());
		}
	}
}

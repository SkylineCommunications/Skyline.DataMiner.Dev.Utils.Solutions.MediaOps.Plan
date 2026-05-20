namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using DomJobSettings = Storage.DOM.SlcWorkflow.AppSettingsInstance;

	internal class DomJobSettingHandler : DomInstanceApiObjectValidator<DomJobSettings>
	{
		private readonly MediaOpsPlanApi planApi;

		private DomJobSettingHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		internal static Guid JobSettingId => Guid.Parse("CC1F5D49-D501-41F9-809E-2228CC427B1C");

		internal static bool TryGetOrCreate(MediaOpsPlanApi planApi, out DomInstanceBulkOperationResult<DomJobSettings> result)
		{
			var handler = new DomJobSettingHandler(planApi);
			handler.GetOrCreate();

			result = new DomInstanceBulkOperationResult<DomJobSettings>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryGetNextKeys(MediaOpsPlanApi planApi, int count, out IReadOnlyList<string> keys, out DomInstanceBulkOperationResult<DomJobSettings> result)
		{
			var handler = new DomJobSettingHandler(planApi);
			keys = handler.GetNextKeys(count);

			result = new DomInstanceBulkOperationResult<DomJobSettings>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryUpdate(MediaOpsPlanApi planApi, JobSettings apiJobSetting, out DomInstanceBulkOperationResult<DomJobSettings> result)
		{
			var handler = new DomJobSettingHandler(planApi);
			handler.Update(apiJobSetting);

			result = new DomInstanceBulkOperationResult<DomJobSettings>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		private void GetOrCreate()
		{
			var existing = planApi.DomHelpers.SlcWorkflowHelper.GetAppSettings(DomInstanceExposers.Id.Equal(JobSettingId)).SingleOrDefault();
			if (existing != null)
			{
				ReportSuccess(existing);
				return;
			}

			var lockResult = planApi.LockManager.LockAndExecute<JobSettingsSentinel, DomJobSettings>(
				new[] { new JobSettingsSentinel(JobSettingId) },
				_ =>
				{
					return new[] { CreateLocked() };
				});
			
			if (!lockResult.AllLocksGranted)
			{
				ReportError(JobSettingId, new MediaOpsErrorData { ErrorMessage = $"Failed to acquire lock for {nameof(JobSettings)} {JobSettingId}." });
				return;
			}

			ReportSuccess(lockResult.ActionResults.Single());
		}

		private IReadOnlyList<string> GetNextKeys(int count)
		{
			if (count <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
			}

			IReadOnlyList<string> generatedKeys = null;

			var lockResult = planApi.LockManager.LockAndExecute<JobSettingsSentinel, DomJobSettings>(
				new[] { new JobSettingsSentinel(JobSettingId) },
				_ =>
				{
					var settings = GetOrCreateInsideLock();
					if (settings == null)
					{
						return Array.Empty<DomJobSettings>();
					}

					generatedKeys = GenerateKeys(settings, count);

					var nextSequence = settings.JobSettings.JobIDNextSequence + ((long)count * settings.JobSettings.JobIDIncrement);
					settings.JobSettings.JobIDNextSequence = nextSequence;

					planApi.DomHelpers.SlcWorkflowHelper.DomHelper.DomInstances.Update(settings.ToInstance());

					return new[] { settings };
				});

			if (!lockResult.AllLocksGranted)
			{
				ReportError(JobSettingId, new MediaOpsErrorData { ErrorMessage = $"Failed to acquire lock for {nameof(JobSettings)} {JobSettingId}." });
				return Array.Empty<string>();
			}

			return generatedKeys ?? Array.Empty<string>();
		}

		private DomJobSettings GetOrCreateInsideLock()
		{
			var existing = planApi.DomHelpers.SlcWorkflowHelper.GetAppSettings(DomInstanceExposers.Id.Equal(JobSettingId)).SingleOrDefault();
			if (existing != null)
			{
				return existing;
			}

			return CreateLocked();
		}

		private static IReadOnlyList<string> GenerateKeys(DomJobSettings settings, int count)
		{
			var keys = new List<string>(count);
			var prefix = settings.JobSettings.JobIDPrefix ?? string.Empty;
			var minDigits = (int)(settings.JobSettings.JobIDMinimumDigits ?? 1);
			var increment = settings.JobSettings.JobIDIncrement ?? 1;
			var next = settings.JobSettings.JobIDNextSequence ?? settings.JobSettings.JobIDStartingSeed ?? 1;

			for (int i = 0; i < count; i++)
			{
				keys.Add($"{prefix}{next.ToString().PadLeft(minDigits, '0')}");
				next += increment;
			}

			return keys;
		}

		private DomJobSettings CreateLocked()
		{
			var toRemove = planApi.DomHelpers.SlcWorkflowHelper.GetAppSettings(DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.AppSettings.Id)).ToList();

			DomJobSettings result;
			if (toRemove.Count == 0)
			{
				result = CreateDefaultInstance();
			}
			else
			{
				result = MigrateInstance(toRemove.First());

				planApi.DomHelpers.SlcWorkflowHelper.DomHelper.DomInstances.DeleteInBatches(toRemove.Select(i => i.ToInstance()));
			}

			return result;
		}

		private DomJobSettings CreateDefaultInstance()
		{
			var newInstance = new DomJobSettings(JobSettingId);
			newInstance.JobSettings.JobIDPrefix = "JOB #";
			newInstance.JobSettings.JobIDMinimumDigits = 5;
			newInstance.JobSettings.JobIDStartingSeed = 1;
			newInstance.JobSettings.JobIDIncrement = 1;
			newInstance.JobSettings.JobIDNextSequence = 1;

			newInstance.JobSettings.DefaultPreroll = TimeSpan.Zero;
			newInstance.JobSettings.DefaultPostroll = TimeSpan.Zero;

			newInstance.JobSettings.DesiredJobStatus = SlcWorkflowIds.Enums.Desiredjobstatus.Draft;

			var createdInstance = planApi.DomHelpers.SlcWorkflowHelper.DomHelper.DomInstances.Create(newInstance.ToInstance());
			return new DomJobSettings(createdInstance);
		}

		private DomJobSettings MigrateInstance(DomJobSettings source)
		{
			var mergedInstance = new DomJobSettings(JobSettingId);
			mergedInstance.JobSettings = source.JobSettings;

			var createdInstance = planApi.DomHelpers.SlcWorkflowHelper.DomHelper.DomInstances.Create(mergedInstance.ToInstance());
			return new DomJobSettings(createdInstance);
		}

		private void Update(JobSettings apiJobSetting)
		{
			if (apiJobSetting == null)
			{
				throw new ArgumentNullException(nameof(apiJobSetting));
			}

			ValidateKeySettings(apiJobSetting);
			if (!IsValid(apiJobSetting))
			{
				return;
			}

			var lockResult = planApi.LockManager.LockAndExecute<JobSettings, DomJobSettings>(
				new[] { apiJobSetting },
				_ =>
				{
					UpdateLocked(apiJobSetting);
					return Array.Empty<DomJobSettings>();
				});

			if (!lockResult.AllLocksGranted)
			{
				ReportError(apiJobSetting.Id, new MediaOpsErrorData { ErrorMessage = $"Failed to acquire lock for {nameof(JobSettings)} {apiJobSetting.Id}." });
			}
		}

		private void UpdateLocked(JobSettings apiJobSetting)
		{
			if (apiJobSetting == null)
			{
				throw new ArgumentNullException(nameof(apiJobSetting));
			}

			var change = GetChanges(apiJobSetting);
			if (change == null)
			{
				// No changes: treat as a successful no-op and report the existing instance.
				ReportSuccess(apiJobSetting.OriginalInstance);
				return;
			}

			var domJobSettings = new DomJobSettings(change.Instance);
			var changesImpactingNextSequence = new List<Guid>
			{
				SlcWorkflowIds.Sections.JobSettings.JobIDStartingSeed.Id,
				SlcWorkflowIds.Sections.JobSettings.JobIDIncrement.Id,
			};
			if (change.ChangedFields.Any(x => changesImpactingNextSequence.Contains(x.FieldDescriptorId)))
			{
				domJobSettings.JobSettings.JobIDNextSequence = RecalculateNextSequence(domJobSettings);
			}

			planApi.DomHelpers.SlcWorkflowHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches([domJobSettings], out var domResult);

			foreach (var id in domResult.UnsuccessfulIds)
			{
				ReportError(id.Id);

				if (domResult.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					var mediaOpsTraceData = new MediaOpsTraceData();
					mediaOpsTraceData.Add(new MediaOpsErrorData() { ErrorMessage = traceData.ToString() });

					PassTraceData(id.Id, mediaOpsTraceData);
				}
			}

			ReportSuccess(domResult.SuccessfulItems.Select(x => new DomJobSettings(x)));
		}

		private void ValidateKeySettings(JobSettings apiJobSetting)
		{
			if (apiJobSetting == null)
			{
				throw new ArgumentNullException(nameof(apiJobSetting));
			}

			if (!InputValidator.IsNonEmptyText(apiJobSetting.KeyPrefix))
			{
				var error = new JobSettingsInvalidKeyPrefixError
				{
					ErrorMessage = "Key prefix cannot be empty.",
					Id = apiJobSetting.Id,
				};

				ReportError(apiJobSetting.Id, error);
			}
			else if (!InputValidator.HasValidTextLength(apiJobSetting.KeyPrefix))
			{
				var error = new JobSettingsInvalidKeyPrefixError
				{
					ErrorMessage = $"Key prefix exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
					Id = apiJobSetting.Id,
				};

				ReportError(apiJobSetting.Id, error);
			}

			if (apiJobSetting.KeyMinimumDigits < 1
				|| apiJobSetting.KeyMinimumDigits > 20)
			{
				var error = new JobSettingsInvalidKeyMinimumDigitsError
				{
					ErrorMessage = "Key minimum digits must be between 1 and 20.",
					Id = apiJobSetting.Id,
					KeyMinimumDigits = apiJobSetting.KeyMinimumDigits,
				};

				ReportError(apiJobSetting.Id, error);
			}

			if (apiJobSetting.KeyStartingSeed < 1)
			{
				var error = new JobSettingsInvalidKeyStartingSeedError
				{
					ErrorMessage = "Key starting seed must be greater than 0.",
					Id = apiJobSetting.Id,
					KeyStartingSeed = apiJobSetting.KeyStartingSeed,
				};

				ReportError(apiJobSetting.Id, error);
			}

			if (apiJobSetting.KeyIncrement < 1)
			{
				var error = new JobSettingsInvalidKeyIncrementError
				{
					ErrorMessage = "Key increment must be greater than 0.",
					Id = apiJobSetting.Id,
					KeyIncrement = apiJobSetting.KeyIncrement,
				};

				ReportError(apiJobSetting.Id, error);
			}
		}

		private DomChangeResults GetChanges(JobSettings apiJobSetting)
		{
			var changes = GetItemsWithChanges<JobSettings, DomJobSettings>(
				[apiJobSetting],
				x => x.OriginalInstance,
				x => x.GetInstanceWithChanges(),
				ids => planApi.DomHelpers.SlcWorkflowHelper.GetAppSettings(ids),
				x => new JobSettingsNotFoundError { ErrorMessage = $"Job setting with ID '{x.Id}' no longer exists.", Id = x.Id },
				(x, msg) => new JobSettingsValueAlreadyChangedError { ErrorMessage = msg, Id = x.Id })
				.ToList();

			return changes.FirstOrDefault(x => x.Id == apiJobSetting.Id);
		}

		private long RecalculateNextSequence(DomJobSettings settings)
		{
			var nextSequence = settings.JobSettings.JobIDNextSequence;
			if (nextSequence <= settings.JobSettings.JobIDStartingSeed)
			{
				return settings.JobSettings.JobIDStartingSeed.Value;
			}

			var calculated = (nextSequence / settings.JobSettings.JobIDIncrement) * settings.JobSettings.JobIDIncrement;
			while (calculated < nextSequence)
			{
				calculated += settings.JobSettings.JobIDIncrement;
			}

			return calculated.Value;
		}

		private sealed class JobSettingsSentinel : ApiObject
		{
			internal JobSettingsSentinel(Guid id) : base(id)
			{
			}
		}
	}
}

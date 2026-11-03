namespace RT_MediaOps.Plan.Workflow.JobSettings
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Systems upgraded from MediaOps 1.6.X hold the job settings in an App Settings instance with a random ID.
	/// These tests verify that the DevPack merges such legacy instances into the instance with the fixed ID.
	/// </summary>
	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class LegacyJobSettingsMigrationTests : IDisposable
	{
		private static readonly Guid FixedJobSettingsId = Guid.Parse("CC1F5D49-D501-41F9-809E-2228CC427B1C");

		private readonly TestObjectCreator objectCreator;
		private readonly List<DomInstance> originalAppSettings;

		public LegacyJobSettingsMigrationTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
			originalAppSettings = ReadAllAppSettings().Select(x => (DomInstance)x.Clone()).ToList();
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		private static DomHelper DomHelper => ((MediaOpsPlanApi)TestContext.Api).DomHelpers.SlcWorkflowHelper.DomHelper;

		public void Dispose()
		{
			objectCreator.Dispose();
			RestoreAppSettings();
		}

		[TestMethod]
		public void GetJobSettings_OnlyLegacyInstance_MigratesLegacyValues()
		{
			DeleteAllAppSettings();
			var legacyId = CreateLegacyInstance("LEG-", 6, 100, 5, 245);

			var settings = TestContext.Api.GlobalSettings.GetJobSettings();

			Assert.AreEqual(FixedJobSettingsId, settings.Id);
			Assert.AreEqual("LEG-", settings.KeyPrefix);
			Assert.AreEqual(6, settings.KeyMinimumDigits);
			Assert.AreEqual(100, settings.KeyStartingSeed);
			Assert.AreEqual(5, settings.KeyIncrement);

			var remaining = ReadAllAppSettings();
			Assert.AreEqual(1, remaining.Count, "Only the fixed-ID instance should remain.");
			Assert.AreEqual(FixedJobSettingsId, remaining[0].ID.Id);
			Assert.IsFalse(remaining.Any(x => x.ID.Id == legacyId), "The legacy instance should be deleted.");
			Assert.AreEqual(245, ReadFixedInstance().JobSettings.JobIDNextSequence);
		}

		[TestMethod]
		public void CreateJob_WithoutKey_OnlyLegacyInstance_ContinuesLegacySequence()
		{
			DeleteAllAppSettings();
			CreateLegacyInstance("LEG-", 6, 100, 5, 245);

			var job = objectCreator.CreateJob(NewJob());

			Assert.AreEqual("LEG-000245", job.Key);
			Assert.AreEqual(250, ReadFixedInstance().JobSettings.JobIDNextSequence);
			Assert.AreEqual(1, ReadAllAppSettings().Count, "Only the fixed-ID instance should remain.");
		}

		[TestMethod]
		public void GetJobSettings_FixedWithDefaultsAndLegacy_LegacyConfigurationAndHighestSequenceWin()
		{
			DeleteAllAppSettings();
			CreateFixedInstance("JOB #", 5, 1, 1, 3);
			CreateLegacyInstance("LEG-", 6, 1, 1, 120);

			var settings = TestContext.Api.GlobalSettings.GetJobSettings();

			Assert.AreEqual("LEG-", settings.KeyPrefix);
			Assert.AreEqual(6, settings.KeyMinimumDigits);
			Assert.AreEqual(120, ReadFixedInstance().JobSettings.JobIDNextSequence);
			Assert.AreEqual(1, ReadAllAppSettings().Count, "Only the fixed-ID instance should remain.");
		}

		[TestMethod]
		public void GetJobSettings_FixedSequenceHigherThanLegacy_KeepsFixedSequence()
		{
			DeleteAllAppSettings();
			CreateFixedInstance("JOB #", 5, 1, 1, 500);
			CreateLegacyInstance("LEG-", 6, 1, 1, 120);

			var settings = TestContext.Api.GlobalSettings.GetJobSettings();

			Assert.AreEqual("LEG-", settings.KeyPrefix);
			Assert.AreEqual(500, ReadFixedInstance().JobSettings.JobIDNextSequence);
		}

		[TestMethod]
		public void GetJobSettings_RepairedSequence_IsRoundedUpToTheLegacyIncrement()
		{
			DeleteAllAppSettings();
			CreateFixedInstance("JOB #", 5, 1, 1, 125);
			CreateLegacyInstance("LEG-", 6, 10, 10, 120);

			TestContext.Api.GlobalSettings.GetJobSettings();

			Assert.AreEqual(130, ReadFixedInstance().JobSettings.JobIDNextSequence);
		}

		[TestMethod]
		public void CreateJob_WithoutKey_FixedWithDefaultsAndLegacy_ContinuesLegacySequence()
		{
			DeleteAllAppSettings();
			CreateFixedInstance("JOB #", 5, 1, 1, 3);
			CreateLegacyInstance("LEG-", 6, 1, 1, 120);

			var job = objectCreator.CreateJob(NewJob());

			Assert.AreEqual("LEG-000120", job.Key);
			Assert.AreEqual(121, ReadFixedInstance().JobSettings.JobIDNextSequence);
			Assert.AreEqual(1, ReadAllAppSettings().Count, "Only the fixed-ID instance should remain.");
		}

		[TestMethod]
		public void GetJobSettings_MultipleLegacyInstances_HighestSequenceWinsAndAllAreDeleted()
		{
			DeleteAllAppSettings();
			CreateLegacyInstance("LOW-", 4, 1, 1, 10);
			CreateLegacyInstance("HIGH-", 7, 1, 1, 90);
			CreateLegacyInstance("MID-", 5, 1, 1, 40);

			var settings = TestContext.Api.GlobalSettings.GetJobSettings();

			Assert.AreEqual("HIGH-", settings.KeyPrefix);
			Assert.AreEqual(7, settings.KeyMinimumDigits);
			Assert.AreEqual(90, ReadFixedInstance().JobSettings.JobIDNextSequence);

			var remaining = ReadAllAppSettings();
			Assert.AreEqual(1, remaining.Count, "All legacy instances should be deleted.");
			Assert.AreEqual(FixedJobSettingsId, remaining[0].ID.Id);
		}

		[TestMethod]
		public void GetJobSettings_LegacyInstanceWithoutRollAndStateFields_FillsDefaults()
		{
			DeleteAllAppSettings();
			CreateLegacyInstance("LEG-", 6, 1, 1, 12);

			var settings = TestContext.Api.GlobalSettings.GetJobSettings();

			Assert.AreEqual(TimeSpan.Zero, settings.DefaultPreRoll);
			Assert.AreEqual(TimeSpan.Zero, settings.DefaultPostRoll);
			Assert.AreEqual(DesiredJobState.Draft, settings.DesiredJobState);
		}

		[TestMethod]
		public void GetJobSettings_NoAppSettings_CreatesFixedInstanceWithDefaults()
		{
			DeleteAllAppSettings();

			var settings = TestContext.Api.GlobalSettings.GetJobSettings();

			Assert.AreEqual(FixedJobSettingsId, settings.Id);
			Assert.AreEqual("JOB #", settings.KeyPrefix);
			Assert.AreEqual(5, settings.KeyMinimumDigits);
			Assert.AreEqual(1, settings.KeyStartingSeed);
			Assert.AreEqual(1, settings.KeyIncrement);
			Assert.AreEqual(1, ReadFixedInstance().JobSettings.JobIDNextSequence);
			Assert.AreEqual(1, ReadAllAppSettings().Count);
		}

		[TestMethod]
		public void GetJobSettings_OnlyFixedInstance_LeavesItUnchanged()
		{
			DeleteAllAppSettings();
			CreateFixedInstance("FIX-", 4, 1, 1, 77);

			var settings = TestContext.Api.GlobalSettings.GetJobSettings();

			Assert.AreEqual("FIX-", settings.KeyPrefix);
			Assert.AreEqual(77, ReadFixedInstance().JobSettings.JobIDNextSequence);
			Assert.AreEqual(1, ReadAllAppSettings().Count);
		}

		private static Job NewJob()
		{
			var start = DateTime.Now.RoundToNextSecond().AddMinutes(10);
			return new Job
			{
				Name = $"{Guid.NewGuid()}_Job",
				Start = start,
				End = start.AddMinutes(5),
				PreRollStart = start,
				PostRollEnd = start.AddMinutes(5),
			};
		}

		private static Guid CreateLegacyInstance(string prefix, long minimumDigits, long startingSeed, long increment, long nextSequence)
		{
			// Mirrors the instance created by the 1.6.X solution setup: random ID and only the job ID fields.
			var instance = new AppSettingsInstance(Guid.NewGuid());
			instance.JobSettings.JobIDPrefix = prefix;
			instance.JobSettings.JobIDMinimumDigits = minimumDigits;
			instance.JobSettings.JobIDStartingSeed = startingSeed;
			instance.JobSettings.JobIDIncrement = increment;
			instance.JobSettings.JobIDNextSequence = nextSequence;

			return DomHelper.DomInstances.Create(instance.ToInstance()).ID.Id;
		}

		private static void CreateFixedInstance(string prefix, long minimumDigits, long startingSeed, long increment, long nextSequence)
		{
			var instance = new AppSettingsInstance(FixedJobSettingsId);
			instance.JobSettings.JobIDPrefix = prefix;
			instance.JobSettings.JobIDMinimumDigits = minimumDigits;
			instance.JobSettings.JobIDStartingSeed = startingSeed;
			instance.JobSettings.JobIDIncrement = increment;
			instance.JobSettings.JobIDNextSequence = nextSequence;
			instance.JobSettings.DefaultPreroll = TimeSpan.Zero;
			instance.JobSettings.DefaultPostroll = TimeSpan.Zero;
			instance.JobSettings.DesiredJobStatus = SlcWorkflowIds.Enums.Desiredjobstatus.Draft;

			DomHelper.DomInstances.Create(instance.ToInstance());
		}

		private static AppSettingsInstance ReadFixedInstance()
		{
			return new AppSettingsInstance(ReadAllAppSettings().Single(x => x.ID.Id == FixedJobSettingsId));
		}

		private static List<DomInstance> ReadAllAppSettings()
		{
			return DomHelper.DomInstances.Read(DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.AppSettings.Id));
		}

		private static void DeleteAllAppSettings()
		{
			foreach (var instance in ReadAllAppSettings())
			{
				DomHelper.DomInstances.Delete(instance);
			}
		}

		private void RestoreAppSettings()
		{
			DeleteAllAppSettings();

			foreach (var instance in originalAppSettings)
			{
				DomHelper.DomInstances.Create(instance);
			}
		}
	}
}

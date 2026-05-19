namespace RT_MediaOps.Plan.Workflow.JobSettings
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class BasicTests
	{
		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		[TestMethod]
		public void ReadJobSettings()
		{
			var settings = TestContext.Api.GlobalSettings.GetJobSettings();

			Assert.IsNotNull(settings);
			Assert.IsFalse(string.IsNullOrEmpty(settings.KeyPrefix));
			Assert.IsTrue(settings.KeyMinimumDigits >= 1);
			Assert.IsTrue(settings.KeyStartingSeed >= 1);
			Assert.IsTrue(settings.KeyIncrement >= 1);
		}

		[TestMethod]
		public void ReadJobSettingsIsIdempotent()
		{
			var first = TestContext.Api.GlobalSettings.GetJobSettings();
			var second = TestContext.Api.GlobalSettings.GetJobSettings();

			Assert.AreEqual(first.Id, second.Id);
			Assert.AreEqual(first.KeyPrefix, second.KeyPrefix);
			Assert.AreEqual(first.KeyMinimumDigits, second.KeyMinimumDigits);
			Assert.AreEqual(first.KeyStartingSeed, second.KeyStartingSeed);
			Assert.AreEqual(first.KeyIncrement, second.KeyIncrement);
		}

		[TestMethod]
		public void UpdateJobSettings_AllFields()
		{
			var prefix = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
			var original = TestContext.Api.GlobalSettings.GetJobSettings();
			var snapshot = Snapshot(original);

			try
			{
				original.KeyPrefix = $"RT_{prefix}_";
				original.KeyMinimumDigits = 6;
				original.KeyStartingSeed = 10;
				original.KeyIncrement = 2;
				original.DefaultPreRoll = TimeSpan.FromSeconds(5);
				original.DefaultPostRoll = TimeSpan.FromSeconds(10);
				original.DesiredJobState = DesiredJobState.Tentative;

				var updated = TestContext.Api.GlobalSettings.UpdateJobSettings(original);

				Assert.AreEqual($"RT_{prefix}_", updated.KeyPrefix);
				Assert.AreEqual(6, updated.KeyMinimumDigits);
				Assert.AreEqual(10, updated.KeyStartingSeed);
				Assert.AreEqual(2, updated.KeyIncrement);
				Assert.AreEqual(TimeSpan.FromSeconds(5), updated.DefaultPreRoll);
				Assert.AreEqual(TimeSpan.FromSeconds(10), updated.DefaultPostRoll);
				Assert.AreEqual(DesiredJobState.Tentative, updated.DesiredJobState);

				var reread = TestContext.Api.GlobalSettings.GetJobSettings();
				Assert.AreEqual(updated.KeyPrefix, reread.KeyPrefix);
				Assert.AreEqual(updated.KeyMinimumDigits, reread.KeyMinimumDigits);
				Assert.AreEqual(updated.KeyStartingSeed, reread.KeyStartingSeed);
				Assert.AreEqual(updated.KeyIncrement, reread.KeyIncrement);
				Assert.AreEqual(updated.DefaultPreRoll, reread.DefaultPreRoll);
				Assert.AreEqual(updated.DefaultPostRoll, reread.DefaultPostRoll);
				Assert.AreEqual(updated.DesiredJobState, reread.DesiredJobState);
			}
			finally
			{
				Restore(snapshot);
			}
		}

		[TestMethod]
		public void UpdateJobSettings_NoChanges_IsNoOp()
		{
			var settings = TestContext.Api.GlobalSettings.GetJobSettings();

			var updated = TestContext.Api.GlobalSettings.UpdateJobSettings(settings);

			Assert.IsNotNull(updated);
			Assert.AreEqual(settings.Id, updated.Id);
			Assert.AreEqual(settings.KeyPrefix, updated.KeyPrefix);
			Assert.AreEqual(settings.KeyMinimumDigits, updated.KeyMinimumDigits);
			Assert.AreEqual(settings.KeyStartingSeed, updated.KeyStartingSeed);
			Assert.AreEqual(settings.KeyIncrement, updated.KeyIncrement);
			Assert.AreEqual(settings.DefaultPreRoll, updated.DefaultPreRoll);
			Assert.AreEqual(settings.DefaultPostRoll, updated.DefaultPostRoll);
			Assert.AreEqual(settings.DesiredJobState, updated.DesiredJobState);
		}

		[TestMethod]
		public void UpdateJobSettings_NullThrows()
		{
			Assert.ThrowsException<ArgumentNullException>(() => TestContext.Api.GlobalSettings.UpdateJobSettings(null));
		}

		[TestMethod]
		public void UpdateJobSettings_EmptyKeyPrefix_Throws()
		{
			var settings = TestContext.Api.GlobalSettings.GetJobSettings();
			settings.KeyPrefix = string.Empty;

			try
			{
				TestContext.Api.GlobalSettings.UpdateJobSettings(settings);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobSettingsInvalidKeyPrefixError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual("Key prefix cannot be empty.", error.ErrorMessage);
				Assert.AreEqual(settings.Id, error.Id);
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void UpdateJobSettings_NullKeyPrefix_Throws()
		{
			var settings = TestContext.Api.GlobalSettings.GetJobSettings();
			settings.KeyPrefix = null;

			try
			{
				TestContext.Api.GlobalSettings.UpdateJobSettings(settings);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobSettingsInvalidKeyPrefixError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual("Key prefix cannot be empty.", error.ErrorMessage);
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void UpdateJobSettings_KeyMinimumDigitsBelowRange_Throws()
		{
			var settings = TestContext.Api.GlobalSettings.GetJobSettings();
			settings.KeyMinimumDigits = 0;

			try
			{
				TestContext.Api.GlobalSettings.UpdateJobSettings(settings);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobSettingsInvalidKeyMinimumDigitsError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual("Key minimum digits must be between 1 and 20.", error.ErrorMessage);
				Assert.AreEqual(0, error.KeyMinimumDigits);
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void UpdateJobSettings_KeyMinimumDigitsAboveRange_Throws()
		{
			var settings = TestContext.Api.GlobalSettings.GetJobSettings();
			settings.KeyMinimumDigits = 21;

			try
			{
				TestContext.Api.GlobalSettings.UpdateJobSettings(settings);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobSettingsInvalidKeyMinimumDigitsError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual("Key minimum digits must be between 1 and 20.", error.ErrorMessage);
				Assert.AreEqual(21, error.KeyMinimumDigits);
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void UpdateJobSettings_KeyStartingSeedBelowOne_Throws()
		{
			var settings = TestContext.Api.GlobalSettings.GetJobSettings();
			settings.KeyStartingSeed = 0;

			try
			{
				TestContext.Api.GlobalSettings.UpdateJobSettings(settings);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobSettingsInvalidKeyStartingSeedError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual("Key starting seed must be greater than 0.", error.ErrorMessage);
				Assert.AreEqual(0, error.KeyStartingSeed);
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void UpdateJobSettings_KeyIncrementBelowOne_Throws()
		{
			var settings = TestContext.Api.GlobalSettings.GetJobSettings();
			settings.KeyIncrement = 0;

			try
			{
				TestContext.Api.GlobalSettings.UpdateJobSettings(settings);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobSettingsInvalidKeyIncrementError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual("Key increment must be greater than 0.", error.ErrorMessage);
				Assert.AreEqual(0, error.KeyIncrement);
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		private static JobSettingsSnapshot Snapshot(JobSettings settings)
		{
			return new JobSettingsSnapshot
			{
				KeyPrefix = settings.KeyPrefix,
				KeyMinimumDigits = settings.KeyMinimumDigits,
				KeyStartingSeed = settings.KeyStartingSeed,
				KeyIncrement = settings.KeyIncrement,
				DefaultPreRoll = settings.DefaultPreRoll,
				DefaultPostRoll = settings.DefaultPostRoll,
				DesiredJobState = settings.DesiredJobState,
			};
		}

		private static void Restore(JobSettingsSnapshot snapshot)
		{
			var current = TestContext.Api.GlobalSettings.GetJobSettings();
			current.KeyPrefix = snapshot.KeyPrefix;
			current.KeyMinimumDigits = snapshot.KeyMinimumDigits;
			current.KeyStartingSeed = snapshot.KeyStartingSeed;
			current.KeyIncrement = snapshot.KeyIncrement;
			current.DefaultPreRoll = snapshot.DefaultPreRoll;
			current.DefaultPostRoll = snapshot.DefaultPostRoll;
			current.DesiredJobState = snapshot.DesiredJobState;

			TestContext.Api.GlobalSettings.UpdateJobSettings(current);
		}

		private sealed class JobSettingsSnapshot
		{
			public string KeyPrefix { get; set; }

			public int KeyMinimumDigits { get; set; }

			public int KeyStartingSeed { get; set; }

			public int KeyIncrement { get; set; }

			public TimeSpan DefaultPreRoll { get; set; }

			public TimeSpan DefaultPostRoll { get; set; }

			public DesiredJobState DesiredJobState { get; set; }
		}
	}
}

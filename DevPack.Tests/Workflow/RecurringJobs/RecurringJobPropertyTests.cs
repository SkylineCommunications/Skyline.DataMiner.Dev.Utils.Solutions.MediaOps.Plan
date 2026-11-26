namespace RT_MediaOps.Plan.Workflow.RecurringJobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class RecurringJobPropertyTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public RecurringJobPropertyTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void AddSetRemoveProperties_ConfigureUnsavedRecurringJob()
		{
			var property = new StringProperty(Guid.NewGuid());
			var otherProperty = new StringProperty(Guid.NewGuid());
			var recurringJob = NewRecurringJob("Test");

			recurringJob.AddProperty(new StringPropertySetting(property) { Value = "A" });
			recurringJob.AddCustomProperty(new CustomPropertySetting("Tag") { Value = "A" });
			Assert.AreEqual("A", ((StringPropertySetting)recurringJob.PropertySettings.Single()).Value);
			Assert.AreEqual("A", recurringJob.CustomPropertySettings.Single().Value);

			recurringJob.SetProperties([
				new StringPropertySetting(property) { Value = "B" },
				new StringPropertySetting(otherProperty) { Value = "C" },
			]);
			recurringJob.SetCustomProperties([
				new CustomPropertySetting("Tag") { Value = "B" },
				new CustomPropertySetting("Other") { Value = "C" },
			]);
			recurringJob.RemoveProperty(recurringJob.PropertySettings.Single(x => x.Id == property.Id));
			recurringJob.RemoveCustomProperty(recurringJob.CustomPropertySettings.Single(x => x.Name == "Tag"));

			Assert.AreEqual(otherProperty.Id, recurringJob.PropertySettings.Single().Id);
			Assert.AreEqual("Other", recurringJob.CustomPropertySettings.Single().Name);
		}

		[TestMethod]
		public void CreateRecurringJob_WithPropertiesOnRecurringJob_PersistsPropertiesWithOwnerMetadata()
		{
			var prefix = Guid.NewGuid();
			var property = objectCreator.CreateSchedulingProperty(new StringProperty { Name = $"{prefix}_String", SectionName = "General" });

			var recurringJob = NewRecurringJob($"{prefix}_RecurringJob");
			recurringJob.AddProperty(new StringPropertySetting(property) { Value = "Value1" });
			recurringJob.AddCustomProperty(new CustomPropertySetting("Tag1") { Value = "Value1" });
			recurringJob = objectCreator.CreateRecurringJob(recurringJob);

			var returned = TestContext.Api.RecurringJobs.Read(recurringJob.Id);
			Assert.AreEqual("Value1", returned.PropertySettings.OfType<StringPropertySetting>().Single(x => x.Id == property.Id).Value);
			Assert.AreEqual("Value1", returned.CustomPropertySettings.Single(x => x.Name == "Tag1").Value);

			var collection = TestContext.Api.PropertySettingCollections.Read(new ANDFilterElement<PropertySettingCollection>(
				PropertySettingCollectionExposers.LinkedObjectId.Equal(recurringJob.Id.ToString()),
				PropertySettingCollectionExposers.Scope.Equal(PropertySettingsContext.MediaOpsScope))).Single();
			Assert.IsTrue(String.IsNullOrEmpty(collection.SubId), "The recurring job collection must use an empty SubId.");
		}

		[TestMethod]
		public void CreateRecurringJob_WithInvalidPropertyValueOnRecurringJob_ReportsErrorAndDoesNotCreate()
		{
			var prefix = Guid.NewGuid();
			var discreteProperty = new DiscreteProperty { Name = $"{prefix}_Discrete", SectionName = "General" };
			discreteProperty.AddDiscrete("A");
			discreteProperty = objectCreator.CreateSchedulingProperty(discreteProperty);

			var recurringJob = NewRecurringJob($"{prefix}_RecurringJob");
			recurringJob.AddProperty(new DiscretePropertySetting(discreteProperty) { Value = "Unknown" });

			var exception = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreateRecurringJob(recurringJob));

			Assert.IsTrue(exception.TraceData.ErrorData.OfType<PropertySettingCollectionInvalidPropertySettingsError>().Any());
			Assert.IsNull(TestContext.Api.RecurringJobs.Read(recurringJob.Id));
		}

		[TestMethod]
		public void FromRecurringJob_WithPersistedPropertiesOnRecurringJob_CopiesThemToGeneratedJob()
		{
			var prefix = Guid.NewGuid();
			var property = objectCreator.CreateSchedulingProperty(new StringProperty { Name = $"{prefix}_String", SectionName = "General" });

			var recurringJob = NewRecurringJob($"{prefix}_RecurringJob");
			recurringJob.AddProperty(new StringPropertySetting(property) { Value = "Value1" });
			recurringJob.AddCustomProperty(new CustomPropertySetting("Tag1") { Value = "Value1" });
			recurringJob = objectCreator.CreateRecurringJob(recurringJob);

			var storedRecurringJob = TestContext.Api.RecurringJobs.Read(recurringJob.Id);
			var job = objectCreator.CreateJob(Job.FromRecurringJob(storedRecurringJob, DateTime.UtcNow.AddHours(2).RoundToNextSecond()));

			var returned = TestContext.Api.Jobs.Read(job.Id);
			Assert.AreEqual("Value1", returned.PropertySettings.OfType<StringPropertySetting>().Single(x => x.Id == property.Id).Value);
			Assert.AreEqual("Value1", returned.CustomPropertySettings.Single(x => x.Name == "Tag1").Value);
		}

		private static RecurringJob NewRecurringJob(string name)
		{
			var recurringJob = new RecurringJob
			{
				Name = name,
				Start = DateTime.UtcNow.AddHours(1),
				Duration = TimeSpan.FromHours(1),
			};
			recurringJob.Pattern.RepeatType = RepeatType.Daily;
			recurringJob.Pattern.RepeatEvery = 1;
			recurringJob.Pattern.EndDate = DateTime.UtcNow.AddDays(10);
			return recurringJob;
		}
	}
}

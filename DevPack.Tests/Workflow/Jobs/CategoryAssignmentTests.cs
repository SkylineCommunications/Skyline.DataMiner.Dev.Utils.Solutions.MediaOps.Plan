namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.Categories.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class CategoryAssignmentTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public CategoryAssignmentTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void CreateJobAssignsCategory()
		{
			var category = CreateCategory();
			var job = CreateJob(category.ID.ToString());

			AssertCategoryAssignment(job.Id, category);
		}

		[TestMethod]
		public void UpdateJobAssignsCategory()
		{
			var category = CreateCategory();
			var job = CreateJob(null);

			AssertCategoryAssignment(job.Id, null);

			job.JobTypeCategoryId = category.ID.ToString();
			TestContext.Api.Jobs.Update(job);

			AssertCategoryAssignment(job.Id, category);
		}

		[TestMethod]
		public void UpdateJobChangesCategory()
		{
			var category1 = CreateCategory();
			var category2 = CreateCategory();
			var job = CreateJob(category1.ID.ToString());

			AssertCategoryAssignment(job.Id, category1);

			job.JobTypeCategoryId = category2.ID.ToString();
			TestContext.Api.Jobs.Update(job);

			AssertCategoryAssignment(job.Id, category2);
		}

		[TestMethod]
		public void UpdateJobRemovesCategory()
		{
			var category = CreateCategory();
			var job = CreateJob(category.ID.ToString());

			AssertCategoryAssignment(job.Id, category);

			job.JobTypeCategoryId = null;
			TestContext.Api.Jobs.Update(job);

			AssertCategoryAssignment(job.Id, null);
		}

		[TestMethod]
		public void DeleteJobRemovesCategoryItem()
		{
			var category = CreateCategory();
			var job = CreateJob(category.ID.ToString());

			AssertCategoryAssignment(job.Id, category);

			TestContext.Api.Jobs.Delete(job);

			var categoryItems = TestContext.CategoriesApi.CategoryItems.Read(CategoryItemExposers.InstanceId.Equal(job.Id.ToString())).ToArray();
			Assert.AreEqual(0, categoryItems.Length, "The category item was not removed when the job was deleted.");
		}

		[TestMethod]
		public void CreateJobWithNonExistingCategoryThrowsException()
		{
			var categoryId = Guid.NewGuid().ToString();

			try
			{
				CreateJob(categoryId);
			}
			catch (MediaOpsException exception)
			{
				var traceData = exception.TraceData.ErrorData.OfType<JobCategoryNotFoundError>().Single();
				Assert.IsNotNull(traceData);
				Assert.AreEqual(categoryId, traceData.CategoryId);
				Assert.AreEqual($"Category with ID '{categoryId}' not found in Scope '{JobCategoryScopes.JobTypes}'.", traceData.ErrorMessage);

				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		[TestMethod]
		public void UpdateJobWithNonExistingCategoryThrowsException()
		{
			var job = CreateJob(null);

			try
			{
				job.JobTypeCategoryId = Guid.NewGuid().ToString();
				TestContext.Api.Jobs.Update(job);
			}
			catch (MediaOpsException exception)
			{
				var traceData = exception.TraceData.ErrorData.OfType<JobCategoryNotFoundError>().Single();
				Assert.IsNotNull(traceData);
				Assert.AreEqual(job.Id, traceData.Id);
				Assert.AreEqual(job.JobTypeCategoryId, traceData.CategoryId);

				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		[TestMethod]
		public void CreateJobTranslatesLegacySchedulingSource()
		{
			// The legacy fixed source value "Scheduling" must be translated to the fixed Scheduled job type category
			// so jobs created against the old implementation remain valid.
			var job = CreateJob("Scheduling");

			var read = TestContext.Api.Jobs.Read(job.Id);
			Assert.IsNotNull(read);
			Assert.AreEqual(JobTypes.Scheduled.ToString(), read.JobTypeCategoryId);
		}

		private static Scope GetJobScope()
		{
			return TestContext.CategoriesApi.Scopes.Read(JobCategoryScopes.JobTypes)
				?? throw new InvalidOperationException($"Category Scope '{JobCategoryScopes.JobTypes}' is not available");
		}

		private static void AssertCategoryAssignment(Guid jobId, Category? expectedCategory)
		{
			var job = TestContext.Api.Jobs.Read(jobId);
			Assert.IsNotNull(job);

			if (expectedCategory == null)
			{
				Assert.IsNull(job.JobTypeCategoryId);
			}
			else
			{
				Assert.AreEqual(expectedCategory.ID.ToString(), job.JobTypeCategoryId);
			}

			// Check if the job DOM instance was registered as a category item on the expected category.
			var categoryItems = TestContext.CategoriesApi.CategoryItems.Read(CategoryItemExposers.InstanceId.Equal(jobId.ToString())).ToArray();
			if (expectedCategory == null)
			{
				Assert.AreEqual(0, categoryItems.Length, "The job was registered as a category item while it shouldn't have been.");
			}
			else
			{
				Assert.AreEqual(1, categoryItems.Length, "Category items count mismatch");
				Assert.AreEqual(expectedCategory.ID.ToString(), categoryItems.Single().Category.ID.ToString());

				var childItems = expectedCategory.GetChildItems(TestContext.CategoriesApi.CategoryItems);
				Assert.AreEqual(1, childItems.Count(), "Child items count mismatch");
			}
		}

		private Category CreateCategory()
		{
			return objectCreator.CreateCategory(new Category
			{
				Name = $"JobCategory_{Guid.NewGuid()}",
				Scope = GetJobScope(),
			});
		}

		private Job CreateJob(string? categoryId)
		{
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			return objectCreator.CreateJob(new Job
			{
				Name = $"Job_{Guid.NewGuid()}",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
				JobTypeCategoryId = categoryId,
			});
		}
	}
}

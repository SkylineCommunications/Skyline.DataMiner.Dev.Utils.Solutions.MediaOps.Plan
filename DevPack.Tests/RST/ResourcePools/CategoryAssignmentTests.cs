namespace RT_MediaOps.Plan.RST.ResourcePools
{
	using System;
	using System.Linq;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.SLSearch.Exceptions;
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
		public void CreatePoolAssignsCategory()
		{
			var category = CreateCategory();
			var resourcePool = objectCreator.CreateResourcePool(new ResourcePool
			{
				Name = $"ResourcePool_{Guid.NewGuid()}",
				CategoryId = category.ID.ToString(),
			});

			AssertCategoryAssignment(resourcePool.Id, category);
		}

		[TestMethod]
		public void UpdatePoolAssignsCategory()
		{
			var category = CreateCategory();
			var resourcePool = objectCreator.CreateResourcePool(new ResourcePool
			{
				Name = $"ResourcePool_{Guid.NewGuid()}",
			});

			resourcePool.CategoryId = category.ID.ToString();
			TestContext.Api.ResourcePools.Update(resourcePool);

			AssertCategoryAssignment(resourcePool.Id, category);
		}

		[TestMethod]
		public void UpdatePoolChangesCategory()
		{
			var category1 = CreateCategory();
			var category2 = CreateCategory();
			var resourcePool = objectCreator.CreateResourcePool(new ResourcePool
			{
				Name = $"ResourcePool_{Guid.NewGuid()}",
				CategoryId = category1.ID.ToString(),
			});

			AssertCategoryAssignment(resourcePool.Id, category1);

			resourcePool.CategoryId = category2.ID.ToString();
			TestContext.Api.ResourcePools.Update(resourcePool);

			AssertCategoryAssignment(resourcePool.Id, category2);
		}

		[TestMethod]
		public void UpdatePoolRemovesCategory()
		{
			var category = CreateCategory();
			var resourcePool = objectCreator.CreateResourcePool(new ResourcePool
			{
				Name = $"ResourcePool_{Guid.NewGuid()}",
				CategoryId = category.ID.ToString(),
			});

			AssertCategoryAssignment(resourcePool.Id, category);

			resourcePool.CategoryId = null;
			TestContext.Api.ResourcePools.Update(resourcePool);

			AssertCategoryAssignment(resourcePool.Id, null);
		}

		[TestMethod]
		public void CreatePoolWithInvalidCategoryIdThrowsException()
		{
			MediaOpsException expectedException = null;
			try
			{
				objectCreator.CreateResourcePool(new ResourcePool
				{
					Name = $"ResourcePool_{Guid.NewGuid()}",
					CategoryId = Guid.NewGuid().ToString(),
				});
			}
			catch (MediaOpsException exception)
			{
				var tracedata = exception.TraceData.ErrorData.OfType<ResourcePoolCategoryNotFoundError>().Single();
				Assert.IsNotNull(tracedata);

				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		[TestMethod]
		public void UpdatePoolWithInvalidCategoryIdThrowsException()
		{
			var resourcePool = objectCreator.CreateResourcePool(new ResourcePool
			{
				Name = $"ResourcePool_{Guid.NewGuid()}",
			});


			try
			{
				resourcePool.CategoryId = Guid.NewGuid().ToString();
				resourcePool = TestContext.Api.ResourcePools.Update(resourcePool);
			}
			catch (MediaOpsException exception)
			{
				var tracedata = exception.TraceData.ErrorData.OfType<ResourcePoolCategoryNotFoundError>().Single();
				Assert.IsNotNull(tracedata);

				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		private static Scope GetResourcePoolScope()
		{
			return TestContext.CategoriesApi.Scopes.Read(ScopeExposers.Name.Equal("Resource Pools")).FirstOrDefault()
				?? throw new InvalidOperationException("Category Scope 'Resource Pools' is not available");
		}

		private Category CreateCategory()
		{
			return objectCreator.CreateCategory(new Category
			{
				Name = $"ResourcePoolCategory_{Guid.NewGuid()}",
				Scope = GetResourcePoolScope(),
			});
		}

		private static void AssertCategoryAssignment(Guid resourcePoolId, Category expectedCategory)
		{
			var resourcePool = TestContext.Api.ResourcePools.Read(resourcePoolId);
			Assert.IsNotNull(resourcePool);

			if (expectedCategory == null)
			{
				Assert.IsNull(resourcePool.CategoryId);
			}
			else
			{
				Assert.AreEqual(expectedCategory.ID.ToString(), resourcePool.CategoryId);
			}

			var categoryItems = TestContext.CategoriesApi.CategoryItems.Read(CategoryItemExposers.InstanceId.Equal(resourcePoolId.ToString())).ToArray();
			if (expectedCategory == null)
			{
				Assert.AreEqual(0, categoryItems.Length);
				return;
			}

			Assert.AreEqual(1, categoryItems.Length);
			Assert.AreEqual(expectedCategory.ID.ToString(), categoryItems.Single().Category.ID.ToString());

			var childItems = expectedCategory.GetChildItems(TestContext.CategoriesApi.CategoryItems);
			Assert.AreEqual(1, childItems.Count());
		}
	}
}

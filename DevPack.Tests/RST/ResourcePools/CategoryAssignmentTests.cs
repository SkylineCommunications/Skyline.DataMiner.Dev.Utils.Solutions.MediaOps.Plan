namespace RT_MediaOps.Plan.RST.ResourcePools
{
	using System;
	using System.Linq;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.Categories.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	[TestCategory("IntegrationTest")]
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
		public void UpdatePoolAssignsCategory()
		{
			var category = CreateCategory();
			var resourcePool = objectCreator.CreateResourcePool(new ResourcePool
			{
				Name = $"ResourcePool_{Guid.NewGuid()}",
			});

			resourcePool.CategoryId = category.ID.ToString();
			TestContext.Api.ResourcePools.Update(resourcePool);

			AssertCategoryAssignment(resourcePool.Id, category.ID.ToString());
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

			AssertCategoryAssignment(resourcePool.Id, category1.ID.ToString());

			resourcePool.CategoryId = category2.ID.ToString();
			TestContext.Api.ResourcePools.Update(resourcePool);

			AssertCategoryAssignment(resourcePool.Id, category2.ID.ToString());
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

			AssertCategoryAssignment(resourcePool.Id, category.ID.ToString());

			resourcePool.CategoryId = null;
			TestContext.Api.ResourcePools.Update(resourcePool);

			AssertCategoryAssignment(resourcePool.Id, null);
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

		private static void AssertCategoryAssignment(Guid resourcePoolId, string expectedCategoryId)
		{
			var resourcePool = TestContext.Api.ResourcePools.Read(resourcePoolId);
			Assert.IsNotNull(resourcePool);

			if (expectedCategoryId == null)
			{
				Assert.IsNull(resourcePool.CategoryId);
			}
			else
			{
				Assert.AreEqual(expectedCategoryId, resourcePool.CategoryId);
			}

			var categoryItems = TestContext.CategoriesApi.CategoryItems.Read(CategoryItemExposers.InstanceId.Equal(resourcePoolId.ToString())).ToArray();
			if (expectedCategoryId == null)
			{
				Assert.AreEqual(0, categoryItems.Length);
				return;
			}

			Assert.AreEqual(1, categoryItems.Length);
			Assert.AreEqual(expectedCategoryId, categoryItems.Single().Category.ID.ToString());
		}

	}
}

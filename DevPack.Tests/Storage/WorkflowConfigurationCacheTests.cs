namespace RT_MediaOps.Plan.Storage
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	[TestClass]
	public sealed class WorkflowConfigurationCacheTests
	{
		[TestMethod]
		public void Resolve_WithoutScope_ReportsEverythingAsMissing()
		{
			var cache = new WorkflowConfigurationCache();
			var id = Guid.NewGuid();

			var resolved = cache.Resolve([id], out var missing);

			Assert.IsFalse(cache.IsActive);
			Assert.AreEqual(0, resolved.Count);
			CollectionAssert.AreEquivalent(new[] { id }, missing.ToList());
		}

		[TestMethod]
		public void Resolve_SeededConfiguration_IsReturnedWithoutReading()
		{
			var cache = new WorkflowConfigurationCache();
			var configuration = new ConfigurationInstance(Guid.NewGuid());

			using (cache.BeginScope())
			{
				cache.Seed([configuration]);

				var resolved = cache.Resolve([configuration.ID.Id], out var missing);

				Assert.AreEqual(0, missing.Count);
				Assert.AreSame(configuration, resolved.Single());
			}
		}

		[TestMethod]
		public void Resolve_IdentifierThatDoesNotExist_IsOnlyReadOnce()
		{
			var cache = new WorkflowConfigurationCache();
			var id = Guid.NewGuid();

			using (cache.BeginScope())
			{
				cache.Resolve([id], out var missing);
				cache.Store(missing, []);

				cache.Resolve([id], out var missingAfterStore);

				Assert.AreEqual(0, missingAfterStore.Count);
			}
		}

		[TestMethod]
		public void BeginScope_Nested_OnlyClearsWhenTheOutermostScopeCloses()
		{
			var cache = new WorkflowConfigurationCache();
			var configuration = new ConfigurationInstance(Guid.NewGuid());

			using (cache.BeginScope())
			{
				cache.Seed([configuration]);

				using (cache.BeginScope())
				{
					Assert.IsTrue(cache.IsActive);
				}

				cache.Resolve([configuration.ID.Id], out var missing);
				Assert.AreEqual(0, missing.Count, "The nested scope cleared the cache of the outer scope.");
			}

			Assert.IsFalse(cache.IsActive);

			cache.Resolve([configuration.ID.Id], out var missingAfterScope);
			Assert.AreEqual(1, missingAfterScope.Count, "The cache was not cleared when the outermost scope closed.");
		}

		[TestMethod]
		public void Seed_WithoutScope_Throws()
		{
			var cache = new WorkflowConfigurationCache();

			Assert.ThrowsException<InvalidOperationException>(() => cache.Seed([new ConfigurationInstance(Guid.NewGuid())]));
		}
	}
}

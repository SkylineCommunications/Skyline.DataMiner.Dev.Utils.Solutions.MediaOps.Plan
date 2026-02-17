namespace RT_MediaOps.Plan.RST.Resources
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

	[TestClass]
	public sealed class StorageTests
	{
		[TestMethod]
		public void SetCache_HappyPath()
		{
			var resourceInstance = new ResourceInstance();

			var resourcePool1 = new ResourcepoolInstance();
			var resourcePool2 = new ResourcepoolInstance();
			resourceInstance.SetCache<ResourcepoolInstance>([resourcePool1, resourcePool2]);

			var propertyConfiguration1 = new ResourcepropertyInstance();
			var propertyConfiguration2 = new ResourcepropertyInstance();
			resourceInstance.SetCache<ResourcepropertyInstance>([propertyConfiguration1, propertyConfiguration2]);

			var cachedResourcePools = resourceInstance.GetFromCache<ResourcepoolInstance>().ToList();
			Assert.IsNotNull(cachedResourcePools);
			Assert.AreEqual(2, cachedResourcePools.OfType<ResourcepoolInstance>().Count());
			Assert.IsTrue(cachedResourcePools.Exists(x => x.ID.Id == resourcePool1.ID.Id));
			Assert.IsTrue(cachedResourcePools.Exists(x => x.ID.Id == resourcePool2.ID.Id));

			var cachedPropertyConfigurations = resourceInstance.GetFromCache<ResourcepropertyInstance>().ToList();
			Assert.IsNotNull(cachedPropertyConfigurations);
			Assert.AreEqual(2, cachedPropertyConfigurations.OfType<ResourcepropertyInstance>().Count());
			Assert.IsTrue(cachedPropertyConfigurations.Exists(x => x.ID.Id == propertyConfiguration1.ID.Id));
			Assert.IsTrue(cachedPropertyConfigurations.Exists(x => x.ID.Id == propertyConfiguration2.ID.Id));
		}

		[TestMethod]
		public void SetCache_Update()
		{
			var resourceInstance = new ResourceInstance();

			var resourcePool1 = new ResourcepoolInstance()
			{
				ResourcePoolInfo = new ResourcePoolInfoSection
				{
					Name = "Pool 1",
				},
			};
			var resourcePool2 = new ResourcepoolInstance()
			{
				ResourcePoolInfo = new ResourcePoolInfoSection
				{
					Name = "Pool 2",
				},
			};
			resourceInstance.SetCache<ResourcepoolInstance>([resourcePool1, resourcePool2]);

			var cachedResourcePools = resourceInstance.GetFromCache<ResourcepoolInstance>().ToList();
			Assert.IsNotNull(cachedResourcePools);
			Assert.AreEqual(2, cachedResourcePools.OfType<ResourcepoolInstance>().Count());
			Assert.IsTrue(cachedResourcePools.Exists(x => x.ID.Id == resourcePool1.ID.Id && x.ResourcePoolInfo.Name == "Pool 1"));
			Assert.IsTrue(cachedResourcePools.Exists(x => x.ID.Id == resourcePool2.ID.Id && x.ResourcePoolInfo.Name == "Pool 2"));

			resourcePool1.ResourcePoolInfo.Name = "Pool 1_Updated";
			var resourcePool3 = new ResourcepoolInstance()
			{
				ResourcePoolInfo = new ResourcePoolInfoSection
				{
					Name = "Pool 3",
				},
			};
			resourceInstance.SetCache<ResourcepoolInstance>([resourcePool1, resourcePool3]);

			cachedResourcePools = resourceInstance.GetFromCache<ResourcepoolInstance>().ToList();
			Assert.IsNotNull(cachedResourcePools);
			Assert.AreEqual(2, cachedResourcePools.OfType<ResourcepoolInstance>().Count());
			Assert.IsTrue(cachedResourcePools.Exists(x => x.ID.Id == resourcePool1.ID.Id && x.ResourcePoolInfo.Name == "Pool 1_Updated"));
			Assert.IsTrue(cachedResourcePools.Exists(x => x.ID.Id == resourcePool3.ID.Id && x.ResourcePoolInfo.Name == "Pool 3"));
		}

		[TestMethod]
		public void SetCache_NullCollection()
		{
			var resourceInstance = new ResourceInstance();

			try
			{
				resourceInstance.SetCache<ResourcepoolInstance>(null);
			}
			catch (ArgumentNullException)
			{
				// Expected
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void SetCache_EmptyCollection()
		{
			var resourceInstance = new ResourceInstance();
			resourceInstance.SetCache<ResourcepoolInstance>([]);

			var cachedResourcePools = resourceInstance.GetFromCache<ResourcepoolInstance>().ToList();
			Assert.AreEqual(0, cachedResourcePools.OfType<ResourcepoolInstance>().Count());
		}

		[TestMethod]
		public void SetCache_CollectionWithNulls()
		{
			var resourceInstance = new ResourceInstance();

			var resourcePool1 = new ResourcepoolInstance();
			var resourcePool2 = new ResourcepoolInstance();

			try
			{
				resourceInstance.SetCache<ResourcepoolInstance?>([resourcePool1, null, resourcePool2, null]);
			}
			catch (ArgumentException)
			{
				// Expected
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void SetCache_MixedCollection()
		{
			var resourceInstance = new ResourceInstance();

			var resourcePool1 = new ResourcepoolInstance();
			var resourcePool2 = new ResourcepoolInstance();

			var propertyConfiguration1 = new ResourcepropertyInstance();
			var propertyConfiguration2 = new ResourcepropertyInstance();

			try
			{
				resourceInstance.SetCache<DomInstanceBase>([resourcePool1, resourcePool2, propertyConfiguration1, propertyConfiguration2]);
			}
			catch (InvalidOperationException ex)
			{
				Assert.AreEqual("Cannot use DomInstanceBase directly. Use a derived type.", ex.Message);
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void AddToCache_HappyPath()
		{
			var resourceInstance = new ResourceInstance();

			var resourcePool1 = new ResourcepoolInstance();
			var resourcePool2 = new ResourcepoolInstance();
			resourceInstance.AddToCache<ResourcepoolInstance>([resourcePool1, resourcePool2]);

			var propertyConfiguration1 = new ResourcepropertyInstance();
			var propertyConfiguration2 = new ResourcepropertyInstance();
			resourceInstance.AddToCache<ResourcepropertyInstance>([propertyConfiguration1, propertyConfiguration2]);

			var cachedResourcePools = resourceInstance.GetFromCache<ResourcepoolInstance>().ToList();
			Assert.IsNotNull(cachedResourcePools);
			Assert.AreEqual(2, cachedResourcePools.OfType<ResourcepoolInstance>().Count());
			Assert.IsTrue(cachedResourcePools.Exists(x => x.ID.Id == resourcePool1.ID.Id));
			Assert.IsTrue(cachedResourcePools.Exists(x => x.ID.Id == resourcePool2.ID.Id));

			var cachedPropertyConfigurations = resourceInstance.GetFromCache<ResourcepropertyInstance>().ToList();
			Assert.IsNotNull(cachedPropertyConfigurations);
			Assert.AreEqual(2, cachedPropertyConfigurations.OfType<ResourcepropertyInstance>().Count());
			Assert.IsTrue(cachedPropertyConfigurations.Exists(x => x.ID.Id == propertyConfiguration1.ID.Id));
			Assert.IsTrue(cachedPropertyConfigurations.Exists(x => x.ID.Id == propertyConfiguration2.ID.Id));
		}

		[TestMethod]
		public void AddToCache_Update()
		{
			var resourceInstance = new ResourceInstance();

			var resourcePool1 = new ResourcepoolInstance()
			{
				ResourcePoolInfo = new ResourcePoolInfoSection
				{
					Name = "Pool 1",
				},
			};
			var resourcePool2 = new ResourcepoolInstance()
			{
				ResourcePoolInfo = new ResourcePoolInfoSection
				{
					Name = "Pool 2",
				},
			};
			resourceInstance.AddToCache<ResourcepoolInstance>([resourcePool1, resourcePool2]);

			var cachedResourcePools = resourceInstance.GetFromCache<ResourcepoolInstance>().ToList();
			Assert.IsNotNull(cachedResourcePools);
			Assert.AreEqual(2, cachedResourcePools.OfType<ResourcepoolInstance>().Count());
			Assert.IsTrue(cachedResourcePools.Exists(x => x.ID.Id == resourcePool1.ID.Id && x.ResourcePoolInfo.Name == "Pool 1"));
			Assert.IsTrue(cachedResourcePools.Exists(x => x.ID.Id == resourcePool2.ID.Id && x.ResourcePoolInfo.Name == "Pool 2"));

			resourcePool1.ResourcePoolInfo.Name = "Pool 1_Updated";
			var resourcePool3 = new ResourcepoolInstance()
			{
				ResourcePoolInfo = new ResourcePoolInfoSection
				{
					Name = "Pool 3",
				},
			};
			resourceInstance.AddToCache<ResourcepoolInstance>([resourcePool1, resourcePool3]);

			cachedResourcePools = resourceInstance.GetFromCache<ResourcepoolInstance>().ToList();
			Assert.IsNotNull(cachedResourcePools);
			Assert.AreEqual(3, cachedResourcePools.OfType<ResourcepoolInstance>().Count());
			Assert.IsTrue(cachedResourcePools.Exists(x => x.ID.Id == resourcePool1.ID.Id && x.ResourcePoolInfo.Name == "Pool 1_Updated"));
			Assert.IsTrue(cachedResourcePools.Exists(x => x.ID.Id == resourcePool2.ID.Id && x.ResourcePoolInfo.Name == "Pool 2"));
			Assert.IsTrue(cachedResourcePools.Exists(x => x.ID.Id == resourcePool3.ID.Id && x.ResourcePoolInfo.Name == "Pool 3"));
		}

		[TestMethod]
		public void AddToCache_NullCollection()
		{
			var resourceInstance = new ResourceInstance();

			try
			{
				resourceInstance.AddToCache<ResourcepoolInstance>(null);
			}
			catch (ArgumentNullException)
			{
				// Expected
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void AddToCache_EmptyCollection()
		{
			var resourceInstance = new ResourceInstance();
			resourceInstance.AddToCache<ResourcepoolInstance>([]);

			var cachedResourcePools = resourceInstance.GetFromCache<ResourcepoolInstance>().ToList();
			Assert.AreEqual(0, cachedResourcePools.OfType<ResourcepoolInstance>().Count());
		}

		[TestMethod]
		public void AddToCache_CollectionWithNulls()
		{
			var resourceInstance = new ResourceInstance();

			var resourcePool1 = new ResourcepoolInstance();
			var resourcePool2 = new ResourcepoolInstance();

			try
			{
				resourceInstance.AddToCache<ResourcepoolInstance?>([resourcePool1, null, resourcePool2, null]);
			}
			catch (ArgumentException)
			{
				// Expected
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void AddToCache_MixedCollection()
		{
			var resourceInstance = new ResourceInstance();

			var resourcePool1 = new ResourcepoolInstance();
			var resourcePool2 = new ResourcepoolInstance();

			var propertyConfiguration1 = new ResourcepropertyInstance();
			var propertyConfiguration2 = new ResourcepropertyInstance();

			try
			{
				resourceInstance.AddToCache<DomInstanceBase>([resourcePool1, resourcePool2, propertyConfiguration1, propertyConfiguration2]);
			}
			catch (InvalidOperationException ex)
			{
				Assert.AreEqual("Cannot use DomInstanceBase directly. Use a derived type.", ex.Message);
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void GetFromCache_NotCachedInstance()
		{
			var resourceInstance = new ResourceInstance();

			var resourcePool1 = new ResourcepoolInstance();
			var resourcePool2 = new ResourcepoolInstance();
			resourceInstance.SetCache<ResourcepoolInstance>([resourcePool1, resourcePool2]);

			var cachedPropertyConfigurations = resourceInstance.GetFromCache<ResourcepropertyInstance>().ToList();
			Assert.IsNotNull(cachedPropertyConfigurations);
		}
	}
}

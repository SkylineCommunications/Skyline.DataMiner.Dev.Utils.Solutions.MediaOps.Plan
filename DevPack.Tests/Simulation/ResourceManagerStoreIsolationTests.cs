namespace RT_MediaOps.Plan.Simulation
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.ResourceManager.Helpers;
	using Skyline.DataMiner.Net.SRM.Capacities;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	/// <summary>
	/// Guards that the simulated Resource Manager copies objects in and out, the way an out-of-process agent does.
	/// Without this a caller can mutate the store by accident, which hides missing saves and fakes writes that never happened.
	/// </summary>
	[TestClass]
	public sealed class ResourceManagerStoreIsolationTests
	{
		[TestMethod]
		public void MutatingFetchedResourceDoesNotAffectStore()
		{
			var helper = CreateHelper();
			var id = Guid.NewGuid();

			var resource = new Resource(id)
			{
				Name = "Original",
				MaxConcurrency = 1,
			};
			resource.Capacities.Add(new MultiResourceCapacity
			{
				CapacityProfileID = Guid.NewGuid(),
				Value = new Skyline.DataMiner.Net.Profiles.CapacityParameterValue { MaxDecimalQuantity = 10 },
			});
			resource.PoolGUIDs.Add(Guid.NewGuid());

			helper.AddOrUpdateResources(resource);

			var fetched = helper.GetResource(id);
			fetched.Name = "Mutated";
			fetched.MaxConcurrency = 99;
			fetched.Capacities.Single().Value.MaxDecimalQuantity = 20;
			fetched.PoolGUIDs.Clear();

			var refetched = helper.GetResource(id);
			Assert.AreEqual("Original", refetched.Name);
			Assert.AreEqual(1, refetched.MaxConcurrency);
			Assert.AreEqual(10m, refetched.Capacities.Single().Value.MaxDecimalQuantity, "Nested state must be copied too.");
			Assert.AreEqual(1, refetched.PoolGUIDs.Count);
		}

		[TestMethod]
		public void MutatingResourceAfterStoringDoesNotAffectStore()
		{
			var helper = CreateHelper();
			var id = Guid.NewGuid();

			var resource = new Resource(id)
			{
				Name = "Original",
				MaxConcurrency = 1,
			};

			helper.AddOrUpdateResources(resource);

			resource.Name = "Mutated";
			resource.MaxConcurrency = 99;

			var fetched = helper.GetResource(id);
			Assert.AreEqual("Original", fetched.Name);
			Assert.AreEqual(1, fetched.MaxConcurrency);
		}

		[TestMethod]
		public void MutatingFetchedResourcePoolDoesNotAffectStore()
		{
			var helper = CreateHelper();
			var id = Guid.NewGuid();

			helper.AddOrUpdateResourcePools(new ResourcePool(id) { Name = "Original" });

			var fetched = helper.GetResourcePool(id);
			fetched.Name = "Mutated";

			Assert.AreEqual("Original", helper.GetResourcePool(id).Name);
		}

		private static ResourceManagerHelper CreateHelper()
		{
			var connection = MediaOpsPlanSimulation.Create().CreateConnection();

			return new ResourceManagerHelper(connection.HandleSingleResponseMessage);
		}
	}
}

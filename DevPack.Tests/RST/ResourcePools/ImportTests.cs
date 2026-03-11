namespace RT_MediaOps.Plan.RST.ResourcePools
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using CoreResource = Skyline.DataMiner.Net.Messages.Resource;
	using CoreResourcePool = Skyline.DataMiner.Net.Messages.ResourcePool;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class ImportTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public ImportTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void UnmanagedResourceImport()
		{
			var prefix = Guid.NewGuid();

			// Initial setup
			var corePool = new CoreResourcePool(Guid.NewGuid())
			{
				Name = $"{prefix}_ResourcePool",
			};
			objectCreator.CreateCoreResourcePool(corePool);

			var coreResource1 = new CoreResource(Guid.NewGuid())
			{
				Name = $"{prefix}_Resource1",
				MaxConcurrency = 1,
			};
			coreResource1.PoolGUIDs.Add(corePool.GUID);

			var coreResource2 = new CoreResource(Guid.NewGuid())
			{
				Name = $"{prefix}_Resource2",
				MaxConcurrency = 2,
			};
			coreResource2.PoolGUIDs.Add(corePool.GUID);

			var coreResource3 = new CoreResource(Guid.NewGuid())
			{
				Name = $"{prefix}_Resource3",
				MaxConcurrency = 3,
			};
			coreResource3.PoolGUIDs.Add(corePool.GUID);

			objectCreator.CreateCoreResources([coreResource1, coreResource2, coreResource3]);

			// Import resource pool
			TestContext.Api.ResourcePools.Import(corePool);

			// Verify API resource pool
			var apiResourcePool = TestContext.Api.ResourcePools.Read(ResourcePoolExposers.Name.Equal(corePool.Name)).SingleOrDefault();
			Assert.IsNotNull(apiResourcePool);
			objectCreator.StoreResourcePoolIds([apiResourcePool.Id]);

			Assert.AreEqual(ResourcePoolState.Complete, apiResourcePool.State);
			Assert.AreEqual(corePool.ID, apiResourcePool.CoreResourcePoolId);

			// Verify API resources
			var apiResources = TestContext.Api.Resources.Read(ResourceExposers.Name.Contains($"{prefix}_Resource"))
				.Where(x => x.Name.StartsWith($"{prefix}_Resource"))
				.ToList();
			Assert.AreEqual(3, apiResources.Count);
			objectCreator.StoreResourceIds(apiResources.Select(x => x.Id));

			foreach (var coreResource in new[] { coreResource1, coreResource2, coreResource3 })
			{
				var apiResource = apiResources.SingleOrDefault(x => x.Name == coreResource.Name);
				Assert.IsNotNull(apiResource);
				Assert.AreEqual(coreResource.MaxConcurrency, apiResource.Concurrency);
				Assert.AreEqual(ResourceState.Complete, apiResource.State);
				Assert.AreEqual(coreResource.ID, apiResource.CoreResourceId);
				Assert.AreEqual(1, apiResource.ResourcePoolIds.Count);
				Assert.IsTrue(apiResource.ResourcePoolIds.Contains(apiResourcePool.Id));
				Assert.IsTrue(apiResource is UnmanagedResource);
			}

			// Verify CORE resources
			var coreResourceFilter = new ORFilterElement<CoreResource>(
				Skyline.DataMiner.Net.Messages.ResourceExposers.ID.Equal(coreResource1.ID),
				Skyline.DataMiner.Net.Messages.ResourceExposers.ID.Equal(coreResource2.ID),
				Skyline.DataMiner.Net.Messages.ResourceExposers.ID.Equal(coreResource3.ID));

			var coreResources = TestContext.ResourceManagerHelper.GetResources(coreResourceFilter).ToList();
			Assert.AreEqual(3, coreResources.Count);
			foreach (var coreResource in coreResources)
			{
				Assert.AreEqual(1, coreResource.Capabilities.Count);
				var resourceCapability = coreResource.Capabilities.FirstOrDefault();
				Assert.IsNotNull(resourceCapability);
				Assert.AreEqual(TestContext.Api.Capabilities.SystemCapabilities.ResourceType.Id, resourceCapability.CapabilityProfileID);
				Assert.AreEqual(1, resourceCapability.Value.Discreets.Count);
				Assert.IsTrue(resourceCapability.Value.Discreets.Contains("Unlinked Resource"));
			}
		}

		[TestMethod]
		public void ElementResourceImport()
		{
			var prefix = Guid.NewGuid();

			// Verify if connector Generic Camera is available on the system
			var protocols = TestContext.Dms.GetProtocols().Where(p => p.Name == "Generic Camera").ToList();
			if (protocols.Count == 0)
			{
				Assert.Fail("Connector 'Generic Camera' is not available on the system. Cannot proceed with the test.");
			}

			var productionProtocol = protocols.FirstOrDefault(x => x.Version == "Production");
			if (productionProtocol == null)
			{
				Assert.Fail("Connector 'Generic Camera' does not have a Production version available on the system. Cannot proceed with the test.");
			}

			// Initial setup
			var httpConnection = new HttpConnection()
			{
				TcpConfiguration = new Tcp
				{
					RemoteHost = "127.0.0.1",
					RemotePort = 100,
				},
			};
			var elementConfiguration = new ElementConfiguration(TestContext.Dms, $"{prefix}_Camera", productionProtocol, [httpConnection]);
			var elementId = objectCreator.CreateElement(elementConfiguration);

			var corePool = new CoreResourcePool(Guid.NewGuid())
			{
				Name = $"{prefix}_ResourcePool",
			};
			objectCreator.CreateCoreResourcePool(corePool);

			var coreResource = new CoreResource(Guid.NewGuid())
			{
				Name = $"{prefix}_Resource",
				MaxConcurrency = 1,
				DmaID = elementId.AgentId,
				ElementID = elementId.ElementId,
			};
			coreResource.PoolGUIDs.Add(corePool.GUID);
			objectCreator.CreateCoreResource(coreResource);

			// Import resource pool
			TestContext.Api.ResourcePools.Import(corePool);

			// Verify API resource pool
			var apiResourcePool = TestContext.Api.ResourcePools.Read(ResourcePoolExposers.Name.Equal(corePool.Name)).SingleOrDefault();
			Assert.IsNotNull(apiResourcePool);
			objectCreator.StoreResourcePoolIds([apiResourcePool.Id]);

			Assert.AreEqual(ResourcePoolState.Complete, apiResourcePool.State);
			Assert.AreEqual(corePool.ID, apiResourcePool.CoreResourcePoolId);

			// Verify API resource
			var apiResource = TestContext.Api.Resources.Read(ResourceExposers.Name.Equal(coreResource.Name)).SingleOrDefault();
			Assert.IsNotNull(apiResource);
			objectCreator.StoreResourceIds([apiResource.Id]);

			Assert.IsNotNull(apiResource);
			Assert.AreEqual(coreResource.MaxConcurrency, apiResource.Concurrency);
			Assert.AreEqual(ResourceState.Complete, apiResource.State);
			Assert.AreEqual(coreResource.ID, apiResource.CoreResourceId);
			Assert.AreEqual(1, apiResource.ResourcePoolIds.Count);
			Assert.IsTrue(apiResource.ResourcePoolIds.Contains(apiResourcePool.Id));
			var apiElementResource = apiResource as ElementResource;
			Assert.IsNotNull(apiElementResource);
			Assert.AreEqual(elementId.AgentId, apiElementResource.AgentId);
			Assert.AreEqual(elementId.ElementId, apiElementResource.ElementId);

			// Verify CORE resource
			coreResource = TestContext.ResourceManagerHelper.GetResource(coreResource.ID);
			Assert.IsNotNull(coreResource);

			Assert.AreEqual(1, coreResource.Capabilities.Count);
			var resourceCapability = coreResource.Capabilities.FirstOrDefault();
			Assert.IsNotNull(resourceCapability);
			Assert.AreEqual(TestContext.Api.Capabilities.SystemCapabilities.ResourceType.Id, resourceCapability.CapabilityProfileID);
			Assert.AreEqual(1, resourceCapability.Value.Discreets.Count);
			Assert.IsTrue(resourceCapability.Value.Discreets.Contains("Element"));
		}

		[TestMethod]
		public void FunctionResourceWithElementLinkImport()
		{
			var prefix = Guid.NewGuid();

			// Verify if connector Generic Camera is available on the system
			var protocols = TestContext.Dms.GetProtocols().Where(p => p.Name == "Generic Camera").ToList();
			if (protocols.Count == 0)
			{
				Assert.Fail("Connector 'Generic Camera' is not available on the system. Cannot proceed with the test.");
			}

			var productionProtocol = protocols.FirstOrDefault(x => x.Version == "Production");
			if (productionProtocol == null)
			{
				Assert.Fail("Connector 'Generic Camera' does not have a Production version available on the system. Cannot proceed with the test.");
			}

			// Verify if function Generic Camera is available on the system
			var protocolfunctions = TestContext.ProtocolFunctionHelper.GetProtocolFunctions("Generic Camera");
			if (protocolfunctions.Count == 0)
			{
				Assert.Fail("Connector 'Generic Camera' has no active function available on the system. Cannot proceed with the test.");
			}

			var activeFunctionVersion = protocolfunctions
				.SelectMany(x => x.ProtocolFunctionVersions)
				.FirstOrDefault(v => v.Active);
			if (activeFunctionVersion == null)
			{
				Assert.Fail("Connector 'Generic Camera' has no active function version available on the system. Cannot proceed with the test.");
			}

			// Initial setup
			var httpConnection = new HttpConnection()
			{
				TcpConfiguration = new Tcp
				{
					RemoteHost = "127.0.0.1",
					RemotePort = 100,
				},
			};
			var elementConfiguration = new ElementConfiguration(TestContext.Dms, $"{prefix}_Camera", productionProtocol, [httpConnection]);
			var elementId = objectCreator.CreateElement(elementConfiguration);

			var corePool = new CoreResourcePool(Guid.NewGuid())
			{
				Name = $"{prefix}_ResourcePool",
			};
			objectCreator.CreateCoreResourcePool(corePool);

			var functionId = activeFunctionVersion.FunctionDefinitions.First().GUID;
			var coreResource = new Skyline.DataMiner.Net.ResourceManager.Objects.FunctionResource()
			{
				ID = Guid.NewGuid(),
				Name = $"{prefix}_Resource",
				MaxConcurrency = 1,
				MainDVEDmaID = elementId.AgentId,
				MainDVEElementID = elementId.ElementId,
				FunctionGUID = functionId,
			};
			coreResource.PoolGUIDs.Add(corePool.GUID);
			objectCreator.CreateCoreResource(coreResource);

			// Import resource pool
			TestContext.Api.ResourcePools.Import(corePool);

			// Verify API resource pool
			var apiResourcePool = TestContext.Api.ResourcePools.Read(ResourcePoolExposers.Name.Equal(corePool.Name)).SingleOrDefault();
			Assert.IsNotNull(apiResourcePool);
			objectCreator.StoreResourcePoolIds([apiResourcePool.Id]);

			Assert.AreEqual(ResourcePoolState.Complete, apiResourcePool.State);
			Assert.AreEqual(corePool.ID, apiResourcePool.CoreResourcePoolId);

			// Verify API resource
			var apiResource = TestContext.Api.Resources.Read(ResourceExposers.Name.Equal(coreResource.Name)).SingleOrDefault();
			Assert.IsNotNull(apiResource);
			objectCreator.StoreResourceIds([apiResource.Id]);

			Assert.IsNotNull(apiResource);
			Assert.AreEqual(coreResource.MaxConcurrency, apiResource.Concurrency);
			Assert.AreEqual(ResourceState.Complete, apiResource.State);
			Assert.AreEqual(coreResource.ID, apiResource.CoreResourceId);
			Assert.AreEqual(1, apiResource.ResourcePoolIds.Count);
			Assert.IsTrue(apiResource.ResourcePoolIds.Contains(apiResourcePool.Id));
			var apiFunctionResource = apiResource as VirtualFunctionResource;
			Assert.IsNotNull(apiFunctionResource);
			Assert.AreEqual(elementId.AgentId, apiFunctionResource.AgentId);
			Assert.AreEqual(elementId.ElementId, apiFunctionResource.ElementId);
			Assert.AreEqual(functionId, apiFunctionResource.FunctionId);

			// Verify CORE resource
			coreResource = TestContext.ResourceManagerHelper.GetResource(coreResource.ID) as Skyline.DataMiner.Net.ResourceManager.Objects.FunctionResource;
			Assert.IsNotNull(coreResource);

			Assert.AreEqual(1, coreResource.Capabilities.Count);
			var resourceCapability = coreResource.Capabilities.FirstOrDefault();
			Assert.IsNotNull(resourceCapability);
			Assert.AreEqual(TestContext.Api.Capabilities.SystemCapabilities.ResourceType.Id, resourceCapability.CapabilityProfileID);
			Assert.AreEqual(1, resourceCapability.Value.Discreets.Count);
			Assert.IsTrue(resourceCapability.Value.Discreets.Contains("Virtual Function"));
		}

		[TestMethod]
		public void ImportManualAddedResources()
		{
			var prefix = Guid.NewGuid();

			// Initial setup
			var apiResourcePool = new ResourcePool
			{
				Name = $"{prefix}_API_ResourcePool",
			};
			objectCreator.CreateResourcePool(apiResourcePool);
			TestContext.Api.ResourcePools.Complete(apiResourcePool.Id);

			var apiResource1 = new UnmanagedResource
			{
				Name = $"{prefix}_API_Resource1",
			}
			.AssignToPool(apiResourcePool.Id);
			var apiResource2 = new UnmanagedResource
			{
				Name = $"{prefix}_API_Resource2",
			}
			.AssignToPool(apiResourcePool.Id);
			var apiResource3 = new UnmanagedResource
			{
				Name = $"{prefix}_API_Resource3",
			}
			.AssignToPool(apiResourcePool.Id);
			objectCreator.CreateResources([apiResource1, apiResource2, apiResource3]);
			TestContext.Api.Resources.Complete([apiResource1.Id, apiResource2.Id, apiResource3.Id]);

			apiResourcePool = TestContext.Api.ResourcePools.Read(apiResourcePool.Id);
			Assert.IsNotNull(apiResourcePool);
			Assert.AreNotEqual(Guid.Empty, apiResourcePool.CoreResourcePoolId);

			var coreResource1 = new CoreResource(Guid.NewGuid())
			{
				Name = $"{prefix}_CORE_Resource1",
				MaxConcurrency = 1,
			};
			coreResource1.PoolGUIDs.Add(apiResourcePool.CoreResourcePoolId);
			var coreResource2 = new CoreResource(Guid.NewGuid())
			{
				Name = $"{prefix}_CORE_Resource2",
				MaxConcurrency = 2,
			};
			coreResource2.PoolGUIDs.Add(apiResourcePool.CoreResourcePoolId);
			var coreResource3 = new CoreResource(Guid.NewGuid())
			{
				Name = $"{prefix}_CORE_Resource3",
				MaxConcurrency = 3,
			};
			coreResource3.PoolGUIDs.Add(apiResourcePool.CoreResourcePoolId);
			objectCreator.CreateCoreResources([coreResource1, coreResource2, coreResource3]);

			// Verify API resources before import
			var apiResources = TestContext.Api.Resources.GetResourcesInPool(apiResourcePool).ToList();
			Assert.AreEqual(3, apiResources.Count);

			// Import resource pool
			TestContext.Api.ResourcePools.Import(new CoreResourcePool(apiResourcePool.CoreResourcePoolId));

			// Verify API resources after import
			apiResources = TestContext.Api.Resources.GetResourcesInPool(apiResourcePool).ToList();
			Assert.AreEqual(6, apiResources.Count);
			objectCreator.StoreResourceIds(apiResources.Select(x => x.Id));
		}

		[TestMethod]
		public void ImportResourcesWithParameters()
		{
			var prefix = Guid.NewGuid();

			// Initial setup
			var capability = new Capability
			{
				Name = $"{prefix}_Capability",
			}
			.SetDiscretes(["Value 1", "Value 2", "Value 3"]);
			objectCreator.CreateCapability(capability);

			var numberCapacity = new NumberCapacity
			{
				Name = $"{prefix}_NumberCapacity",
			};
			var rangeCapacity = new RangeCapacity
			{
				Name = $"{prefix}_RangeCapacity",
			};
			objectCreator.CreateCapacities([numberCapacity, rangeCapacity]);

			var corePool = new CoreResourcePool(Guid.NewGuid())
			{
				Name = $"{prefix}_ResourcePool",
			};
			objectCreator.CreateCoreResourcePool(corePool);

			var coreResource = new CoreResource(Guid.NewGuid())
			{
				Name = $"{prefix}_Resource",
				MaxConcurrency = 1,
				Capabilities = new List<Skyline.DataMiner.Net.SRM.Capabilities.ResourceCapability>
				{
					new Skyline.DataMiner.Net.SRM.Capabilities.ResourceCapability(capability.Id)
					{
						Value = new Skyline.DataMiner.Net.Profiles.CapabilityParameterValue(["Value 2"]),
					},
				},
				Capacities = new List<Skyline.DataMiner.Net.SRM.Capacities.MultiResourceCapacity>
				{
					new Skyline.DataMiner.Net.SRM.Capacities.MultiResourceCapacity
					{
						CapacityProfileID = numberCapacity.Id,
						Value = new Skyline.DataMiner.Net.Profiles.CapacityParameterValue
						{
							MaxDecimalQuantity = 10,
						},
					},
					new Skyline.DataMiner.Net.SRM.Capacities.MultiResourceCapacity
					{
						CapacityProfileID = rangeCapacity.Id,
						Value = new Skyline.DataMiner.Net.Profiles.CapacityParameterValue
						{
							MinDecimalQuantity = 10,
							MaxDecimalQuantity = 20,
						},
					},
				},
			};
			coreResource.PoolGUIDs.Add(corePool.GUID);
			objectCreator.CreateCoreResource(coreResource);

			// Import resource pool
			TestContext.Api.ResourcePools.Import(corePool);

			// Verify API resource pool
			var apiResourcePool = TestContext.Api.ResourcePools.Read(ResourcePoolExposers.Name.Equal(corePool.Name)).SingleOrDefault();
			Assert.IsNotNull(apiResourcePool);
			objectCreator.StoreResourcePoolIds([apiResourcePool.Id]);

			// Verify API resource
			var apiResource = TestContext.Api.Resources.Read(ResourceExposers.Name.Equal(coreResource.Name)).SingleOrDefault();
			Assert.IsNotNull(apiResource);
			objectCreator.StoreResourceIds([apiResource.Id]);

			Assert.AreEqual(1, apiResource.Capabilities.Count);
			var capabilitySetting = apiResource.Capabilities.First();
			Assert.AreEqual(capability.Id, capabilitySetting.Id);
			Assert.AreEqual(1, capabilitySetting.Discretes.Count);
			Assert.IsTrue(capabilitySetting.Discretes.Contains("Value 2"));

			Assert.AreEqual(2, apiResource.Capacities.Count);
			var numberCapacitySetting = apiResource.Capacities.OfType<NumberCapacitySetting>().SingleOrDefault();
			Assert.IsNotNull(numberCapacitySetting);
			Assert.AreEqual(numberCapacity.Id, numberCapacitySetting.Id);
			Assert.AreEqual(10m, numberCapacitySetting.Value);

			var rangeCapacitySetting = apiResource.Capacities.OfType<RangeCapacitySetting>().SingleOrDefault();
			Assert.IsNotNull(rangeCapacitySetting);
			Assert.AreEqual(rangeCapacity.Id, rangeCapacitySetting.Id);
			Assert.AreEqual(10m, rangeCapacitySetting.MinValue);
			Assert.AreEqual(20m, rangeCapacitySetting.MaxValue);

			// Verify CORE resource
			coreResource = TestContext.ResourceManagerHelper.GetResource(coreResource.ID);
			Assert.IsNotNull(coreResource);

			Assert.AreEqual(2, coreResource.Capabilities.Count);
			Assert.AreEqual(2, coreResource.Capacities.Count);
		}
	}
}

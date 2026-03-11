namespace RT_MediaOps.Plan.RST.Resources
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	[TestCategory("IntegrationTest")]

	public sealed class VirtualFunctionResourceTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public VirtualFunctionResourceTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void HappyPathCrud_ElementLink()
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

			var functionResource = new VirtualFunctionResource()
			{
				Name = $"{prefix}_Resource",
				FunctionId = activeFunctionVersion.FunctionDefinitions.First().GUID,
				AgentId = elementId.AgentId,
				ElementId = elementId.ElementId,
			};

			var resource = objectCreator.CreateResource(functionResource);
			Assert.IsNotNull(resource);
			Assert.AreEqual(Guid.Empty, resource.CoreResourceId);
			Assert.IsTrue(resource is VirtualFunctionResource);

			TestContext.Api.Resources.Complete(resource.Id);
			resource = (VirtualFunctionResource)TestContext.Api.Resources.Read(functionResource.Id);
			Assert.IsTrue(resource.CoreResourceId != Guid.Empty);

			var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
			Assert.IsNotNull(coreResource);

			var coreFunctionResource = coreResource as Skyline.DataMiner.Net.ResourceManager.Objects.FunctionResource;
			Assert.IsNotNull(coreFunctionResource);
			Assert.AreEqual(activeFunctionVersion.FunctionDefinitions.First().GUID, coreFunctionResource.FunctionGUID);

			// todo: add more validation
		}
	}
}

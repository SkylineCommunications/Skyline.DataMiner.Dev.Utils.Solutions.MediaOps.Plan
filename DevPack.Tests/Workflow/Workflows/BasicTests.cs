namespace RT_MediaOps.Plan.Workflow.Workflows
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using SLDataGateway.API.Querying;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class BasicTests
	{
		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		[TestMethod]
		public void ReadAllWorkflows()
		{
			try
			{
				TestContext.Api.Workflows.Read().ToArray();
				return;
			}
			catch (Exception)
			{
				Assert.Fail();
			}
		}

		[TestMethod]
		public void ReadWorkflowById()
		{
			var firstWorkflow = TestContext.Api.Workflows.Read().First();
			var jobToVerify = TestContext.Api.Workflows.Read(firstWorkflow.Id);

			Assert.AreEqual(firstWorkflow, jobToVerify);
		}

		[TestMethod]
		public void ReadWorkflowByName()
		{
			var firstWorkflow = TestContext.Api.Workflows.Read().First();
			var jobToVerify = TestContext.Api.Workflows.Read(WorkflowExposers.Name.Equal(firstWorkflow.Name)).First();

			Assert.AreEqual(firstWorkflow, jobToVerify);
		}

		[TestMethod]
		public void ReadWithEmptyFilterReturnsEmptyList()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Workflow>(idsToRetrieve.Select(x => WorkflowExposers.Id.Equal(x)).ToArray());

			var workflows = TestContext.Api.Workflows.Read(emptyFilter);
			Assert.IsNotNull(workflows);
			Assert.AreEqual(0, workflows.Count());
		}

		[TestMethod]
		public void CountWithEmptyFilterReturnsZero()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Workflow>(idsToRetrieve.Select(x => WorkflowExposers.Id.Equal(x)).ToArray());

			var count = TestContext.Api.Workflows.Count(emptyFilter);
			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public void ReadWithEmptyQueryReturnsEmptyList()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Workflow>(idsToRetrieve.Select(x => WorkflowExposers.Id.Equal(x)).ToArray());
			var queryWithEmptyFilter = emptyFilter.ToQuery();

			var workflows = TestContext.Api.Workflows.Read(queryWithEmptyFilter);
			Assert.IsNotNull(workflows);
			Assert.AreEqual(0, workflows.Count());
		}
	}
}

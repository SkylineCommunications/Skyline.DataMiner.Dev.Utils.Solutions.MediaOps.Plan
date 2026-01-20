namespace RT_MediaOps.Plan.Workflow.Workflows
{
    using System;
    using System.Linq;
    using RT_MediaOps.Plan.RegressionTests;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class BasicTests
    {
        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        [TestMethod]
        public void ReadAllWorkflows()
        {
            Exception exception = null;

            try
            {
                TestContext.Api.Workflows.Read().ToArray();
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.IsNull(exception);
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
    }
}

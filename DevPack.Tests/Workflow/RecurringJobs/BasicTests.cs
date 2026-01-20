namespace RT_MediaOps.Plan.Workflow.RecurringJobs
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
        public void ReadAllRecurringJobs()
        {
            Exception exception = null;

            try
            {
                TestContext.Api.RecurringJobs.Read().ToArray();
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.IsNull(exception);
        }

        [TestMethod]
        public void ReadRecurringJobById()
        {
            var firstRecurringJob = TestContext.Api.RecurringJobs.Read().First();
            var jobToVerify = TestContext.Api.RecurringJobs.Read(firstRecurringJob.Id);

            Assert.AreEqual(firstRecurringJob, jobToVerify);
        }

        [TestMethod]
        public void ReadRecurringJobByName()
        {
            var firstRecurringJob = TestContext.Api.RecurringJobs.Read().First();
            var jobToVerify = TestContext.Api.RecurringJobs.Read(RecurringJobExposers.Name.Equal(firstRecurringJob.Name)).First();

            Assert.AreEqual(firstRecurringJob, jobToVerify);
        }
    }
}

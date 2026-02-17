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
            try
            {
                TestContext.Api.RecurringJobs.Read().ToArray();
                return;
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void ReadRecurringJobById()
        {
            var firstRecurringJob = TestContext.Api.RecurringJobs.Read().First();
            var jobToVerify = TestContext.Api.RecurringJobs.Read(firstRecurringJob.ID);

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

namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class JobPropertyManagementTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public JobPropertyManagementTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void CreateAndUpdateJob_WithCustomPropertiesOnJobAndNodes_PersistsAllProperties()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
			};
			job.AddCustomProperty(new CustomPropertySetting("Tag1") { Value = "Value1" });

			var node1 = new JobResourcePoolNode(pool);
			node1.AddCustomProperty(new CustomPropertySetting("Tag1") { Value = "Value1" });
			job.NodeGraph.Add(node1);

			job = objectCreator.CreateJob(job);
			Assert.IsNotNull(job);
			Assert.AreEqual(1, job.CustomPropertySettings.Count);
			var customJobProperty = job.CustomPropertySettings.First();
			Assert.AreEqual("Tag1", customJobProperty.Name);
			Assert.AreEqual("Value1", customJobProperty.Value);

			Assert.AreEqual(1, job.NodeGraph.Nodes.Count);
			foreach (var node in job.NodeGraph.Nodes)
			{
				Assert.AreEqual(1, node.CustomPropertySettings.Count);
				var customNodeProperty = node.CustomPropertySettings.First();
				Assert.AreEqual("Tag1", customNodeProperty.Name);
				Assert.AreEqual("Value1", customNodeProperty.Value);
			}

			// Update job and node properties
			job.AddCustomProperty(new CustomPropertySetting("Tag2") { Value = "Value2" });

			node1 = job.NodeGraph.Nodes.OfType<JobResourcePoolNode>().First();
			node1.AddCustomProperty(new CustomPropertySetting("Tag2") { Value = "Value2" });

			var node2 = new JobResourcePoolNode(pool);
			node2.AddCustomProperty(new CustomPropertySetting("Tag1") { Value = "Value1" });
			node2.AddCustomProperty(new CustomPropertySetting("Tag2") { Value = "Value2" });
			job.NodeGraph.Add(node2);

			job = TestContext.Api.Jobs.Update(job);
			Assert.IsNotNull(job);
			Assert.AreEqual(2, job.CustomPropertySettings.Count);
			var customJobTag1Property = job.CustomPropertySettings.FirstOrDefault(x => x.Name == "Tag1");
			Assert.IsNotNull(customJobTag1Property);
			Assert.AreEqual("Value1", customJobTag1Property.Value);
			var customJobTag2Property = job.CustomPropertySettings.FirstOrDefault(x => x.Name == "Tag2");
			Assert.IsNotNull(customJobTag2Property);
			Assert.AreEqual("Value2", customJobTag2Property.Value);

			Assert.AreEqual(2, job.NodeGraph.Nodes.Count);
			foreach (var node in job.NodeGraph.Nodes)
			{
				Assert.AreEqual(2, node.CustomPropertySettings.Count);
				var customNodeTag1Property = node.CustomPropertySettings.FirstOrDefault(x => x.Name == "Tag1");
				Assert.IsNotNull(customNodeTag1Property);
				Assert.AreEqual("Value1", customNodeTag1Property.Value);
				var customNodeTag2Property = node.CustomPropertySettings.FirstOrDefault(x => x.Name == "Tag2");
				Assert.IsNotNull(customNodeTag2Property);
				Assert.AreEqual("Value2", customNodeTag2Property.Value);
			}
		}
	}
}

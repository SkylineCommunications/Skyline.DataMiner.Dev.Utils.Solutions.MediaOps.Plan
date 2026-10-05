namespace RT_MediaOps.Plan.RST.Resources
{
	using System;
	using System.Linq;
	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class ZzProbeTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;
		public ZzProbeTests() { objectCreator = new TestObjectCreator(TestContext); }
		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;
		public void Dispose() { objectCreator.Dispose(); }

		private (Job A, Job B, ResourcePool Pool, NumberCapacity Cap, DateTime T) Setup()
		{
			var prefix = Guid.NewGuid();
			var t = DateTime.UtcNow.RoundToNextSecond();
			var pool = TestContext.Api.ResourcePools.Complete(objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" }));
			var capacity = new NumberCapacity { Name = $"{prefix}_Capacity", RangeMin = 0, RangeMax = 100 };
			objectCreator.CreateCapacity(capacity);
			var resource = new UnmanagedResource { Name = $"{prefix}_R", Concurrency = 2 }.AssignToPool(pool);
			resource.AddCapacity(new NumberCapacitySetting(capacity) { Value = 100 });
			resource = TestContext.Api.Resources.Complete(objectCreator.CreateResource(resource));
			Job Make(string n)
			{
				var j = new Job { Name = $"{prefix}_{n}", Start = t.AddHours(1), End = t.AddHours(2), PreRollStart = t.AddHours(1), PostRollEnd = t.AddHours(2) };
				var node = new JobResourceNode(pool, resource);
				node.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(capacity) { Value = 10 });
				j.NodeGraph.Add(node);
				return TestContext.Api.Jobs.SaveAsTentative(objectCreator.CreateJob(j));
			}
			var a = Make("A"); var b = Make("B");
			var core = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
			core.MaxConcurrency = 1;
			TestContext.ResourceManagerHelper.AddOrUpdateResources(true, [core]);
			return (a, b, pool, capacity, t);
		}

		private static ReservationInstance Res(Job j) => TestContext.ResourceManagerHelper.GetReservationInstances(ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(j.Id))).Single();

		[TestMethod]
		public void Probe_ChangeCapacityOnConcurrencyQuarantinedNode()
		{
			var s = Setup();
			var q = new[] { s.A, s.B }.Single(x => Res(x).IsQuarantined);
			q = TestContext.Api.Jobs.Read(q.Id);
			((NumberCapacitySetting)q.NodeGraph.Nodes.Single().OrchestrationSettings.Capacities.Single()).Value = 5;
			try { TestContext.Api.Jobs.Update(q); Console.WriteLine("UPDATE OK; quarantined=" + Res(q).IsQuarantined); }
			catch (Exception ex) { Console.WriteLine("UPDATE FAILED: " + ex.Message); }
			Assert.Fail("probe");
		}

		[TestMethod]
		public void Probe_MoveConcurrencyQuarantinedJobOutOfOverlap()
		{
			var s = Setup();
			var q = new[] { s.A, s.B }.Single(x => Res(x).IsQuarantined);
			q = TestContext.Api.Jobs.Read(q.Id);
			q.PostRollEnd = s.T.AddHours(6); q.End = s.T.AddHours(6); q.Start = s.T.AddHours(5); q.PreRollStart = s.T.AddHours(5);
			try { TestContext.Api.Jobs.Update(q); Console.WriteLine("UPDATE OK; quarantined=" + Res(q).IsQuarantined); }
			catch (Exception ex) { Console.WriteLine("UPDATE FAILED: " + ex.Message); }
			Assert.Fail("probe");
		}

		[TestMethod]
		public void Probe_RenameConcurrencyQuarantinedJob()
		{
			var s = Setup();
			var q = new[] { s.A, s.B }.Single(x => Res(x).IsQuarantined);
			q = TestContext.Api.Jobs.Read(q.Id);
			q.Name = q.Name + "_x";
			try { TestContext.Api.Jobs.Update(q); Console.WriteLine("UPDATE OK; quarantined=" + Res(q).IsQuarantined); }
			catch (Exception ex) { Console.WriteLine("UPDATE FAILED: " + ex.Message); }
			Assert.Fail("probe");
		}
	}
}

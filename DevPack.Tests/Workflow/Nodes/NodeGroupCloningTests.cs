namespace RT_MediaOps.Plan.Workflow.Nodes
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class NodeGroupCloningTests
	{
		private static readonly DateTimeOffset BaseStart = new DateTimeOffset(2024, 6, 1, 10, 0, 0, TimeSpan.Zero);
		private static readonly DateTimeOffset BaseEnd = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);

		[TestMethod]
		public void WorkflowDuplicate_CopiesGroupsAndPointsAtClonedNodes()
		{
			var workflow = new Workflow { Name = "Workflow" };
			var firstNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var secondNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(firstNode).Add(secondNode);

			workflow.NodeGraph.AddGroup("A").Add(firstNode);
			workflow.NodeGraph.AddGroup("B").Add(firstNode).Add(secondNode);

			var duplicate = workflow.Duplicate();

			Assert.AreEqual(2, duplicate.NodeGraph.Groups.Count);

			var groupA = duplicate.NodeGraph.Groups.Single(group => group.Name == "A");
			Assert.AreEqual(1, groupA.Nodes.Count);
			Assert.IsFalse(groupA.Nodes.Contains(firstNode));
			Assert.IsTrue(duplicate.NodeGraph.Nodes.Contains(groupA.Nodes.Single()));

			var groupB = duplicate.NodeGraph.Groups.Single(group => group.Name == "B");
			Assert.AreEqual(2, groupB.Nodes.Count);
			CollectionAssert.IsSubsetOf(groupB.Nodes.ToArray(), duplicate.NodeGraph.Nodes.ToArray());
		}

		[TestMethod]
		public void JobDuplicate_CopiesGroups()
		{
			var job = new Job { Name = "Job", Start = BaseStart, End = BaseEnd };
			var node = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			job.NodeGraph.Add(node);
			job.NodeGraph.AddGroup("Group").Add(node);

			var duplicate = job.Duplicate();

			var group = duplicate.NodeGraph.Groups.Single();
			Assert.AreEqual("Group", group.Name);
			Assert.AreSame(duplicate.NodeGraph.Nodes.Single(), group.Nodes.Single());
		}

		[TestMethod]
		public void RecurringJobDuplicate_CopiesGroups()
		{
			var recurringJob = RecurringJob.FromJob(CreateJobWithGroup(out _));
			var duplicate = recurringJob.Duplicate();

			var group = duplicate.NodeGraph.Groups.Single();
			Assert.AreEqual("Group", group.Name);
			Assert.AreSame(duplicate.NodeGraph.Nodes.Single(), group.Nodes.Single());
		}

		[TestMethod]
		public void RecurringJobFromJob_CopiesGroupsWithoutAddingAnExtraGroup()
		{
			var job = CreateJobWithGroup(out _);

			var recurringJob = RecurringJob.FromJob(job);

			var group = recurringJob.NodeGraph.Groups.Single();
			Assert.AreEqual("Group", group.Name);
			Assert.AreSame(recurringJob.NodeGraph.Nodes.Single(), group.Nodes.Single());
		}

		[TestMethod]
		public void JobFromRecurringJob_CopiesGroupsWithoutAddingAnExtraGroup()
		{
			var recurringJob = RecurringJob.FromJob(CreateJobWithGroup(out _));

			var job = Job.FromRecurringJob(recurringJob, BaseStart);

			var group = job.NodeGraph.Groups.Single();
			Assert.AreEqual("Group", group.Name);
			Assert.AreSame(job.NodeGraph.Nodes.Single(), group.Nodes.Single());
		}

		[TestMethod]
		public void Duplicate_GroupMembershipIsIndependentFromTheSource()
		{
			var job = CreateJobWithGroup(out var node);

			var duplicate = job.Duplicate();
			duplicate.NodeGraph.Groups.Single().Clear();

			Assert.AreSame(node, job.NodeGraph.Groups.Single().Nodes.Single());
		}

		private static Job CreateJobWithGroup(out JobNode node)
		{
			var job = new Job { Name = "Job", Start = BaseStart, End = BaseEnd };
			node = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			job.NodeGraph.Add(node);
			job.NodeGraph.AddGroup("Group").Add(node);

			return job;
		}
	}
}

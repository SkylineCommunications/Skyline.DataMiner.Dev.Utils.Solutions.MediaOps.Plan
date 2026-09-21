namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using StorageWorkflow = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	[TestClass]
	public class JobValidationResultTests
	{
		[TestMethod]
		public void SyncResultsToJob_ReplacesAndClearsOwnedErrorsWhilePreservingOtherErrors()
		{
			var job = new Job()
				.AddError(new JobError("J101", "old"))
				.AddError(new JobError("J501", "lifecycle"))
				.AddError(new JobError("LIV101", "leave"));
			var result = new JobValidationResult(job);
			result.SetError(new DomResourceNotFoundJobValidatorError(System.Guid.Empty, "node-1"));

			Assert.IsTrue(result.SyncResultsToJob());
			Assert.AreEqual(2, job.Errors.Count);
			Assert.AreEqual("leave", job.Errors.Single(error => error.Code == "LIV101").Message);
			Assert.AreEqual(result.Errors.Single().Message, job.Errors.Single(error => error.Code == "J101").Message);
		}

		[TestMethod]
		public void SyncResultsToJob_WhenAlreadySynchronized_ReturnsFalse()
		{
			var error = new UnresolvedReferencesJobValidatorError("Unresolved references: value.");
			var job = new Job().AddError(error);
			var result = new JobValidationResult(job);
			result.SetError(error);

			Assert.IsFalse(result.SyncResultsToJob());
		}

		[TestMethod]
		public void SyncResultsToJob_SynchronizesQuarantinedResourceNodes()
		{
			var quarantinedNode = new JobResourceNode(System.Guid.NewGuid(), System.Guid.NewGuid());
			var quarantinedByCoreIdNode = new JobResourceNode(System.Guid.NewGuid(), System.Guid.NewGuid());
			quarantinedByCoreIdNode.SetCoreReservationNodeId(42);
			var recoveredNode = new JobResourceNode(System.Guid.NewGuid(), System.Guid.NewGuid()) { HasError = true };
			var job = new Job();
			job.NodeGraph.Add(quarantinedNode);
			job.NodeGraph.Add(quarantinedByCoreIdNode);
			job.NodeGraph.Add(recoveredNode);
			var result = new JobValidationResult(job);
			result.AddQuarantinedNodeId(quarantinedNode.Id);
			result.AddQuarantinedNodeId("42");

			Assert.IsTrue(result.SyncResultsToJob());
			Assert.IsTrue(quarantinedNode.HasError);
			Assert.IsTrue(quarantinedByCoreIdNode.HasError);
			Assert.IsFalse(recoveredNode.HasError);
		}

		[TestMethod]
		public void JobResourceNode_HasError_RoundTripsResourceSelectState()
		{
			var node = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid()) { HasError = true };
			node.SetCoreReservationNodeId(1);

			var section = node.GetSectionWithChanges();

			Assert.AreEqual(
				StorageWorkflow.SlcWorkflowIds.Enums.Resourceselectstate.Error,
				section.ResourceSelectState);
			Assert.IsTrue(CreateStoredNode(StorageWorkflow.SlcWorkflowIds.Enums.Resourceselectstate.Error).HasError);
			Assert.IsFalse(CreateStoredNode(StorageWorkflow.SlcWorkflowIds.Enums.Resourceselectstate.Selected).HasError);
			Assert.IsFalse(CreateStoredNode(null).HasError);
		}

		private static JobResourceNode CreateStoredNode(StorageWorkflow.SlcWorkflowIds.Enums.Resourceselectstate? state)
		{
			var section = new StorageWorkflow.NodesSection
			{
				NodeID = Guid.NewGuid().ToString(),
				NodeStartTime = DateTime.UtcNow,
				NodeEndTime = DateTime.UtcNow.AddHours(1),
				CoreReservationNodeID = 1,
				ReferenceId = Guid.NewGuid(),
				ParentReferenceId = Guid.NewGuid(),
				ResourceSelectState = state,
			};

			return new JobResourceNode(null, section);
		}
	}
}
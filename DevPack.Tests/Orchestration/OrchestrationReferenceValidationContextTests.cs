namespace RT_MediaOps.Plan.Orchestration
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class OrchestrationReferenceValidationContextTests
	{
		[TestMethod]
		public void TryGetTarget_KnownId_ReturnsTargetAndFlag()
		{
			var id = Guid.NewGuid();
			var targets = new Dictionary<Guid, (ReferenceResolver Resolver, string OwningNodeId, bool ReportErrors)>
			{
				[id] = (null, "node-1", true),
			};

			var context = new OrchestrationReferenceValidationContext(targets);

			Assert.IsTrue(context.TryGetTarget(id, out var resolver, out var owningNodeId, out var reportErrors));
			Assert.IsNull(resolver);
			Assert.AreEqual("node-1", owningNodeId);
			Assert.IsTrue(reportErrors);
		}

		[TestMethod]
		public void TryGetTarget_UnknownId_ReturnsFalse()
		{
			var context = new OrchestrationReferenceValidationContext(
				new Dictionary<Guid, (ReferenceResolver Resolver, string OwningNodeId, bool ReportErrors)>());

			Assert.IsFalse(context.TryGetTarget(Guid.NewGuid(), out var resolver, out var owningNodeId, out var reportErrors));
			Assert.IsNull(resolver);
			Assert.IsNull(owningNodeId);
			Assert.IsFalse(reportErrors);
		}

		[TestMethod]
		public void Constructor_Null_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new OrchestrationReferenceValidationContext(null));
		}
	}
}

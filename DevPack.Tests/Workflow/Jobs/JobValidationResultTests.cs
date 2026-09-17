namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System.Linq;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public class JobValidationResultTests
	{
		[TestMethod]
		public void SyncResultsToInstance_ReplacesAndClearsOwnedErrorsWhilePreservingOtherErrors()
		{
			var job = new Job()
				.AddError(new JobError("J101", "old"))
				.AddError(new JobError("J501", "lifecycle"))
				.AddError(new JobError("LIV101", "leave"));
			var result = new JobValidationResult(job);
			result.SetError(new DomResourceNotFoundJobError(System.Guid.Empty, "node-1"));

			Assert.IsTrue(result.SyncResultsToInstance());
			Assert.AreEqual(2, job.Errors.Count);
			Assert.AreEqual("leave", job.Errors.Single(error => error.Code == "LIV101").Message);
			Assert.AreEqual(result.Errors.Single().Message, job.Errors.Single(error => error.Code == "J101").Message);
		}

		[TestMethod]
		public void SyncResultsToInstance_WhenAlreadySynchronized_ReturnsFalse()
		{
			var error = new UnresolvedReferencesJobError("Unresolved references: value.");
			var job = new Job().AddError(error);
			var result = new JobValidationResult(job);
			result.SetError(error);

			Assert.IsFalse(result.SyncResultsToInstance());
		}
	}
}
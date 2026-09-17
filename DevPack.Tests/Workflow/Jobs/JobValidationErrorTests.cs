namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public class JobValidationErrorTests
	{
		[TestMethod]
		public void ValidationErrors_ExposeExpectedCodes()
		{
			var errors = new JobError[]
			{
				new GenericJobValidationError(new InvalidOperationException("failure")),
				new DomResourceNotFoundJobError(Guid.Empty, "node"),
				new CoreResourceNotFoundJobError(Guid.Empty, "resource"),
				new DomResourceInvalidJobError("message"),
				new CoreResourceUnavailableJobError("resource", "Unavailable"),
				new QuarantinedReservationJobError("message"),
				new VirtualSignalGroupNotFoundJobError("input", Guid.Empty, "resource"),
				new TransitionToTentativeJobError("message"),
				new UnresolvedReferencesJobError("message"),
				new ReservationRequirementsMismatchJobError("message"),
				new LiveEventsMismatchJobError("message"),
			};

			CollectionAssert.AreEqual(
				new[] { "J000", "J101", "J102", "J103", "J104", "J105", "J201", "J501", "J601", "J602", "J603" },
				Array.ConvertAll(errors, error => error.Code));
		}

		[TestMethod]
		public void TypedErrors_FormatStoredMessages()
		{
			var resourceId = Guid.Parse("7d078529-a34b-47af-9043-04704b954858");

			Assert.AreEqual(
				$"Couldn't find DOM resource with ID '{resourceId}' for node node-1",
				new DomResourceNotFoundJobError(resourceId, "node-1").Message);
			Assert.AreEqual(
				"Resource 'Encoder' has state Unavailable",
				new CoreResourceUnavailableJobError("Encoder", "Unavailable").Message);
		}
	}
}
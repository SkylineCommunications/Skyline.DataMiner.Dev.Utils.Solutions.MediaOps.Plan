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
			var errors = new JobValidationError[]
			{
				new GenericJobValidationError(new InvalidOperationException("failure")),
				new DomResourceNotFoundJobValidationError(Guid.Empty, "node"),
				new CoreResourceNotFoundJobValidationError(Guid.Empty, "resource"),
				new DomResourceInvalidJobValidationError("message"),
				new CoreResourceUnavailableJobValidationError("resource", "Unavailable"),
				new QuarantinedReservationJobValidationError("message"),
				new VirtualSignalGroupNotFoundJobValidationError("input", Guid.Empty, "resource"),
				new TransitionToTentativeJobValidationError("message"),
				new UnresolvedReferencesJobValidationError("message"),
				new ReservationRequirementsMismatchJobValidationError("message"),
				new LiveEventsMismatchJobValidationError("message"),
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
				new DomResourceNotFoundJobValidationError(resourceId, "node-1").Message);
			Assert.AreEqual(
				"Resource 'Encoder' has state Unavailable",
				new CoreResourceUnavailableJobValidationError("Encoder", "Unavailable").Message);
		}
	}
}
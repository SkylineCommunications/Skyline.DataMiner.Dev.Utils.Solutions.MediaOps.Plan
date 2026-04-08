namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	internal class ParameterApiObjectValidator : ApiObjectValidator<Net.Profiles.Parameter>
	{
		private readonly List<Guid> successfulIds = new List<Guid>();

		internal override IReadOnlyCollection<Guid> SuccessfulIds => successfulIds;

		protected override void ReportSuccess(Net.Profiles.Parameter item)
		{
			if (unsuccessfulItems.Contains(item.ID))
			{
				throw new InvalidOperationException($"An item cannot be marked as both successful and unsuccessful");
			}

			successfulIds.Add(item.ID);
			successfulItems.Add(item);
		}
	}
}

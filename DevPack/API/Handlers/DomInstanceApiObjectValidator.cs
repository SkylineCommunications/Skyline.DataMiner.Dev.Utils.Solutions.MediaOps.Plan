namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	internal class DomInstanceApiObjectValidator<T> : ApiObjectValidator<T> where T : DomInstanceBase
	{
		private readonly List<Guid> successfulIds = new List<Guid>();

		internal override IReadOnlyCollection<Guid> SuccessfulIds => successfulIds;

		protected override void ReportSuccess(T item)
		{
			if (unsuccessfulItems.Contains(item.ID.Id))
			{
				throw new InvalidOperationException($"An item cannot be marked as both successful and unsuccessful");
			}

			successfulIds.Add(item.ID.Id);
			successfulItems.Add(item);
		}
	}
}

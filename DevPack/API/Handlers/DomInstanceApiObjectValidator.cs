namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

    internal class DomInstanceApiObjectValidator<T> : ApiObjectValidator<T> where T : DomInstanceBase
    {
        internal override IReadOnlyCollection<Guid> SuccessfulIds => successfulIItems.Select(x => x.ID.Id).ToList();

        protected override void ReportSuccess(T item)
        {
            if (unsuccessfulItems.Contains(item.ID.Id))
            {
                throw new InvalidOperationException($"An item cannot be marked as both successful and unsuccessful");
            }

            successfulIItems.Add(item);
        }
    }
}

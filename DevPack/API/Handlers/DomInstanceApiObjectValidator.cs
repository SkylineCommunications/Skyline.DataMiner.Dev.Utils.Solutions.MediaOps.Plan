namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Linq;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

    internal class DomInstanceApiObjectValidator : ApiObjectValidator<DomInstance>
    {
        protected override void ReportError(Guid key)
        {
            if (successfulIItems.Any(x => x.ID.Id == key))
            {
                throw new InvalidOperationException($"An item cannot be marked as both successful and unsuccessful");
            }

            unsuccessfulItems.Add(key);
        }

        protected override void ReportSuccess(DomInstance item)
        {
            if (unsuccessfulItems.Contains(item.ID.Id))
            {
                throw new InvalidOperationException($"An item cannot be marked as both successful and unsuccessful");
            }

            successfulIItems.Add(item);
        }
    }
}

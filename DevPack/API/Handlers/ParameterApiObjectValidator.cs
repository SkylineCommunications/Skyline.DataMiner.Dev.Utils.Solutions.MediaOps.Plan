namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class ParameterApiObjectValidator : ApiObjectValidator<Net.Profiles.Parameter>
    {
        internal override IReadOnlyCollection<Guid> SuccessfulIds => successfulIItems.Select(x => x.ID).ToList();

        protected override void ReportSuccess(Net.Profiles.Parameter item)
        {
            if (unsuccessfulItems.Contains(item.ID))
            {
                throw new InvalidOperationException($"An item cannot be marked as both successful and unsuccessful");
            }

            successfulIItems.Add(item);
        }
    }
}

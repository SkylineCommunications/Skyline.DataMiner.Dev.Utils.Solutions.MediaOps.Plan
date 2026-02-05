namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Linq;

    internal class ParameterApiObjectValidator : ApiObjectValidator<Net.Profiles.Parameter>
    {
        protected override void ReportError(Guid key)
        {
            if (successfulIItems.Any(x => x.ID == key))
            {
                throw new InvalidOperationException($"An item cannot be marked as both successful and unsuccessful");
            }

            unsuccessfulItems.Add(key);
        }

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

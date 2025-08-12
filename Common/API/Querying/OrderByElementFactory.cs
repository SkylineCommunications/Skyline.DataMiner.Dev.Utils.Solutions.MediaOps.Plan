namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    using SLDataGateway.API.Querying;
    using SLDataGateway.API.Types.Querying;

    internal static class OrderByElementFactory
    {
        public static IOrderByElement Create(FieldExposer exposer, SortOrder sortOrder, bool naturalSort = false)
        {
            if (exposer == null)
            {
                throw new ArgumentNullException(nameof(exposer));
            }

            return OrderByElement.Default
                .WithFieldExposer(exposer)
                .WithSortOrder(sortOrder)
                .WithNaturalSort(naturalSort);
        }
    }
}

namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.Profiles;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

    internal class CapacityFilterTranslator : ParameterFilterTranslator<Capacity>
    {
        private readonly Dictionary<string, Func<Comparer, object, FilterElement<Net.Profiles.Parameter>>> handlers = new Dictionary<string, Func<Comparer, object, FilterElement<Net.Profiles.Parameter>>>
        {
            [CapacityExposers.Id.fieldName] = HandleGuid,
            [CapacityExposers.Name.fieldName] = HandleName,
            [CapacityExposers.IsMandatory.fieldName] = HandleIsMandatory,
            [CapacityExposers.RangeMin.fieldName] = (comparer, value) => FilterElementFactory.Create(ParameterExposers.RangeMin, comparer, ((decimal?)value).HasValue ? Convert.ToDouble(value) : double.NaN),
            [CapacityExposers.RangeMax.fieldName] = (comparer, value) => FilterElementFactory.Create(ParameterExposers.RangeMax, comparer, ((decimal?)value).HasValue ? Convert.ToDouble(value) : double.NaN),
            [CapacityExposers.StepSize.fieldName] = (comparer, value) => FilterElementFactory.Create(ParameterExposers.Stepsize, comparer, ((decimal?)value).HasValue ? Convert.ToDouble(value) : double.NaN),
            [CapacityExposers.Decimals.fieldName] = (comparer, value) => FilterElementFactory.Create(ParameterExposers.Decimals, comparer, ((int?)value).HasValue ? Convert.ToInt32(value) : int.MaxValue),
        };

        protected override Dictionary<string, Func<Comparer, object, FilterElement<Net.Profiles.Parameter>>> Handlers => handlers;

        protected override FilterElement<Net.Profiles.Parameter> ParameterTypeFilter => ProfileProvider.AllCapacitiesFilter;
    }
}

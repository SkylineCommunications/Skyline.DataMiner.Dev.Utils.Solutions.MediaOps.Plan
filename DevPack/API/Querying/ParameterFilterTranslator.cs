namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying
{
    using System;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.Profiles;

    internal abstract class ParameterFilterTranslator<T> : FilterTranslator<T, Net.Profiles.Parameter> where T : ApiObject
    {
        protected static FilterElement<Net.Profiles.Parameter> HandleGuid(Comparer comparer, object value)
        {
            return FilterElementFactory.Create(ParameterExposers.ID, comparer, (Guid)value);
        }

        protected static FilterElement<Net.Profiles.Parameter> HandleName(Comparer comparer, object value)
        {
            return FilterElementFactory.Create(ParameterExposers.Name, comparer, (string)value);
        }

        protected static FilterElement<Net.Profiles.Parameter> HandleIsMandatory(Comparer comparer, object value)
        {
            return FilterElementFactory.Create(ParameterExposers.IsOptional, comparer, !(bool)value);
        }

        protected abstract FilterElement<Net.Profiles.Parameter> ParameterTypeFilter { get; }

        public override FilterElement<Net.Profiles.Parameter> Translate(FilterElement<T> filter)
        {
            return base.Translate(filter).AND(ParameterTypeFilter);
        }
    }
}

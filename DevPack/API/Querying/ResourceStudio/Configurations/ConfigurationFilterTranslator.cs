namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.Profiles;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;
    using static Skyline.DataMiner.Net.Profiles.Parameter;

    internal class ConfigurationFilterTranslator : ParameterFilterTranslator<Configuration>
    {
        private readonly Dictionary<string, Func<Comparer, object, FilterElement<Net.Profiles.Parameter>>> handlers = new Dictionary<string, Func<Comparer, object, FilterElement<Net.Profiles.Parameter>>>
        {
            [ConfigurationExposers.Id.fieldName] = HandleGuid,
            [ConfigurationExposers.Name.fieldName] = HandleName,
            [ConfigurationExposers.IsMandatory.fieldName] = HandleIsMandatory,
            [DiscreteTextConfigurationExposers.Discretes.fieldName] = (comparer, value) => FilterElementFactory.Create(ParameterExposers.Discretes, comparer, (List<string>)value).AND(ParameterExposers.Type.Equal((int)ParameterType.Discrete)),
            [DiscreteNumberConfigurationExposers.Discretes.fieldName] = (comparer, value) => FilterElementFactory.Create(ParameterExposers.Discretes, comparer, ((List<decimal>)value).Select(x => Convert.ToString(x, CultureInfo.InvariantCulture)).ToList()).AND(ParameterExposers.Type.Equal((int)ParameterType.Discrete)),
        };

        protected override Dictionary<string, Func<Comparer, object, FilterElement<Net.Profiles.Parameter>>> Handlers => handlers;

        protected override FilterElement<Net.Profiles.Parameter> ParameterTypeFilter => ProfileProvider.AllConfigurationsFilter;
    }
}

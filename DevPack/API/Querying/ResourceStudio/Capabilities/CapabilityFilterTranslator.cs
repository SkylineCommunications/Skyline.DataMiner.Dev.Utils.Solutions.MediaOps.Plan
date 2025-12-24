namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.Profiles;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;
    using static Skyline.DataMiner.Net.Profiles.Parameter;

    internal class CapabilityFilterTranslator : ParameterFilterTranslator<Capability>
    {
        private readonly Dictionary<string, Func<Comparer, object, FilterElement<Net.Profiles.Parameter>>> handlers = new Dictionary<string, Func<Comparer, object, FilterElement<Net.Profiles.Parameter>>>
        {
            [CapabilityExposers.Id.fieldName] = HandleGuid,
            [CapabilityExposers.Name.fieldName] = HandleName,
            [CapabilityExposers.IsMandatory.fieldName] = HandleIsMandatory,
            [CapabilityExposers.Discretes.fieldName] = (comparer, value) => FilterElementFactory.Create(ParameterExposers.Discretes, comparer, (string)value).AND(ParameterExposers.Type.Equal((int)ParameterType.Discrete)),
            [CapabilityExposers.IsTimeDependent.fieldName] = (comparer, value) => IsTimeDependantFilter(comparer, (bool)value),
        };

        protected override Dictionary<string, Func<Comparer, object, FilterElement<Net.Profiles.Parameter>>> Handlers => handlers;

        protected override FilterElement<Net.Profiles.Parameter> ParameterTypeFilter => ProfileProvider.AllCapabilitiesFilter;

        private static FilterElement<Net.Profiles.Parameter> IsTimeDependantFilter(Comparer comparer, bool value)
        {
            bool isTimeDependentCheck;
            switch (comparer)
            {
                case Comparer.Equals:
                    isTimeDependentCheck = value;
                    break;
                case Comparer.NotEquals:
                    isTimeDependentCheck = !value;
                    break;
                default:
                    throw new NotSupportedException($"Comparer {comparer} is not supported for boolean TimeDependency checks");
            }

            if (isTimeDependentCheck)
            {
                return Net.Profiles.ParameterExposers.Remarks.Contains("\"isTimeDependent\":true");
            }
            else
            {
                return Net.Profiles.ParameterExposers.Remarks.NotContains("\"isTimeDependent\":true");
            }
        }
    }
}

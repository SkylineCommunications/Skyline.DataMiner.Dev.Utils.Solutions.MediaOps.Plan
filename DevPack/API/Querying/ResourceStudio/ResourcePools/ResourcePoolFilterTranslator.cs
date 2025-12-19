namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

    internal class ResourcePoolPoolFilterTranslator : DomInstanceFilterTranslator<ResourcePool>
    {
        private readonly FilterElement<DomInstance> ResourcePoolDomDefinitionFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resourcepool.Id);
        private readonly Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> handlers = new Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>>
        {
            [ResourcePoolExposers.Id.fieldName] = HandleGuid,
            [ResourcePoolExposers.Name.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourcePoolInfo.Name), comparer, (string)value),
            [ResourcePoolExposers.State.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.StatusId, comparer, ConvertResourcePoolState((ResourcePoolState)value)),
            [ResourcePoolExposers.IconImage.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourcePoolOther.IconImage), comparer, (string)value),
            [ResourcePoolExposers.Url.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourcePoolOther.URL), comparer, (string)value),
            [ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourcePoolLinks.LinkedResourcePool), comparer, Convert.ToString(value)),
            [ResourcePoolExposers.LinkedResourcePools.SelectionType.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourcePoolLinks.ResourceSelectionType), comparer, ConvertResourceSelectionType((ResourceSelectionType)value)),
            [ResourcePoolExposers.Capabilities.CapabilityId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourcePoolCapabilities.ProfileParameterID), comparer, Convert.ToString(value)),
            [ResourcePoolExposers.Capabilities.Discretes.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourcePoolCapabilities.StringValue), comparer, Convert.ToString(value)),
        };

        protected override Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> Handlers => handlers;

        protected override FilterElement<DomInstance> DomDefinitionFilter => ResourcePoolDomDefinitionFilter;

        private static string ConvertResourcePoolState(ResourcePoolState filterValue)
        {
            switch (filterValue)
            {
                case ResourcePoolState.Draft:
                    return SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Statuses.Draft;
                case ResourcePoolState.Complete:
                    return SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Statuses.Complete;
                case ResourcePoolState.Deprecated:
                    return SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Statuses.Deprecated;
                default:
                    throw new InvalidOperationException($"Unsupported ResourcePool state: {filterValue}");
            }
        }

        private static int ConvertResourceSelectionType(ResourceSelectionType filterValue)
        {
            return (int)filterValue.MapEnum<ResourceSelectionType, SlcResource_StudioIds.Enums.Resourceselectiontype>();
        }
    }
}

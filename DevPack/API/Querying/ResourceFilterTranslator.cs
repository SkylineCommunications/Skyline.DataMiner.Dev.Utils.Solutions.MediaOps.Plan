namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

    internal class ResourceFilterTranslator : DomInstanceFilterTranslator<Resource>
    {
        private readonly FilterElement<DomInstance> resourceDomDefinitionFilter = DomInstanceExposers.DomDefinitionId.Equal(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Definitions.Resource.Id);
        private readonly Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> handlers = new Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>>
        {
            [ResourceExposers.Id.fieldName] = HandleGuid,
            [ResourceExposers.Name.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Name), comparer, (string)value),
            [ResourceExposers.IsFavorite.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Favorite), comparer, (bool)value),
            [ResourceExposers.Concurrency.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Concurrency), comparer, (int)value),
            [ResourceExposers.State.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.StatusId, comparer, ConvertResourceState((int)value)),
            [ResourceExposers.AssignedResourcePoolIds.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.Pool_Ids), comparer, Convert.ToString(value)),
            [ResourceExposers.Capabilities.Id.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceCapabilities.ProfileParameterID), comparer, Convert.ToString(value)),
            [ResourceExposers.Capabilities.Discretes.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceCapabilities.StringValue), comparer, Convert.ToString(value)),
            [ResourceExposers.Capacities.Id.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceCapacities.ProfileParameterID), comparer, Convert.ToString(value)),
            [ResourceExposers.Properties.Id.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceProperties.Property), comparer, Convert.ToString(value)),
            [ResourceExposers.Properties.Value.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceProperties.PropertyValue), comparer, Convert.ToString(value)),
        };

        protected override Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> Handlers => handlers;

        protected override FilterElement<DomInstance> DomDefinitionFilter => resourceDomDefinitionFilter;

        private static string ConvertResourceState(int filterValue)
        {
            ResourceState state = (ResourceState)filterValue;
            switch (state)
            {
                case ResourceState.Draft:
                    return SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Draft;
                case ResourceState.Complete:
                    return SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Complete;
                case ResourceState.Deprecated:
                    return SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Deprecated;
                default:
                    throw new InvalidOperationException($"Unsupported resource state: {state}");
            }
        }
    }
}

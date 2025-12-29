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
        private readonly FilterElement<DomInstance> resourceDomDefinitionFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id);
        private readonly Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> handlers = new Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>>
        {
            [ResourceExposers.Id.fieldName] = HandleGuid,
            [ResourceExposers.Name.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Name), comparer, (string)value),
            [ResourceExposers.IsFavorite.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Favorite), comparer, (bool)value),
            [ResourceExposers.Concurrency.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Concurrency), comparer, (int)value),
            [ResourceExposers.State.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.StatusId, comparer, ConvertResourceState((ResourceState)value)),
            [ResourceExposers.ResourcePoolIds.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.Pool_Ids), comparer, Convert.ToString(value)),
            [ResourceExposers.Capabilities.CapabilityId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceCapabilities.ProfileParameterID), comparer, Convert.ToString(value)),
            [ResourceExposers.Capabilities.Discretes.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceCapabilities.StringValue), comparer, Convert.ToString(value)),
            [ResourceExposers.Capacities.CapacityId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceCapacities.ProfileParameterID), comparer, Convert.ToString(value)),
            [ResourceExposers.Properties.PropertyId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceProperties.Property), comparer, Convert.ToString(value)),
            [ResourceExposers.Properties.Value.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceProperties.PropertyValue), comparer, Convert.ToString(value)),
            [ServiceResourceExposers.AgentId.fieldName] = (comparer, value) => CreateAgentIdFilter(comparer, (int)value).AND(FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Type), Comparer.Equals, (int)SlcResource_StudioIds.Enums.Type.Service)),
            [ServiceResourceExposers.ServiceId.fieldName] = (comparer, value) => CreateElementOrServiceIdFilter(comparer, (int)value).AND(FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Type), Comparer.Equals, (int)SlcResource_StudioIds.Enums.Type.Service)),
            [ElementResourceExposers.AgentId.fieldName] = (comparer, value) => CreateAgentIdFilter(comparer, (int)value).AND(FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Type), Comparer.Equals, (int)SlcResource_StudioIds.Enums.Type.Element)),
            [ElementResourceExposers.ElementId.fieldName] = (comparer, value) => CreateElementOrServiceIdFilter(comparer, (int)value).AND(FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Type), Comparer.Equals, (int)SlcResource_StudioIds.Enums.Type.Element)),
            [VirtualFunctionResourceExposers.AgentId.fieldName] = (comparer, value) => CreateAgentIdFilter(comparer, (int)value).AND(FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Type), Comparer.Equals, (int)SlcResource_StudioIds.Enums.Type.VirtualFunction)),
            [VirtualFunctionResourceExposers.ElementId.fieldName] = (comparer, value) => CreateElementOrServiceIdFilter(comparer, (int)value).AND(FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Type), Comparer.Equals, (int)SlcResource_StudioIds.Enums.Type.VirtualFunction)),
            [VirtualFunctionResourceExposers.FunctionId.fieldName] = (comparer, value) => CreateFunctionIdFilter(comparer, (Guid)value).AND(FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Type), Comparer.Equals, (int)SlcResource_StudioIds.Enums.Type.VirtualFunction)),
            [VirtualFunctionResourceExposers.FunctionTableIndex.fieldName] = (comparer, value) => CreateFunctionTableIndexFilter(comparer, Convert.ToString(value)).AND(FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Type), Comparer.Equals, (int)SlcResource_StudioIds.Enums.Type.VirtualFunction)),
        };

        protected override Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> Handlers => handlers;

        protected override FilterElement<DomInstance> DomDefinitionFilter => resourceDomDefinitionFilter;

        private static string ConvertResourceState(ResourceState filterValue)
        {
            switch (filterValue)
            {
                case ResourceState.Draft:
                    return SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Draft;
                case ResourceState.Complete:
                    return SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Complete;
                case ResourceState.Deprecated:
                    return SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Deprecated;
                default:
                    throw new InvalidOperationException($"Unsupported resource state: {filterValue}");
            }
        }

        private static FilterElement<DomInstance> CreateAgentIdFilter(Comparer comparer, int agentId)
        {
            // Service: {"LinkedElementInfo":null,"LinkedServiceInfo":"78/140467","LinkedFunctionId":"00000000-0000-0000-0000-000000000000","LinkedFunctionTableIndex":null}
            // Element: {"LinkedElementInfo":"78/137485","LinkedServiceInfo":null,"LinkedFunctionId":"00000000-0000-0000-0000-000000000000","LinkedFunctionTableIndex":null}
            // VF:      {"LinkedElementInfo":"78/140461","LinkedServiceInfo":null,"LinkedFunctionId":"7bd8d399-b503-4fd9-9b2e-8dc188d591b8","LinkedFunctionTableIndex":"1"}
            switch (comparer)
            {
                case Comparer.Equals:
                    return CreateResourceMetaDataFilter(Comparer.Equals, $"\"LinkedServiceInfo\":\"{agentId}/").OR(CreateResourceMetaDataFilter(Comparer.Equals, $"\"LinkedElementInfo\":\"{agentId}/"));
                case Comparer.NotEquals:
                    return CreateResourceMetaDataFilter(Comparer.NotEquals, $"\"LinkedServiceInfo\":\"{agentId}/").AND(CreateResourceMetaDataFilter(Comparer.NotEquals, $"\"LinkedElementInfo\":\"{agentId}/"));
                default:
                    throw new NotSupportedException("Comparer {comparer} is not supported for AgentId checks");
            }
        }

        private static FilterElement<DomInstance> CreateElementOrServiceIdFilter(Comparer comparer, int elementOrServiceId)
        {
            // Service: {"LinkedElementInfo":null,"LinkedServiceInfo":"78/140467","LinkedFunctionId":"00000000-0000-0000-0000-000000000000","LinkedFunctionTableIndex":null}
            // Element: {"LinkedElementInfo":"78/137485","LinkedServiceInfo":null,"LinkedFunctionId":"00000000-0000-0000-0000-000000000000","LinkedFunctionTableIndex":null}
            // VF:      {"LinkedElementInfo":"78/140461","LinkedServiceInfo":null,"LinkedFunctionId":"7bd8d399-b503-4fd9-9b2e-8dc188d591b8","LinkedFunctionTableIndex":"1"}
            return CreateResourceMetaDataFilter(comparer, $"/{elementOrServiceId}\"");
        }

        private static FilterElement<DomInstance> CreateFunctionIdFilter(Comparer comparer, Guid functionId)
        {
            // VF:      {"LinkedElementInfo":"78/140461","LinkedServiceInfo":null,"LinkedFunctionId":"7bd8d399-b503-4fd9-9b2e-8dc188d591b8","LinkedFunctionTableIndex":"1"}
            return CreateResourceMetaDataFilter(comparer, $"\"LinkedFunctionId\":\"{functionId}\"");
        }

        private static FilterElement<DomInstance> CreateFunctionTableIndexFilter(Comparer comparer, string functionTableIndex)
        {
            // VF:      {"LinkedElementInfo":"78/140461","LinkedServiceInfo":null,"LinkedFunctionId":"7bd8d399-b503-4fd9-9b2e-8dc188d591b8","LinkedFunctionTableIndex":"1"}
            return CreateResourceMetaDataFilter(comparer, $"\"LinkedFunctionTableIndex\":\"{functionTableIndex}\"");
        }

        private static FilterElement<DomInstance> CreateResourceMetaDataFilter(Comparer comparer, string filterValue)
        {
            bool checkContains;
            switch (comparer)
            {
                case Comparer.Equals:
                    checkContains = true;
                    break;
                case Comparer.NotEquals:
                    checkContains = false;
                    break;
                default:
                    throw new NotSupportedException($"Comparer {comparer} is not supported for ResourceMetaData checks");
            }

            if (checkContains)
            {
                return DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.ResourceMetadata).Contains(filterValue);
            }
            else
            {
                return DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.ResourceMetadata).NotContains(filterValue);
            }
        }
    }
}

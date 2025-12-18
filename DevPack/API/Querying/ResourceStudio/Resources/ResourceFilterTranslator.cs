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
            [ResourceExposers.AssignedResourcePoolIds.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.Pool_Ids), comparer, Convert.ToString(value)),
            [ResourceExposers.Capabilities.Id.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceCapabilities.ProfileParameterID), comparer, Convert.ToString(value)),
            [ResourceExposers.Capabilities.Discretes.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceCapabilities.StringValue), comparer, Convert.ToString(value)),
            [ResourceExposers.Capacities.Id.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceCapacities.ProfileParameterID), comparer, Convert.ToString(value)),
            [ResourceExposers.Properties.Id.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceProperties.Property), comparer, Convert.ToString(value)),
            [ResourceExposers.Properties.Value.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceProperties.PropertyValue), comparer, Convert.ToString(value)),
            [ServiceResourceExposers.ServiceId.fieldName] = (comparer, value) => CreateServiceIdFilter(comparer, Convert.ToString(value)).AND(FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Type), Comparer.Equals, (int)SlcResource_StudioIds.Enums.Type.Service)),
            [ElementResourceExposers.ElementId.fieldName] = (comparer, value) => CreateElementIdFilter(comparer, Convert.ToString(value)).AND(FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Type), Comparer.Equals, (int)SlcResource_StudioIds.Enums.Type.Element)),
            [VirtualFunctionResourceExposers.ElementId.fieldName] = (comparer, value) => CreateElementIdFilter(comparer, Convert.ToString(value)).AND(FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Type), Comparer.Equals, (int)SlcResource_StudioIds.Enums.Type.VirtualFunction)),
            [VirtualFunctionResourceExposers.FunctionId.fieldName] = (comparer, value) => CreateFunctionIdFilter(comparer, Convert.ToString(value)).AND(FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Type), Comparer.Equals, (int)SlcResource_StudioIds.Enums.Type.VirtualFunction)),
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

        private static FilterElement<DomInstance> CreateServiceIdFilter(Comparer comparer, string serviceId)
        {
            bool checkContains;
            switch (comparer)
            {
                case Comparer.Equals:
                case Comparer.Contains:
                    checkContains = true;
                    break;
                case Comparer.NotEquals:
                case Comparer.NotContains:
                    checkContains = false;
                    break;
                default:
                    throw new NotSupportedException($"Comparer {comparer} is not supported for ServiceId checks");
            }

            if (checkContains)
            {
                return DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.ResourceMetadata).Contains($"\"LinkedServiceInfo\":\"{serviceId}\"");
            }
            else
            {
                return DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.ResourceMetadata).NotContains($"\"LinkedServiceInfo\":\"{serviceId}\"");
            }
        }

        private static FilterElement<DomInstance> CreateElementIdFilter(Comparer comparer, string elementId)
        {
            bool checkContains;
            switch (comparer)
            {
                case Comparer.Equals:
                case Comparer.Contains:
                    checkContains = true;
                    break;
                case Comparer.NotEquals:
                case Comparer.NotContains:
                    checkContains = false;
                    break;
                default:
                    throw new NotSupportedException($"Comparer {comparer} is not supported for ElementId checks");
            }

            if (checkContains)
            {
                return DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.ResourceMetadata).Contains($"\"LinkedElementInfo\":\"{elementId}\"");
            }
            else
            {
                return DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.ResourceMetadata).NotContains($"\"LinkedElementInfo\":\"{elementId}\"");
            }
        }

        private static FilterElement<DomInstance> CreateFunctionIdFilter(Comparer comparer, string functionId)
        {
            bool checkContains;
            switch (comparer)
            {
                case Comparer.Equals:
                case Comparer.Contains:
                    checkContains = true;
                    break;
                case Comparer.NotEquals:
                case Comparer.NotContains:
                    checkContains = false;
                    break;
                default:
                    throw new NotSupportedException($"Comparer {comparer} is not supported for FunctionId checks");
            }

            if (checkContains)
            {
                return DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.ResourceMetadata).Contains($"\"LinkedFunctionId\":\"{functionId}\"");
            }
            else
            {
                return DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.ResourceMetadata).NotContains($"\"LinkedFunctionId\":\"{functionId}\"");
            }
        }

        private static FilterElement<DomInstance> CreateFunctionTableIndexFilter(Comparer comparer, string functionTableIndex)
        {
            bool checkContains;
            switch (comparer)
            {
                case Comparer.Equals:
                case Comparer.Contains:
                    checkContains = true;
                    break;
                case Comparer.NotEquals:
                case Comparer.NotContains:
                    checkContains = false;
                    break;
                default:
                    throw new NotSupportedException($"Comparer {comparer} is not supported for FunctionTableIndex checks");
            }

            if (checkContains)
            {
                return DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.ResourceMetadata).Contains($"\"LinkedFunctionTableIndex\":\"{functionTableIndex}\"");
            }
            else
            {
                return DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.ResourceMetadata).NotContains($"\"LinkedFunctionTableIndex\":\"{functionTableIndex}\"");
            }
        }
    }
}

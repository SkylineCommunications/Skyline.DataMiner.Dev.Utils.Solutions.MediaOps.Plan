namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.Profiles;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

    internal class ResourceFilterTranslator : DomFilterTranslator
    {
        private static readonly Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> Handlers = new Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>>
        {
            /*RESOURCES*/
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

            //[ServiceExposers.Guid.fieldName] = HandleGuid,
            //[ServiceExposers.ServiceName.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceInfo.ServiceName), comparer, (string)value),
            //[ServiceExposers.Description.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceInfo.Description), comparer, (string)value),
            //[ServiceExposers.ServiceStartTime.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceInfo.ServiceStartTime), comparer, (DateTime)value),
            //[ServiceExposers.ServiceEndTime.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceInfo.ServiceEndTime), comparer, (DateTime)value),
            //[ServiceExposers.Icon.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceInfo.Icon), comparer, (string)value),
            //[ServiceExposers.ServiceSpecifcation.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceInfo.ServiceSpecifcation), comparer, (Guid)value),
            //[ServiceExposers.RelatedOrganization.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceInfo.RelatedOrganization), comparer, (Guid)value),
            //[ServiceExposers.ServiceCategory.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceInfo.ServiceCategory), comparer, (value as Models.ServiceCategory)?.ID ?? Guid.Empty),
            //[ServiceExposers.ServiceConfigurationParameters.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceInfo.ServiceConfigurationParameters), comparer, (value as Models.ServiceConfigurationValue)?.ID ?? Guid.Empty),
            //[ServiceExposers.ServiceID.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceInfo.ServiceID), comparer, (string)value),
            //[ServiceExposers.ServiceItemsExposers.Label.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceItems.Label), comparer, (string)value),
            //[ServiceExposers.ServiceItemsExposers.ServiceItemID.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceItems.ServiceItemID), comparer, (long)value),
            //[ServiceExposers.ServiceItemsExposers.ServiceItemType.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceItems.ServiceItemType), comparer, (int)value),
            //[ServiceExposers.ServiceItemsExposers.DefinitionReference.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceItems.DefinitionReference), comparer, (string)value),
            //[ServiceExposers.ServiceItemsExposers.ServiceItemScript.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceItems.ServiceItemScript), comparer, (string)value),
            //[ServiceExposers.ServiceItemsExposers.ImplementationReference.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceItems.ImplementationReference), comparer, (string)value),
            //[ServiceExposers.ServiceItemRelationshipsExposers.Type.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceItemRelationship.Type), comparer, (string)value),
            //[ServiceExposers.ServiceItemRelationshipsExposers.ParentServiceItem.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceItemRelationship.ParentServiceItem), comparer, (string)value),
            //[ServiceExposers.ServiceItemRelationshipsExposers.ChildServiceItem.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceItemRelationship.ChildServiceItem), comparer, (string)value),
            //[ServiceExposers.ServiceItemRelationshipsExposers.ParentServiceItemInterfaceID.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceItemRelationship.ParentServiceItemInterfaceID), comparer, (string)value),
            //[ServiceExposers.ServiceItemRelationshipsExposers.ChildServiceItemInterfaceID.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcServicemanagementIds.Sections.ServiceItemRelationship.ChildServiceItemInterfaceID), comparer, (string)value),
        };

        private ResourceFilterTranslator()
        {
        }

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

        /// <summary>
        /// Translates a filter element of type <typeparamref name="T"/> into a filter element for <see cref="DomInstance"/>.
        /// </summary>
        /// <typeparam name="T">The type of the filter element to translate. Must be a class.</typeparam>
        /// <param name="filter">The filter element to translate.</param>
        /// <returns>A <see cref="FilterElement{DomInstance}"/> representing the translated filter.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filter"/> is null.</exception>
        /// <exception cref="NotSupportedException">Thrown when the filter type is not supported.</exception>
        public static FilterElement<DomInstance> TranslateFullFilter<T>(FilterElement<T> filter) where T : class
        {
            if (filter is null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            FilterElement<DomInstance> translated;
            if (filter is ANDFilterElement<T> and)
            {
                translated = new ANDFilterElement<DomInstance>(and.subFilters.Select(TranslateFullFilter).ToArray());
            }
            else if (filter is ORFilterElement<T> or)
            {
                translated = new ORFilterElement<DomInstance>(or.subFilters.Select(TranslateFullFilter).ToArray());
            }
            else if (filter is NOTFilterElement<T> not)
            {
                translated = new NOTFilterElement<DomInstance>(TranslateFullFilter(not));
            }
            else if (filter is TRUEFilterElement<T>)
            {
                translated = new TRUEFilterElement<DomInstance>();
            }
            else if (filter is FALSEFilterElement<T>)
            {
                translated = new FALSEFilterElement<DomInstance>();
            }
            else if (filter is ManagedFilterIdentifier managedFilter)
            {
                translated = TranslateFilter(managedFilter);
            }
            else
            {
                throw new NotSupportedException($"Unsupported filter: {filter}");
            }

            return translated;
        }

        private static FilterElement<DomInstance> TranslateFilter(ManagedFilterIdentifier managedFilter)
        {
            if (managedFilter is null)
            {
                throw new ArgumentNullException(nameof(managedFilter));
            }

            var fieldName = managedFilter.getFieldName().fieldName;
            var comparer = managedFilter.getComparer();
            var value = managedFilter.getValue();
            var translated = CreateFilter(fieldName, comparer, value);
            return translated;
        }

        private static FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
        {
            if (!Handlers.ContainsKey(fieldName))
            {
                throw new NotSupportedException(fieldName);
            }

            return Handlers[fieldName].Invoke(comparer, value);
        }
    }
}

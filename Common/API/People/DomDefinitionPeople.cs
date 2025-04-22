namespace Skyline.DataMiner.MediaOps.API.Common.API.People
{
    using System.Collections.Generic;
    using System;

    using DomHelpers.SlcPeople_Organizations;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    using SLDataGateway.API.Types.Querying;
    using Skyline.DataMiner.MediaOps.API.Common.Storage.DOM;

    internal class DomDefinitionPeople : DomDefinitionBase<IPerson>
    {
        public DomDefinitionPeople(DomHelper helper)
            : base(helper)
        {
        }

        protected override IPerson CreateInstance(DomInstance domInstance)
        {
            return new DomPerson(domInstance);
        }

        protected internal override DomDefinitionId DomDefinition => SlcPeople_OrganizationsIds.Definitions.People;

        protected internal override FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
        {
            switch (fieldName)
            {
                case nameof(DomPerson.Name):
                    return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcPeople_OrganizationsIds.Sections.PeopleInformation.FullName), comparer, (string)value);
                case nameof(DomPerson.Email):
                    return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcPeople_OrganizationsIds.Sections.ContactInfo.Email), comparer, (string)value);
                case nameof(DomPerson.PhoneNumber):
                    return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcPeople_OrganizationsIds.Sections.ContactInfo.Phone), comparer, (string)value);
            }

            return base.CreateFilter(fieldName, comparer, value);
        }

        protected internal override IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
        {
            switch (fieldName)
            {
                case nameof(DomPerson.Name):
                    return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcPeople_OrganizationsIds.Sections.PeopleInformation.FullName), sortOrder, naturalSort);
                case nameof(DomPerson.Email):
                    return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcPeople_OrganizationsIds.Sections.ContactInfo.Email), sortOrder, naturalSort);
                case nameof(DomPerson.PhoneNumber):
                    return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcPeople_OrganizationsIds.Sections.ContactInfo.Phone), sortOrder, naturalSort);
            }

            return base.CreateOrderBy(fieldName, sortOrder, naturalSort);
        }

        public IEnumerable<PeopleInstance> GetAllPeople()
        {
            var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcPeople_OrganizationsIds.Definitions.People.Id);

            return GetPeopleIterator(filter);
        }

        public IEnumerable<PeopleInstance> GetPeople(FilterElement<DomInstance> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            // TODO: Are we sure the filter will limit to the one definition?
            return GetPeopleIterator(filter);
        }

        private IEnumerable<PeopleInstance> GetPeopleIterator(FilterElement<DomInstance> filter)
        {
            return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, x => new PeopleInstance(x));
        }
    }
}

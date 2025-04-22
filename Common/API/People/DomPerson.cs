namespace Skyline.DataMiner.MediaOps.API.Common.API.People
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using DomHelpers;
    using DomHelpers.SlcPeople_Organizations;

    using Skyline.DataMiner.MediaOps.API.Common.API.Teams;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

    internal class DomPerson : IPerson
    {
        private readonly PeopleInstance _domInstance;

        public DomPerson(DomInstance domInstance)
        {
            if (domInstance is null)
            {
                throw new ArgumentNullException(nameof(domInstance));
            }

            _domInstance = new PeopleInstance(domInstance);
        }

        public string Name => _domInstance.PeopleInformation.FullName;

        public string Email => _domInstance.ContactInfo.Email;

        public string PhoneNumber => _domInstance.ContactInfo.Phone;

        public IEnumerable<ITeamMember> Membership => throw new NotImplementedException();

        public Guid Id => _domInstance.ID.Id;

        public override string ToString()
        {
            return $"Person '{Name}' ({Id})";
        }
    }
}

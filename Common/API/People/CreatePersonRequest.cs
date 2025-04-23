namespace Skyline.DataMiner.MediaOps.API.Common.API.People
{
    using System;
    using DomHelpers.SlcPeople_Organizations;

    using Skyline.DataMiner.MediaOps.API.Common.API.Generic;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

    internal class CreatePersonRequest : IRequest
    {
        public Guid RequestId { get; set; } = Guid.NewGuid();

        public Guid ObjectId { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        internal DomInstance ToInstance()
        {
            var person = Guid.Empty.Equals(ObjectId) ? new PeopleInstance() : new PeopleInstance(ObjectId);
            person.ContactInfo.Email = Email;
            person.ContactInfo.Phone = PhoneNumber;
            person.PeopleInformation.FullName = Name;
            return person;
        }
    }
}
namespace Skyline.DataMiner.MediaOps.API.Common.API.People
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DomHelpers.SlcPeople_Organizations;
    using Skyline.DataMiner.MediaOps.API.Common.API.Generic;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    public interface IPeopleApi : IApiQueryable<Person>, ICrudApi<Person>
    {

    }

    public class PeopleApi : IPeopleApi
    {
        private readonly MediaOpsPlanApi _helper;
        private readonly DomHelper _pnoHelper;

        internal DomDefinitionPeople DomDefinitionPeople { get; }

        public PeopleApi(MediaOpsPlanApi api)
        {
            _helper = api ?? throw new ArgumentNullException(nameof(api));
            _pnoHelper = new DomHelper(api.Communication.Connection.HandleMessages, SlcPeople_OrganizationsIds.ModuleId);
            DomDefinitionPeople = new DomDefinitionPeople(_pnoHelper);
        }

        public IOrderedQueryable<Person> Query()
        {
            return DomDefinitionPeople.Query();
        }

        IOrderedQueryable<Person> IApiQueryable<Person>.Query()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Guid Create(Person objectToCreate)
        {
            var request = new CreatePersonRequest
            {
                ObjectId = objectToCreate.ID,
                Name = objectToCreate.Name,
                Email = objectToCreate.Email,
                PhoneNumber = objectToCreate.PhoneNumber
            };

            return Create(request);
        }

        public void Update(Person objectToUpdate)
        {
            var request = new UpdatePersonRequest(objectToUpdate.ID)
            {
                Name = objectToUpdate.Name,
                Email = objectToUpdate.Email,
                PhoneNumber = objectToUpdate.PhoneNumber
            };

            Update(request);
        }

        public IEnumerable<Guid> CreateOrUpdate(IEnumerable<Person> objectsToCreate)
        {
            throw new NotImplementedException();
        }

        public void Delete(params Person[] objectsToDelete)
        {
            Delete(new DeletePersonRequest(objectsToDelete.Select(x => x.ID).ToArray()));
        }

        public void Delete(params Guid[] objectIdsToDelete)
        {
            Delete(new DeletePersonRequest(objectIdsToDelete));
        }

        private Guid Create(CreatePersonRequest createPersonRequest)
        {
            var result = _pnoHelper.DomInstances.Create(createPersonRequest.ToInstance());
            return result.ID.Id;
        }

        private void Update(UpdatePersonRequest updatePersonRequest)
        {
            if (updatePersonRequest == null)
            {
                throw new ArgumentNullException(nameof(updatePersonRequest));
            }

            var peopleInstance = new PeopleInstance(_pnoHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(updatePersonRequest.ObjectId)).Single());

            peopleInstance.ContactInfo.Email = updatePersonRequest.Email;
            peopleInstance.ContactInfo.Phone = updatePersonRequest.PhoneNumber;
            peopleInstance.PeopleInformation.FullName = updatePersonRequest.Name;

            _pnoHelper.DomInstances.Update(peopleInstance.ToInstance());
        }

        private void Delete(params DeletePersonRequest[] deletePersonRequest)
        {
            var filters = deletePersonRequest.Select(x => new ORFilterElement<DomInstance>(DomInstanceExposers.Id.Equal(x.ObjectId))).ToArray();
            var instancesToDelete = _pnoHelper.DomInstances.Read(new ORFilterElement<DomInstance>(filters));

            if (!instancesToDelete.Any()) return;

            _pnoHelper.DomInstances.Delete(instancesToDelete);
        }
    }
}
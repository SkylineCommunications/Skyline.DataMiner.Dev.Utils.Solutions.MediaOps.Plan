namespace Skyline.DataMiner.MediaOps.API.Common.API.People
{
    using System;
    using Skyline.DataMiner.MediaOps.API.Common.API.Generic;

    // IO Data classes
    internal class UpdatePersonRequest : IRequest
    {
        public UpdatePersonRequest(Guid id)
        {
            ObjectId = id;
        }

        public string Name { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public Guid RequestId { get; set; } = Guid.NewGuid();

        public Guid ObjectId { get; set; }
    }
}
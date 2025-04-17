namespace Skyline.DataMiner.MediaOps.API.Common.API.People
{
    using System;

    using Skyline.DataMiner.MediaOps.API.Common.API.Generic;

    public class PersonConfig : IConfiguration
    {
        public Guid? ObjectId { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }
    }
}

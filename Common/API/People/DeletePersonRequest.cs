namespace Skyline.DataMiner.MediaOps.API.Common.API.People
{
    using System;
    using System.Collections.Generic;
    using Skyline.DataMiner.MediaOps.API.Common.API.Generic;

    internal class DeletePersonRequest : IRequest
    {
        public DeletePersonRequest(params Guid[] ids)
        {
            PersonIds = new List<Guid>(ids);
        }

        public Guid RequestId { get; set; }

        public Guid ObjectId { get; set; }

        public IList<Guid> PersonIds { get; private set; }
    }
}
namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Linq;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using SLDataGateway.API.Types.Querying;

    /// <summary>
    /// Provides a base repository for managing DOM-based API objects.
    /// </summary>
    internal abstract class DomRepository : Repository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DomRepository"/> class.
        /// </summary>
        /// <param name="planApi">The MediaOps Plan API instance.</param>
        protected DomRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }
    }
}

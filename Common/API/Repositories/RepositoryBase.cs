namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    internal abstract class RepositoryBase<T> where T : IApiObject
    {
        private readonly IMediaOpsPlanApi planApi;

        public RepositoryBase(IMediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        public IMediaOpsPlanApi PlanApi => this.planApi;
    }
}

namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    internal abstract class RepositoryBase<T> where T : IApiObject
    {
        private readonly MediaOpsPlanApi planApi;

        public RepositoryBase(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        public MediaOpsPlanApi PlanApi => planApi;
    }
}

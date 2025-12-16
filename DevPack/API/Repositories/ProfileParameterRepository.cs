namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    internal abstract class ProfileParameterRepository<T> : Repository<T, Net.Profiles.Parameter> where T : Parameter
    {
        protected ProfileParameterRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }
    }
}

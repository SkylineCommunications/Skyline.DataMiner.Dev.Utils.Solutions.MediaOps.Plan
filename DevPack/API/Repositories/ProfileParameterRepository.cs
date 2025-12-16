namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    /// <summary>
    /// Provides a base repository for managing profile parameter-based API objects.
    /// </summary>
    /// <typeparam name="T">The type of parameter object managed by this repository.</typeparam>
    internal abstract class ProfileParameterRepository<T> : Repository<T, Net.Profiles.Parameter> where T : Parameter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileParameterRepository{T}"/> class.
        /// </summary>
        /// <param name="planApi">The MediaOps Plan API instance.</param>
        protected ProfileParameterRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }
    }
}

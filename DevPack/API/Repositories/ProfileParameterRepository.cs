namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    /// <summary>
    /// Provides a base repository for managing profile parameter-based API objects.
    /// </summary>
    internal abstract class ProfileParameterRepository : Repository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileParameterRepository"/> class.
        /// </summary>
        /// <param name="planApi">The MediaOps Plan API instance.</param>
        protected ProfileParameterRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }
    }
}

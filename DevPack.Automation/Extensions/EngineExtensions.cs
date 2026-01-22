namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Automation
{
    using System;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.Automation;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

    /// <summary>
    /// Defines extension methods on the <see cref="Engine"/> class.
    /// </summary>
    public static class EngineExtensions
    {
        /// <summary>
        /// Retrieves an instance of the <see cref="IMediaOpsPlanApi"/> interface."/>
        /// </summary>
        /// <param name="engine">The <see cref="Engine"/> instance.</param>
        /// <param name="logger">The <see cref="ILogger"/> implementation.</param>
        /// <returns>Instance of the <see cref="IMediaOpsPlanApi"/> interface.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="engine"/> is <see langword="null" />.</exception>
        public static IMediaOpsPlanApi GetMediaOpsPlanApi(this Engine engine, ILogger logger = null)
        {
            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            return new MediaOpsPlanApi(engine.GetUserConnection(), logger);
        }
    }
}

namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Automation
{
    using System;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.Automation;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

    /// <summary>
    /// Defines extension methods on the <see cref="IEngine"/> interface.
    /// </summary>
    public static class IEngineExtensions
    {
        /// <summary>
        /// Retrieves an instance of the <see cref="IMediaOpsPlanApi"/> interface."/>
        /// </summary>
        /// <param name="engine">The <see cref="IEngine"/> implementation.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> implementation.</param>
        /// <returns>Instance of the <see cref="IMediaOpsPlanApi"/> interface.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="engine"/> is <see langword="null" />.</exception>
        public static IMediaOpsPlanApi GetMediaOpsPlanApi(this IEngine engine, ILoggerFactory loggerFactory = null)
        {
            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            return new MediaOpsPlanApi(engine.GetUserConnection(), loggerFactory);
        }
    }
}

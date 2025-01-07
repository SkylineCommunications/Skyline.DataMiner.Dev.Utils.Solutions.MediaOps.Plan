namespace Skyline.DataMiner.MediaOps.API.Automation
{
    using System;

    using Skyline.DataMiner.Automation;
    using Skyline.DataMiner.MediaOps.API.Common;

    /// <summary>
    /// Defines extension methods on the <see cref="Engine"/> class.
    /// </summary>
    public static class EngineExtensions
    {
        /// <summary>
        /// Retrieves an instance of the <see cref="MediaOpsHelpers"/> class.
        /// </summary>
        /// <param name="engine">The <see cref="Engine"/> instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="engine"/> is <see langword="null" />.</exception>
        /// <returns>Instance of the <see cref="MediaOpsHelpers"/> class.</returns>
        public static MediaOpsHelpers GetMediaOpsHelpers(Engine engine)
        {
            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            return new MediaOpsHelpers(new ConnectionCommunication(engine.GetUserConnection()));
        }
    }
}

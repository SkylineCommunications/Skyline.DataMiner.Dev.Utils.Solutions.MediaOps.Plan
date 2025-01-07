namespace Skyline.DataMiner.MediaOps.API.Common
{
    using System;

    /// <summary>
    /// Provides helpers to interact with the MediaOps solution.
    /// </summary>
    public class MediaOpsHelpers
    {
        private readonly ICommunication communication;

        private Lazy<ResourceStudio.ResourceStudioHelper> lazyResourceStudioHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaOpsHelpers"/> class.
        /// </summary>
        /// <param name="communication">The <see cref="ICommunication"/> implementation.</param>
        /// <exception cref="ArgumentNullException"><paramref name="communication"/> is <see langword="null" />.</exception>
        public MediaOpsHelpers(ICommunication communication)
        {
            this.communication = communication ?? throw new ArgumentNullException(nameof(communication));

            Init();
        }

        internal ICommunication Communication => communication;

        /// <summary>
        /// Gets the instance of <see cref="ResourceStudio.ResourceStudioHelper"/>.
        /// </summary>
        /// <value>The lazily initialized <see cref="ResourceStudio.ResourceStudioHelper"/> instance.</value>
        public ResourceStudio.ResourceStudioHelper ResourceStudioHelper => lazyResourceStudioHelper.Value;

        private void Init()
        {
            lazyResourceStudioHelper = new Lazy<ResourceStudio.ResourceStudioHelper>(() => new ResourceStudio.ResourceStudioHelper(this));
        }
    }
}

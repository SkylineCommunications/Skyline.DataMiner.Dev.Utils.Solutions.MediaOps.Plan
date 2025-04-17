namespace Skyline.DataMiner.MediaOps.API.Common
{
    using System;
    using System.Net.Http.Headers;

    using Skyline.DataMiner.Core.DataMinerSystem.Common;
    using Skyline.DataMiner.MediaOps.API.Common.Providers;

    /// <summary>
    /// Provides helpers to interact with the MediaOps solution.
    /// </summary>
    public class MediaOpsHelpers
    {
        private readonly ICommunication communication;

        private Lazy<DataProviders> lazyDataProviders;

        private Lazy<ResourceStudio.DomResourceStudioHelper> lazyResourceStudioHelper;

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

        /// <summary>
        /// Gets the implementation of <see cref="ResourceStudio.IResourceStudioHelper"/>.
        /// </summary>
        /// <value>The lazily initialized <see cref="ResourceStudio.IResourceStudioHelper"/> instance.</value>
        public ResourceStudio.IResourceStudioHelper ResourceStudioHelper => lazyResourceStudioHelper.Value;

        internal ICommunication Communication => communication;

        internal DataProviders DataProviders => lazyDataProviders.Value;

        private void Init()
        {
            lazyDataProviders = new Lazy<DataProviders>(() => new DataProviders(Communication));

            lazyResourceStudioHelper = new Lazy<ResourceStudio.DomResourceStudioHelper>(() => new ResourceStudio.DomResourceStudioHelper(this));
        }
    }

    public static class MediaOpsHelpersExtensions
    {
        /// <summary>
        /// Gets the <see cref="MediaOpsHelpers"/> instance from the <see cref="ICommunication"/> instance.
        /// </summary>
        /// <param name="communication">The <see cref="ICommunication"/> instance.</param>
        /// <returns>The <see cref="MediaOpsHelpers"/> instance.</returns>
        public static IMediaOps GetMediaOpsHelpers(this IDms thisDms)
        {
            throw new NotImplementedException();
        }
    }

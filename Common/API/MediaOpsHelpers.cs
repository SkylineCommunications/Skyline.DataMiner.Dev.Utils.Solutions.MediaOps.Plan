namespace Skyline.DataMiner.MediaOps.API.Common
{
    using System;

    using Skyline.DataMiner.MediaOps.API.Common.API;
    using Skyline.DataMiner.MediaOps.API.Common.API.People;
    using Skyline.DataMiner.MediaOps.API.Common.Providers;

    public class MediaOpsPlanApi : IMediaOpsPlanApi
    {
        private readonly Lazy<DataProviders> _lazyDataProviders;

        private readonly ICommunication _communication;
        private readonly Lazy<PeopleApi> _peopleApi;

        public MediaOpsPlanApi(ICommunication communication)
        {
            _communication = communication ?? throw new ArgumentNullException(nameof(communication));
            _lazyDataProviders = new Lazy<DataProviders>(() => new DataProviders(Communication));
            _peopleApi = new Lazy<PeopleApi>(() => new PeopleApi(this));
        }

        public IPeopleApi People => _peopleApi.Value;

        internal ICommunication Communication => _communication;

        internal DataProviders DataProviders => _lazyDataProviders.Value;
    }
}

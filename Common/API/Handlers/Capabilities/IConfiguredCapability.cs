namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    internal interface IConfiguredCapability
    {
        Guid ProfileParameterId { get; }

        string StringValue { get; }
    }
}

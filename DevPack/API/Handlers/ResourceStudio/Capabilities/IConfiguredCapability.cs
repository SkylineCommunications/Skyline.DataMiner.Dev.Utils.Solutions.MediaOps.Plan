namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	internal interface IConfiguredCapability
	{
		Guid ProfileParameterId { get; }

		string StringValue { get; }
	}
}

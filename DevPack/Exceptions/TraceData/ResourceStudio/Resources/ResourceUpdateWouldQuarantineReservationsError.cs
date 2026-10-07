namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	using Skyline.DataMiner.Net.ResponseErrorData;

	internal sealed class ResourceUpdateWouldQuarantineReservationsError : ResourceError
	{
		internal ResourceUpdateWouldQuarantineReservationsError(Guid coreResourceId, ResourceManagerErrorData resourceManagerError)
		{
			if (resourceManagerError == null)
			{
				throw new ArgumentNullException(nameof(resourceManagerError));
			}

			Id = coreResourceId;
			ErrorMessage = resourceManagerError.ToString();
			ResourceManagerError = resourceManagerError;
		}

		internal ResourceManagerErrorData ResourceManagerError { get; }
	}
}

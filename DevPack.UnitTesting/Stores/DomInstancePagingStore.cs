namespace Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Stores
{
	using System;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.DOM.UnitTesting;

	using SLDataGateway.API.Querying;
	using SLDataGateway.API.Types.Querying;

	/// <summary>
	/// Handles DOM instance paged read requests, mirroring how a real DataMiner Agent answers a
	/// <see cref="ManagerStoreStartPagingRequest{T}"/>.
	/// </summary>
	/// <remarks>
	/// An agent uses the limit of a paged query only as a hint for the page size: it keeps handing out pages until
	/// the final page is reached, so the limit and the offset are not applied to the total result.
	/// <see cref="DomSLNetMessageHandler"/> does apply them, so they are stripped here to keep the simulation
	/// faithful. Without this, code that wrongly assumes a paged read honors the limit passes in a test but not
	/// against a real agent.
	/// </remarks>
	internal static class DomInstancePagingStore
	{
		private const long NoLimit = Int32.MaxValue;

		public static bool TryHandleMessage(DMSMessage message, DomSLNetMessageHandler domHandler, out DMSMessage response)
		{
			if (domHandler is null)
			{
				throw new ArgumentNullException(nameof(domHandler));
			}

			response = null;

			if (!(message is ManagerStoreStartPagingRequest<DomInstance> request) || !IsLimited(request.Filter))
			{
				return false;
			}

			var unlimitedRequest = new ManagerStoreStartPagingRequest<DomInstance>(
				request.Filter.WithLimit(LimitBy.Default),
				request.PreferredPageSize)
			{
				ModuleId = request.ModuleId,
			};

			return domHandler.TryHandleMessage(unlimitedRequest, out response);
		}

		private static bool IsLimited(IQuery<DomInstance> query)
		{
			var limit = query?.Limit;
			if (limit == null)
			{
				return false;
			}

			if (limit.Limit != NoLimit)
			{
				return true;
			}

			return limit is LimitBy limitBy && limitBy.Offset > 0;
		}
	}
}

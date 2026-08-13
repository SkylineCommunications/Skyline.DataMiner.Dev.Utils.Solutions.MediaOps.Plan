namespace Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Stores
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Select;
	using Skyline.DataMiner.Net.Apps.ManagerStore.Select;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.DOM.UnitTesting;

	/// <summary>
	/// Handles DOM instance select (partial object) read requests, mirroring how a real DataMiner Agent
	/// answers a <see cref="ManagerStoreSelectReadRequest{T}"/>.
	/// </summary>
	/// <remarks>
	/// A select read returns only the requested fields instead of full objects. The agent replies with a
	/// <see cref="ManagerStoreCrudResponse{T}"/> whose custom response data holds a <see cref="SelectResult"/>.
	/// <see cref="DomSLNetMessageHandler"/> does not support these requests and returns <see langword="null"/>,
	/// so the request is translated into a regular read whose results are reduced to the requested fields.
	/// </remarks>
	internal static class DomInstanceSelectStore
	{
		public static bool TryHandleMessage(DMSMessage message, DomSLNetMessageHandler domHandler, out DMSMessage response)
		{
			if (domHandler is null)
			{
				throw new ArgumentNullException(nameof(domHandler));
			}

			response = null;

			if (!(message is ManagerStoreSelectReadRequest<DomInstance> request))
			{
				return false;
			}

			var readRequest = new ManagerStoreReadRequest<DomInstance>(request.Query)
			{
				ModuleId = request.ModuleId,
			};

			if (!domHandler.TryHandleMessage(readRequest, out var readResponse)
				|| !(readResponse is ManagerStoreCrudResponse<DomInstance> crudResponse))
			{
				return false;
			}

			response = CreateResponse(request, crudResponse.Objects ?? new List<DomInstance>());
			return true;
		}

		private static DMSMessage CreateResponse(ManagerStoreSelectReadRequest<DomInstance> request, IEnumerable<DomInstance> instances)
		{
			if (request is null)
			{
				throw new ArgumentNullException(nameof(request));
			}

			var selectedFields = request.SelectedFields ?? new List<SelectedFieldReference>();
			var objects = new List<PartialObjectData>();

			foreach (var instance in instances)
			{
				if (instance?.ID is null)
				{
					continue;
				}

				var factory = new PartialDomInstanceFactory(instance.ID.ModuleId);
				var values = new List<IPartialObjectValue>(selectedFields.Count);

				foreach (var selectedField in selectedFields)
				{
					values.Add(new PartialObjectValue<object>
					{
						FieldReferenceId = selectedField.Id,
						Value = Execute(selectedField, instance),
					});
				}

				objects.Add(factory.GetPartialObjectData(instance.ID, values));
			}

			return new ManagerStoreCrudResponse<DomInstance>((object)new SelectResult { Objects = objects });
		}

		private static object Execute(SelectedFieldReference selectedField, DomInstance instance)
		{
			try
			{
				return selectedField.SerializableExposer?.Exposer?.execute(instance);
			}
			catch
			{
				// Mirror the agent's fail-safe behavior: a field that cannot be read is returned as no value.
				return null;
			}
		}
	}
}

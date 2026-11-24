namespace RT_MediaOps.Plan.RegressionTests
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	internal sealed class SimulatedPropertyAttachmentStore : IPropertyAttachmentStore
	{
		private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte[]>> attachments = new ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte[]>>();

		public void Add(DomInstanceId instanceId, string attachmentName, byte[] content)
		{
			var perInstance = attachments.GetOrAdd(instanceId.Id, _ => new ConcurrentDictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase));
			perInstance[attachmentName] = (byte[])content.Clone();
		}

		public byte[] Get(DomInstanceId instanceId, string attachmentName)
		{
			if (attachments.TryGetValue(instanceId.Id, out var perInstance) && perInstance.TryGetValue(attachmentName, out var content))
			{
				return (byte[])content.Clone();
			}

			throw new InvalidOperationException($"Attachment '{attachmentName}' was not found.");
		}

		public void Delete(DomInstanceId instanceId, string attachmentName)
		{
			if (attachments.TryGetValue(instanceId.Id, out var perInstance))
			{
				perInstance.TryRemove(attachmentName, out _);
			}
		}

		public IReadOnlyCollection<string> GetNames(DomInstanceId instanceId)
		{
			return attachments.TryGetValue(instanceId.Id, out var perInstance) ? perInstance.Keys.ToArray() : Array.Empty<string>();
		}
	}
}

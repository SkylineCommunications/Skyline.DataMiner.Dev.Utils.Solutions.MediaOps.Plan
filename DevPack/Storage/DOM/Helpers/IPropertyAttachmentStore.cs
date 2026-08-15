namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

	// Seam around the DataMiner attachment API so the attachment handling can be verified without a DataMiner Agent.
	internal interface IPropertyAttachmentStore
	{
		void Add(DomInstanceId instanceId, string attachmentName, byte[] content);

		byte[] Get(DomInstanceId instanceId, string attachmentName);

		void Delete(DomInstanceId instanceId, string attachmentName);

		IReadOnlyCollection<string> GetNames(DomInstanceId instanceId);
	}
}

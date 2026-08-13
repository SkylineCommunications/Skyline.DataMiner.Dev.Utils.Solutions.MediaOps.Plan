namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

	internal sealed class PropertyAttachmentStore : IPropertyAttachmentStore
	{
		private readonly SlcPropertiesHelper propertiesHelper;

		public PropertyAttachmentStore(SlcPropertiesHelper propertiesHelper)
		{
			this.propertiesHelper = propertiesHelper ?? throw new ArgumentNullException(nameof(propertiesHelper));
		}

		public void Add(DomInstanceId instanceId, string attachmentName, byte[] content)
		{
			propertiesHelper.DomHelper.DomInstances.Attachments.Add(instanceId, attachmentName, content);
		}

		public byte[] Get(DomInstanceId instanceId, string attachmentName)
		{
			return propertiesHelper.DomHelper.DomInstances.Attachments.Get(instanceId, attachmentName);
		}

		public void Delete(DomInstanceId instanceId, string attachmentName)
		{
			propertiesHelper.DomHelper.DomInstances.Attachments.Delete(instanceId, attachmentName);
		}

		public IReadOnlyCollection<string> GetNames(DomInstanceId instanceId)
		{
			return propertiesHelper.DomHelper.DomInstances.Attachments.GetFileNames(instanceId);
		}
	}
}

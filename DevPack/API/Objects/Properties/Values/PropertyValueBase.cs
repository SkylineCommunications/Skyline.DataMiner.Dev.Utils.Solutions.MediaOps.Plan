namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageProperties = Storage.DOM.SlcProperties;

	public abstract class PropertyValueBase : TrackableObject
	{
		private StorageProperties.PropertyValueSection originalSection;
		private StorageProperties.PropertyValueSection updatedSection;

		private protected PropertyValueBase()
		{
			IsNew = true;
		}

		internal PropertyValueBase(StorageProperties.PropertyValueSection section)
		{
			ParseSection(section);
		}

		public string Name { get; protected set; }

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));
		}
	}
}
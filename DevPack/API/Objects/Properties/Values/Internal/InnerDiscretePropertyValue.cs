namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageProperties = Storage.DOM.SlcProperties;

	internal class InnerDiscretePropertyValue : DiscretePropertyValue
	{
		private StorageProperties.PropertyValueSection originalSection;

		internal InnerDiscretePropertyValue(StorageProperties.PropertyValueSection section) : base()
		{
			IsNew = false;
			ParseSection(section);
			InitTracking();
		}

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));
			PropertyId = section.PropertyID.Value;
			Value = section.Value;
		}
	}
}

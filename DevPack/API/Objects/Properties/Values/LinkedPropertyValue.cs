namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageProperties = Storage.DOM.SlcProperties;

	public abstract class LinkedPropertyValue : PropertyValueBase
	{
		public LinkedPropertyValue(Property property)
		{
			PropertyId = property?.Id ?? throw new ArgumentNullException(nameof(property));
		}

		internal LinkedPropertyValue(StorageProperties.PropertyValueSection section) : base(section)
		{
			ParseSection(section);
		}

		public Guid PropertyId { get; private set; }

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			PropertyId = section.PropertyID.Value;
		}
	}
}

namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageProperties = Storage.DOM.SlcProperties;

	public class BooleanPropertyValue : LinkedPropertyValue
	{
		public BooleanPropertyValue(BooleanProperty property) : base(property)
		{
		}

		internal BooleanPropertyValue(StorageProperties.PropertyValueSection section) : base(section)
		{
			ParseSection(section);
			InitTracking();
		}

		public bool Value { get; set; }

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			Value = Convert.ToBoolean(section.Value);
		}
	}
}

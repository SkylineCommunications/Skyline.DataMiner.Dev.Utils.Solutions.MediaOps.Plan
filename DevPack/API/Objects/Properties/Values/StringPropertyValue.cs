namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using StorageProperties = Storage.DOM.SlcProperties;

	public class StringPropertyValue : LinkedPropertyValue
	{
		public StringPropertyValue(StringProperty property) : base(property)
		{
		}

		internal StringPropertyValue(StorageProperties.PropertyValueSection section) : base(section)
		{
			ParseSection(section);
			InitTracking();
		}

		public string Value { get; set; }

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			Value = section.Value;
		}
	}
}

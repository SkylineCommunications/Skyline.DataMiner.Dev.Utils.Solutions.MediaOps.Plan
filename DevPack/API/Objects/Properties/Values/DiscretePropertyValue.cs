namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using StorageProperties = Storage.DOM.SlcProperties;

	public class DiscretePropertyValue : LinkedPropertyValue
	{
		public DiscretePropertyValue(DiscreteProperty property) : base(property)
		{
		}

		internal DiscretePropertyValue(StorageProperties.PropertyValueSection section) : base(section)
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

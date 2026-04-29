namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageProperties = Storage.DOM.SlcProperties;

	public class CustomPropertyValue : PropertyValueBase
	{
		public CustomPropertyValue(string name)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}

		internal CustomPropertyValue(StorageProperties.PropertyValueSection section) : base(section)
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

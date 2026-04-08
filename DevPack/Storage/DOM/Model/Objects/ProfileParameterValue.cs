namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	using System;

	using Newtonsoft.Json;

	[JsonObject(MemberSerialization.OptIn)]
	internal class ProfileParameterValue
	{
		[JsonProperty("id")]
		public Guid ProfileParameterId { get; set; }

		[JsonProperty("stringValue", NullValueHandling = NullValueHandling.Ignore)]
		public string StringValue { get; set; }

		[JsonProperty("doubleMaxValue", NullValueHandling = NullValueHandling.Ignore)]
		public double? DoubleMaxValue { get; set; }

		[JsonProperty("doubleMinValue", NullValueHandling = NullValueHandling.Ignore)]
		public double? DoubleMinValue { get; set; }
	}
}

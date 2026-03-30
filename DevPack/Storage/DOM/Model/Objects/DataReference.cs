namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	using Newtonsoft.Json;

	[JsonObject(MemberSerialization.OptIn)]
	internal class DataReference
	{
		[JsonProperty("referenceType")]
		public string ReferenceType { get; set; }

		[JsonProperty("referenceId")]
		public string ReferenceId { get; set; }
	}
}

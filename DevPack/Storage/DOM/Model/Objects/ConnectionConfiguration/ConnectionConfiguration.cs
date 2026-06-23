namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	using System.Collections.Generic;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft;

	internal sealed class ConnectionConfiguration
	{
		public ICollection<LevelMappingInfo> LevelMappings { get; set; } = new List<LevelMappingInfo>();

		public static bool TryDeserialize(string json, out ConnectionConfiguration connectionConfiguration)
		{
			connectionConfiguration = null;

			if (string.IsNullOrEmpty(json))
			{
				return false;
			}

			try
			{
				connectionConfiguration = SecureNewtonsoftDeserialization.DeserializeObject<ConnectionConfiguration>(json);
				return true;
			}
			catch (JsonException)
			{
				// Handle JSON parsing errors if necessary
				return false;
			}
		}

		public string Serialize()
		{
			return JsonConvert.SerializeObject(this);
		}
	}
}

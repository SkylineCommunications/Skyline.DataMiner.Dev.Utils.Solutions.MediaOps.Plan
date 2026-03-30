namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;

    using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft;

    [JsonObject(MemberSerialization.OptIn)]
    internal class ScriptExecutionDetails : IEquatable<ScriptExecutionDetails>
    {
        [JsonProperty("script")]
        public string ScriptName { get; set; }

        [JsonProperty("parameters")]
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        [JsonProperty("dummies")]
        public Dictionary<string, string> Dummies { get; set; } = new Dictionary<string, string>();

        [JsonProperty("values")]
        public List<ProfileParameterValue> ProfileParameterValues { get; set; } = new List<ProfileParameterValue>();

        [JsonProperty("parameterReferences")]
        public Dictionary<string, DataReference> ParameterReferences { get; set; } = new Dictionary<string, DataReference>();

        [JsonProperty("dummyReferences")]
        public Dictionary<string, DataReference> DummyReferences { get; set; } = new Dictionary<string, DataReference>();

        public static bool TryDeserialize(string json, out ScriptExecutionDetails scriptExecutionDetails)
        {
            scriptExecutionDetails = null;

            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                scriptExecutionDetails = SecureNewtonsoftDeserialization.DeserializeObject<ScriptExecutionDetails>(json);
                return true;
            }
            catch (JsonException)
            {
                // Handle JSON parsing errors if necessary
                return false;
            }
        }

        public bool Equals(ScriptExecutionDetails other)
        {
            if (other == null)
            {
                return false;
            }

            if (ScriptName != other.ScriptName)
            {
                return false;
            }

            if (!Parameters.SequenceEqual(other.Parameters))
            {
                return false;
            }

            if (!Dummies.SequenceEqual(other.Dummies))
            {
                return false;
            }

            if (!ProfileParameterValues.SequenceEqual(other.ProfileParameterValues))
            {
                return false;
            }

            if (!ParameterReferences.SequenceEqual(other.ParameterReferences))
            {
                return false;
            }

            if (!DummyReferences.SequenceEqual(other.DummyReferences))
            {
                return false;
            }

            return true;
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

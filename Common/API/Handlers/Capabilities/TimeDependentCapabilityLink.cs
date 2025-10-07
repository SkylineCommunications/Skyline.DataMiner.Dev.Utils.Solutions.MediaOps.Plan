namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents the data required for parsing Remarks property for capabilities.
    /// </summary>
    internal class TimeDependentCapabilityLink
    {
        /// <summary>
        /// Gets or sets a value indicating whether the capability is time-dependent.
        /// </summary>
        [JsonProperty("isTimeDependent")]
        public bool IsTimeDependent { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the linked parameter.
        /// </summary>
        [JsonProperty("linkedParameterId")]
        public Guid LinkedParameterId { get; set; }

        /// <summary>
        /// Serializes a <see cref="TimeDependentCapabilityLink"/> object to a JSON string.
        /// </summary>
        /// <returns>A JSON string representation of the capability, or <see langword="null"/> if the input capability is <see langword="null"/>.</returns>
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string into a <see cref="TimeDependentCapabilityLink"/> object.
        /// </summary>
        /// <param name="remarksJson">The JSON string to deserialize.</param>
        /// <param name="result">When this method returns, contains the deserialized <see cref="TimeDependentCapabilityLink"/> object if the deserialization was successful; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the JSON string was successfully deserialized; otherwise, <see langword="false"/>.</returns>
        public static bool TryDeserialize(string remarksJson, out TimeDependentCapabilityLink result)
        {
            result = null;

            if (string.IsNullOrWhiteSpace(remarksJson))
            {
                return false;
            }

            try
            {
                result = JsonConvert.DeserializeObject<TimeDependentCapabilityLink>(remarksJson);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
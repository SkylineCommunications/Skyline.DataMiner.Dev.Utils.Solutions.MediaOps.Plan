namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

    internal partial class ResourceCapabilitiesSection : IConfiguredCapability
    {
        public Guid ProfileParameterId
        {
            get
            {
                if (string.IsNullOrEmpty(ProfileParameterID) || !Guid.TryParse(ProfileParameterID, out var id))
                {
                    return Guid.Empty;
                }

                return id;
            }
            internal set
            {
                ProfileParameterID = value == Guid.Empty ? null : value.ToString();
            }
        }

        internal IEnumerable<string> DiscreteValues
        {
            get
            {
                if (string.IsNullOrWhiteSpace(StringValue))
                {
                    return Array.Empty<string>();
                }

                return StringValue.Split([";"], StringSplitOptions.RemoveEmptyEntries);
            }

            set
            {
                if (value == null || !value.Any())
                {
                    StringValue = string.Empty;
                }
                else
                {
                    StringValue = string.Join(";", value);
                }
            }
        }
    }
}

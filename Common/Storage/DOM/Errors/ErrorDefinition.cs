namespace Skyline.DataMiner.MediaOps.Plan.Storage.DOM
{
    using System;
    using System.Collections.Generic;

    internal sealed class ErrorDefinition : IEquatable<ErrorDefinition>
    {
        public ErrorDefinition(string errorCode, string description)
        {
            if (String.IsNullOrWhiteSpace(errorCode))
            {
                throw new ArgumentException($"'{nameof(errorCode)}' cannot be null or whitespace.", nameof(errorCode));
            }

            if (String.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException($"'{nameof(description)}' cannot be null or whitespace.", nameof(description));
            }

            ErrorCode = errorCode;
            Description = description;
        }

        public string ErrorCode { get; private set; }

        public string Description { get; private set; }

        public override bool Equals(object obj)
        {
            return obj is ErrorDefinition definition &&
                   Equals(definition);
        }

        public bool Equals(ErrorDefinition other)
        {
            return other != null &&
                   ErrorCode == other.ErrorCode;
        }

        public override int GetHashCode()
        {
            return 431791832 + EqualityComparer<string>.Default.GetHashCode(ErrorCode);
        }

        public static bool operator ==(ErrorDefinition left, ErrorDefinition right)
        {
            return EqualityComparer<ErrorDefinition>.Default.Equals(left, right);
        }

        public static bool operator !=(ErrorDefinition left, ErrorDefinition right)
        {
            return !(left == right);
        }
    }
}

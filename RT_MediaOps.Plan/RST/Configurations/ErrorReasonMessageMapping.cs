namespace RT_MediaOps.Plan.RST.Configurations
{
    using System;
    using Skyline.DataMiner.MediaOps.Plan.Exceptions;

    internal sealed class ErrorReasonMessageMapping
    {
        public ErrorReasonMessageMapping(ConfigurationConfigurationError.Reason reason, string message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            Reason = reason;
            Message = message;
        }

        public ConfigurationConfigurationError.Reason Reason { get; }

        public string Message { get; }

        public override bool Equals(object obj)
        {
            if (obj is not ErrorReasonMessageMapping other)
            {
                return false;
            }

            return other.Reason == Reason && other.Message == Message;
        }

        public override int GetHashCode()
        {
            return Tuple.Create(Reason, Message).GetHashCode();
        }
    }
}

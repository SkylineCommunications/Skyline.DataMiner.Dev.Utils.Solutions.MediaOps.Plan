namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    internal class ParameterDiscreteValue<T>
    {
        public Guid ParameterId { get; set; }

        public T DiscreteValue { get; set; }
    }
}

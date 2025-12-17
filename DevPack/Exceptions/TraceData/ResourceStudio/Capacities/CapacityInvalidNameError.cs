namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when a capacity configuration specifies an invalid capacity name.
    /// </summary>
    public class CapacityInvalidNameError : CapacityError
    {
        /// <summary>
        /// Gets the name of the capacity.
        /// </summary>
        public string Name { get; set; }
    }
}

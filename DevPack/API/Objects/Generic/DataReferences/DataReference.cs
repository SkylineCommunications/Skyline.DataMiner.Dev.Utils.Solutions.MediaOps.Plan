namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    /// <summary>
    /// Represents a reference to a data source.
    /// </summary>
    public class DataReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataReference"/> class with the specified type.
        /// </summary>
        /// <param name="type">The type of data this reference points to.</param>
        public DataReference(DataReferenceType type)
        {
            Type = type;
        }

        /// <summary>
        /// Gets the type of data this reference points to.
        /// </summary>
        public DataReferenceType Type { get; }
    }
}

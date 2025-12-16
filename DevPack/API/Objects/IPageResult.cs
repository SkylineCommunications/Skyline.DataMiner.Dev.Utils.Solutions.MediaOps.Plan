namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a paged result set.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the page.</typeparam>
    public interface IPagedResult<out T> : IReadOnlyList<T>
    {
        /// <summary>
        /// Gets the current page number (0-based).
        /// </summary>
        int PageNumber { get; }

        /// <summary>
        /// Gets a value indicating whether there is a next page.
        /// </summary>
        bool HasNextPage { get; }
    }
}

namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a paged result set for a collection of items.
    /// </summary>
    /// <typeparam name="T">The type of the items in the result set.</typeparam>
    public class PagedResult<T> : IPagedResult<T>
    {
        /// <summary>
        /// Gets an empty <see cref="PagedResult{T}"/> instance.
        /// </summary>
        public static readonly PagedResult<T> Empty = new PagedResult<T>();

        private readonly List<T> _items;

        /// <summary>
        /// Initializes a new instance of the <see cref="PagedResult{T}"/> class.
        /// </summary>
        /// <param name="items">The items in the current page.</param>
        /// <param name="pageNumber">The zero-based page number.</param>
        /// <param name="pageSize">The maximum number of items per page.</param>
        /// <param name="hasNext">Indicates whether there is a next page.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="pageNumber"/> is less than 0,
        /// <paramref name="pageSize"/> is less than 1,
        /// or the number of items exceeds <paramref name="pageSize"/>.
        /// </exception>
        public PagedResult(IEnumerable<T> items, int pageNumber, int pageSize, bool hasNext)
        {
            _items = items?.ToList() ?? throw new ArgumentNullException(nameof(items));

            if (pageNumber < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be at least 0.");
            }

            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be at least 1.");
            }

            if (_items.Count > pageSize)
            {
                throw new ArgumentOutOfRangeException(nameof(items), "The amount of items don't fit in the page size.");
            }

            PageNumber = pageNumber;
            HasNextPage = hasNext;
        }

        internal PagedResult()
        {
            _items = new List<T>();
            PageNumber = 0;
            HasNextPage = false;
        }

        /// <inheritdoc/>
        public int PageNumber { get; }

        /// <inheritdoc/>
        public int Count { get => _items.Count; }

        /// <inheritdoc/>
        public bool HasNextPage { get; }

        /// <inheritdoc/>
        public T this[int index] { get => _items[index]; }

        public static PagedResult<T> Create(IEnumerable<T> items)
        {
            var list = items?.ToList() ?? throw new ArgumentNullException(nameof(items));
            return Create(list, list.Count, 0);
        }

        /// <summary>
        /// Creates a <see cref="PagedResult{T}"/> from the specified items and page size, starting at the first page.
        /// </summary>
        /// <param name="items">The items to include in the page.</param>
        /// <param name="pageSize">The maximum number of items per page.</param>
        /// <returns>A <see cref="PagedResult{T}"/> containing the items for the first page.</returns>
        public static PagedResult<T> Create(IEnumerable<T> items, int pageSize)
        {
            return Create(items, pageSize, 0);
        }

        /// <summary>
        /// Creates a <see cref="PagedResult{T}"/> from the specified items, page size, and page number.
        /// </summary>
        /// <param name="items">The items to include in the page.</param>
        /// <param name="pageSize">The maximum number of items per page.</param>
        /// <param name="page">The zero-based page number.</param>
        /// <returns>A <see cref="PagedResult{T}"/> containing the items for the specified page.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="pageSize"/> is less than or equal to 0,
        /// or <paramref name="page"/> is less than 0.
        /// </exception>
        public static PagedResult<T> Create(IEnumerable<T> items, int pageSize, int page)
        {
            if (pageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be at least 1.");
            }

            if (page < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(page), "Page number must be at least 0.");
            }

            var list = items.Skip(page * pageSize).ToList();
            var count = list.Count;
            var hasNext = false;
            if (list.Count > pageSize)
            {
                count = pageSize;
                hasNext = true;
            }

            return new PagedResult<T>(list.Take(pageSize), page, count, hasNext);
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

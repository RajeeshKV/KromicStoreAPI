using System;
using System.Collections.Generic;

namespace KromicStore.Contracts.Abstractions
{
    /// <summary>
    /// Generic response class for paginated API responses.
    /// Includes pagination metadata and navigation helpers.
    /// </summary>
    /// <typeparam name="T">The type of items contained in the paginated response.</typeparam>
    public class PagedResponse<T> : ApiResponse
    {
        /// <summary>
        /// Gets or sets the collection of items in the current page.
        /// </summary>
        public IReadOnlyList<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Gets or sets the current page number (1-based).
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the total number of items across all pages.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets the total number of pages.
        /// Calculated from TotalCount and PageSize.
        /// </summary>
        public int TotalPages
        {
            get
            {
                if (PageSize <= 0)
                    return 0;
                return (TotalCount + PageSize - 1) / PageSize;
            }
        }

        /// <summary>
        /// Initializes a new instance of the PagedResponse class.
        /// </summary>
        public PagedResponse()
        {
        }

        /// <summary>
        /// Initializes a new instance of the PagedResponse class with initial values.
        /// </summary>
        /// <param name="items">The collection of items for this page.</param>
        /// <param name="pageNumber">The current page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="totalCount">The total number of items across all pages.</param>
        /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
        /// <exception cref="ArgumentException">Thrown when pageNumber or pageSize is invalid.</exception>
        public PagedResponse(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items), "Items collection cannot be null");
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));
            if (pageSize < 1)
                throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));
            if (totalCount < 0)
                throw new ArgumentException("Total count cannot be negative", nameof(totalCount));

            Items = new List<T>(items).AsReadOnly();
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
        }

        /// <summary>
        /// Determines whether there is a next page available.
        /// </summary>
        /// <returns>True if a next page exists; otherwise, false.</returns>
        public bool HasNextPage()
        {
            return PageNumber < TotalPages;
        }

        /// <summary>
        /// Determines whether there is a previous page available.
        /// </summary>
        /// <returns>True if a previous page exists; otherwise, false.</returns>
        public bool HasPreviousPage()
        {
            return PageNumber > 1;
        }

        /// <summary>
        /// Gets metadata about this paginated response.
        /// </summary>
        /// <returns>A dictionary containing pagination metadata.</returns>
        public override IDictionary<string, object> GetMetadata()
        {
            return new Dictionary<string, object>
            {
                { "PageNumber", PageNumber },
                { "PageSize", PageSize },
                { "TotalCount", TotalCount },
                { "TotalPages", TotalPages },
                { "HasNextPage", HasNextPage() },
                { "HasPreviousPage", HasPreviousPage() }
            };
        }
    }
}

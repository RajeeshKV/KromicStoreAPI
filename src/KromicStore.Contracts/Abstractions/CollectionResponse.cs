using System;
using System.Collections.Generic;

namespace KromicStore.Contracts.Abstractions
{
    /// <summary>
    /// Generic response class for non-paginated collections of items.
    /// Used for endpoints that return entire collections without pagination.
    /// </summary>
    /// <typeparam name="T">The type of items contained in the collection.</typeparam>
    public class CollectionResponse<T> : ApiResponse
    {
        /// <summary>
        /// Gets or sets the collection of items.
        /// </summary>
        public IReadOnlyList<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Gets or sets the count of items in the collection.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Initializes a new instance of the CollectionResponse class.
        /// </summary>
        public CollectionResponse()
        {
        }

        /// <summary>
        /// Initializes a new instance of the CollectionResponse class with items.
        /// </summary>
        /// <param name="items">The collection of items to include in the response.</param>
        /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
        public CollectionResponse(IEnumerable<T> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items), "Items collection cannot be null");

            var itemsList = new List<T>(items);
            Items = itemsList.AsReadOnly();
            Count = itemsList.Count;
        }

        /// <summary>
        /// Gets metadata about this collection response.
        /// </summary>
        /// <returns>A dictionary containing collection metadata.</returns>
        public override IDictionary<string, object> GetMetadata()
        {
            return new Dictionary<string, object>
            {
                { "Count", Count },
                { "ItemType", typeof(T).Name }
            };
        }
    }
}

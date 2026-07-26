using System;

namespace KromicStore.Contracts.Abstractions
{
    /// <summary>
    /// Abstract base class for all API responses.
    /// Provides common properties that all API responses should include.
    /// </summary>
    public abstract class ApiResponse
    {
        /// <summary>
        /// Unique identifier for this response.
        /// Automatically generated as a GUID if not provided.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// UTC timestamp indicating when this response was generated.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets metadata about this response.
        /// Derived classes must implement this method to provide specific metadata.
        /// </summary>
        /// <returns>A dictionary containing metadata about the response.</returns>
        public abstract IDictionary<string, object> GetMetadata();
    }
}

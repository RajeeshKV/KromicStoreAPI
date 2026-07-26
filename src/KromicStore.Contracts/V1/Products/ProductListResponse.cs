namespace KromicStore.Contracts.V1.Products;

/// <summary>
/// Response DTO for paginated product list.
/// </summary>
public class ProductListResponse
{
    /// <summary>
    /// Gets or sets the list of products.
    /// </summary>
    public IReadOnlyList<ProductDto> Data { get; set; } = new List<ProductDto>();

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the page size (number of items per page).
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total count of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage { get; set; }
}

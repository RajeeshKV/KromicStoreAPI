namespace KromicStore.Contracts.V1.Orders;

/// <summary>
/// Represents a paginated list of orders in the response.
/// </summary>
public record OrderListResponse(
    /// <summary>
    /// The collection of orders for this page.
    /// </summary>
    IReadOnlyList<OrderListItemDto> Data,
    
    /// <summary>
    /// The current page number.
    /// </summary>
    int PageNumber,
    
    /// <summary>
    /// The size of each page.
    /// </summary>
    int PageSize,
    
    /// <summary>
    /// The total number of orders across all pages.
    /// </summary>
    int TotalCount,
    
    /// <summary>
    /// The total number of pages.
    /// </summary>
    int TotalPages,
    
    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    bool HasNextPage,
    
    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    bool HasPreviousPage);

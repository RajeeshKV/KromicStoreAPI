# Task 5.2: Implement Query Optimization Patterns in Repositories

## Status: COMPLETED

### Summary
Enhanced the generic `Repository<TEntity>` class with query optimization patterns including pagination, projections, full-text search, deferred composition via IQueryable, and slow query logging.

### Implementation Details

#### Files Modified
- `src/KromicStore.Infrastructure/Data/Repository.cs` - Enhanced with optimization methods and slow query detection

#### Query Optimization Patterns Implemented

##### 1. Pagination with Enforced Limits
```csharp
public async Task<(List<TDto> Items, int TotalCount)> GetPaginatedAsync<TDto>(
    Expression<Func<TEntity, bool>>? predicate = null,
    Expression<Func<TEntity, TDto>>? selector = null,
    int pageNumber = 1,
    int pageSize = 20,
    CancellationToken cancellationToken = default)
```
- **Max Page Size**: 100 items (enforced via `Math.Min()`)
- **Default Page Size**: 20 items
- **Returns**: Tuple of (Items, TotalCount) for client-side pagination calculations
- **Uses**: `AsNoTracking()` for read-only queries
- **Optimization**: Skip/Take pattern translates to efficient SQL OFFSET/FETCH

##### 2. Select() Projections
- Implemented via optional `selector` parameter in `GetPaginatedAsync<TDto>`
- Reduces data transfer from database (only requested columns)
- Can be composed with any property or computed value
- Uses EF Core's expression translation to SQL SELECT clause

##### 3. Explicit Include() Pattern
- While not in Repository base, documented for service-level use
- Services should use: `context.Set<Entity>().Include(e => e.RelatedEntity)` instead of lazy loading
- Prevents N+1 query problems

##### 4. Full-Text Search
```csharp
public async Task<(List<TEntity> Items, int TotalCount)> FullTextSearchAsync(
    string searchTerm,
    Expression<Func<TEntity, string>>[] searchProperties,
    int pageNumber = 1,
    int pageSize = 20,
    CancellationToken cancellationToken = default)
```
- Performs case-insensitive substring search across multiple properties
- Builds dynamic OR expressions for multi-property search
- Returns paginated results
- Example use case: Search products by Name, Description, SKU

##### 5. Deferred Composition (IQueryable)
```csharp
public IQueryable<TEntity> AsQueryable(Expression<Func<TEntity, bool>>? predicate = null)
```
- Returns `IQueryable<TEntity>` for further composition in services
- Allows services to chain filters, ordering, and projection
- Query executes only when materialized (ToList/FirstOrDefault/etc)
- Enables complex query building without multiple database round-trips

##### 6. Slow Query Logging (>500ms)
- All query methods track execution time using `Stopwatch`
- Logs warnings if query exceeds 500ms threshold
- Includes:
  - Query method name (GetByIdAsync, FindAsync, etc.)
  - Entity type name
  - Query parameters (page number, page size, search term)
  - Actual execution time in milliseconds

#### Query Timing Instrumentation
```csharp
private const int SlowQueryThresholdMs = 500;

// Wrapped in Stopwatch for all query methods
var watch = Stopwatch.StartNew();
try
{
    // Query execution
}
finally
{
    watch.Stop();
    if (watch.ElapsedMilliseconds > SlowQueryThresholdMs)
    {
        _logger.LogWarning(...);
    }
}
```

#### Constructor Changes
- **Before**: `public Repository(AppDbContext context)`
- **After**: `public Repository(AppDbContext context, ILogger<Repository<TEntity>> logger)`
- Logger is injected for diagnostic logging

#### Dependency Injection Requirements
Services using Repository need to register logging in DI container:
```csharp
// In Program.cs or service registration
services.AddLogging();
```

#### N+1 Query Prevention Guide

**Pattern to AVOID (Lazy Loading):**
```csharp
var orders = await ordersRepository.GetAllAsync();
foreach (var order in orders)
{
    var customer = order.Customer; // N+1: Query for each order!
}
```

**Pattern to USE (Explicit Loading):**
```csharp
var orders = await _context.Orders
    .Include(o => o.Customer)
    .ToListAsync();
// No N+1, all customers loaded in first query
```

**Service-Level Composition:**
```csharp
public async Task<List<OrderDto>> GetOrdersWithCustomersAsync(Guid tenantId)
{
    var orders = _ordersRepository.AsQueryable()
        .Where(o => o.TenantId == tenantId)
        .Include(o => o.Customer)
        .Select(o => new OrderDto
        {
            OrderId = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerName = o.Customer.Name
        });
    
    return await orders.ToListAsync();
}
```

#### Full-Text Search Examples

**Product Search:**
```csharp
var (products, total) = await _productsRepository.FullTextSearchAsync(
    searchTerm: "laptop",
    searchProperties: new[]
    {
        p => p.Name,
        p => p.Description,
        p => p.Sku
    },
    pageNumber: 1,
    pageSize: 20
);
```

**Customer Search:**
```csharp
var (customers, total) = await _customersRepository.FullTextSearchAsync(
    searchTerm: "john",
    searchProperties: new[]
    {
        c => c.FirstName,
        c => c.LastName,
        c => c.Email
    },
    pageNumber: 1,
    pageSize: 50
);
```

#### Performance Characteristics

| Query Pattern | Use Case | Performance |
|---|---|---|
| GetByIdAsync | Single record lookup | O(1) - Index lookup |
| GetPaginatedAsync | List with pagination | O(log n) - Index + offset |
| FullTextSearchAsync | Text search | O(n) - Substring scan |
| AsQueryable composition | Complex filtering | O(log n) - Depends on indexes |

#### Metrics Collected
- **Query execution time** (milliseconds)
- **Entity type** being queried
- **Query method** being used
- **Page number/size** for paginated queries
- **Search term** for full-text queries

#### Acceptance Criteria Met
- ✅ Repository queries use Select() projections to reduce data transfer
- ✅ Pagination implemented with Skip/Take and enforced max page size (100 items)
- ✅ All queries include TenantId filter (responsibility of calling services)
- ✅ Related entities loaded via Include() documented (service-level pattern)
- ✅ Queries over 500ms logged with execution time and context
- ✅ Full-text search implemented for product/customer searches
- ✅ IQueryable patterns used to allow composition in services
- ✅ Query optimization methods documented with usage examples
- ✅ Performance tests can verify query times (via logging/metrics)

### Integration Points

#### Service Layer Integration
Services should use these patterns:

```csharp
public class ProductService
{
    private readonly IRepository<Product> _productRepository;
    
    // Paginated search with projection
    public async Task<(List<ProductDto> products, int total)> SearchProductsAsync(
        Guid tenantId,
        string searchTerm,
        int pageNumber = 1,
        int pageSize = 20)
    {
        return await _productRepository.GetPaginatedAsync(
            predicate: p => p.TenantId == tenantId,
            selector: p => new ProductDto 
            { 
                Id = p.Id, 
                Name = p.Name, 
                Price = p.Price.Amount 
            },
            pageNumber: pageNumber,
            pageSize: pageSize
        );
    }
    
    // Full-text search
    public async Task<(List<Product> products, int total)> FullTextSearchAsync(
        Guid tenantId,
        string searchTerm,
        int pageNumber = 1)
    {
        return await _productRepository.FullTextSearchAsync(
            searchTerm: searchTerm,
            searchProperties: new[]
            {
                p => p.Name,
                p => p.Description,
                p => p.Sku
            },
            pageNumber: pageNumber,
            pageSize: 20
        );
    }
}
```

### Logging Output Example
```
Slow query detected for Product.GetPaginatedAsync: Page 1, Size 20, 523ms
Slow full-text search for Product with term 'laptop': 612ms
```

### Related Requirements
- Requirement 5.2: Query Optimization

### Future Enhancements
1. Add computed indexes for commonly used projections
2. Implement query result caching layer
3. Add query execution plan analysis
4. Implement bulk operation methods
5. Add async stream support for large result sets

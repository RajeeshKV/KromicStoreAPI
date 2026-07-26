# KromicStore - Multi-Tenant SaaS E-Commerce Platform

A production-ready .NET 8 solution scaffold for a multi-tenant SaaS e-commerce platform built with Clean Architecture principles.

## Solution Overview

This solution demonstrates enterprise-level architecture patterns with clear separation of concerns, testability, and scalability.

### Project Structure

```
KromicStore.sln
├── src/
│   ├── KromicStore.API              # ASP.NET Core Web API
│   ├── KromicStore.Application      # Business logic & DTOs
│   ├── KromicStore.Domain           # Domain entities & value objects
│   └── KromicStore.Infrastructure   # Data access & external services
├── tests/
│   └── KromicStore.Tests            # Unit & integration tests
├── Directory.Build.props             # Common project settings
└── .gitignore                        # Git ignore rules
```

## Projects

### 1. **KromicStore.Domain** (Class Library)
Core domain layer with pure business logic and no external dependencies.

**Contents:**
- **Entities**: `Tenant`, `User`, `Product`, `Customer`, `Order`, `OrderItem`
- **Value Objects**: `Money`, `Address`
- **Enums**: `UserRole`, `OrderStatus`, `PaymentStatus`, `ProductStatus`

**Key Features:**
- Aggregate pattern implementation
- Value object pattern for `Money` and `Address`
- Strong typing with immutable value objects
- Rich domain models with business logic

### 2. **KromicStore.Application** (Class Library)
Application layer containing business rules and service orchestration.

**Contents:**
- **DTOs**: Request/response data transfer objects
- **Interfaces**: Service contracts (`IAuthService`, `IPaymentService`, `IMediaService`, etc.)
- **Exceptions**: Custom exception types (`DomainException`, `ValidationException`)
- **Validators**: FluentValidation rules for all inputs
- **Mappers**: Entity-to-DTO conversion patterns (can be extended with AutoMapper)

**Key Features:**
- MediatR command/query handlers pattern support
- Fluent validation for robust input validation
- Clear service contracts
- SOLID principle adherence

### 3. **KromicStore.Infrastructure** (Class Library)
Infrastructure layer handling data access, external services, and cross-cutting concerns.

**Contents:**
- **Data Access**:
  - `AppDbContext`: EF Core DbContext with entity configurations
  - `Repository<T>`: Generic repository implementation
  - `UnitOfWork`: Transaction management pattern
- **Services**:
  - `TenantProvider`: Multi-tenancy context resolution
  - `CacheService`: Redis-based caching
  - Stubs for: `AuthService`, `PaymentService`, `NotificationService`, `MediaService`

**Key Features:**
- Entity Framework Core 8 with Npgsql for PostgreSQL
- Repository pattern for data access
- Unit of Work for transaction management
- Redis caching infrastructure
- Multi-tenant support with tenant resolution middleware

### 4. **KromicStore.API** (ASP.NET Core Web API)
API layer exposing endpoints and handling HTTP concerns.

**Contents:**
- **Controllers**: Base controller with tenant resolution
- **Configuration**: Service registration and middleware setup
- **Swagger**: OpenAPI documentation
- **Logging**: Serilog integration

**Key Features:**
- Dependency Injection container setup
- JWT Bearer authentication placeholder
- Swagger/OpenAPI integration
- Database migration on startup
- CORS configuration for multi-tenant scenarios
- Comprehensive logging with Serilog

### 5. **KromicStore.Tests** (xUnit Test Project)
Comprehensive test suite covering domain logic and data access.

**Contents:**
- **Unit Tests**: Domain entities and value objects
- **Integration Tests**: Repository and database operations
- **Validator Tests**: FluentValidation rules
- **Test Fixtures**: Reusable test helpers

**Test Coverage:**
- 25 passing tests
- Entity state transitions
- Business rule validation
- Repository CRUD operations
- Validator rule validation

## Technology Stack

### Core Framework
- **.NET 8** - Latest LTS framework
- **C# 12** - Latest language features
- **ASP.NET Core 8** - Web API framework

### Data Access
- **Entity Framework Core 8** - ORM
- **Npgsql** - PostgreSQL provider
- **PostgreSQL** - Database (configurable)

### Business Logic
- **MediatR** - CQRS and mediator pattern
- **FluentValidation** - Input validation
- **StackExchange.Redis** - Caching

### Infrastructure
- **Hangfire** - Background job scheduling
- **Serilog** - Structured logging
- **Swashbuckle** - Swagger/OpenAPI

### Authentication & Security
- **System.IdentityModel.Tokens.Jwt** - JWT token handling
- **Microsoft.AspNetCore.Authentication.JwtBearer** - JWT Bearer authentication

### External Services (Stubs)
- **Razorpay** - Payment gateway (integration ready)
- **Google.Apis.Auth** - OAuth integration
- **CloudinaryDotNet** - Media management
- **Brevo** - Email notifications

### Testing
- **xUnit** - Test framework
- **Moq** - Mocking library
- **Testcontainers** - Container-based integration tests

## Getting Started

### Prerequisites
- **.NET 8 SDK** or later
- **PostgreSQL 12** or later (for production)
- **Redis** (optional, for caching)
- **Visual Studio 2022** or **VS Code**

### Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd KromicStore
   ```

2. **Update connection string**
   Edit `src/KromicStore.API/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=kromicstore;Username=postgres;Password=your_password"
   }
   ```

3. **Restore dependencies**
   ```bash
   dotnet restore
   ```

4. **Run tests**
   ```bash
   dotnet test
   ```

5. **Build the solution**
   ```bash
   dotnet build
   ```

6. **Run the API**
   ```bash
   cd src/KromicStore.API
   dotnet run
   ```

   API will be available at: `https://localhost:5001` or `http://localhost:5000`

### Database Setup

Migrations are applied automatically on API startup. To create migrations manually:

```bash
cd src/KromicStore.API
dotnet ef migrations add <MigrationName> --project ../KromicStore.Infrastructure
dotnet ef database update --project ../KromicStore.Infrastructure
```

## Architecture Patterns

### Clean Architecture
- **Domain Layer**: Pure business logic, no dependencies
- **Application Layer**: Use cases and application services
- **Infrastructure Layer**: External services and data access
- **Presentation Layer**: API endpoints and HTTP concerns

### Design Patterns Used
- **Repository Pattern**: Data access abstraction
- **Unit of Work Pattern**: Transaction management
- **Dependency Injection**: IoC container for loose coupling
- **Value Objects**: Immutable objects for domain concepts
- **Aggregates**: Entity aggregation for consistency
- **CQRS**: Command Query Responsibility Segregation ready
- **Mediator**: Request/response decoupling with MediatR

## Multi-Tenancy

The solution supports multi-tenancy with:
- **Tenant Resolution**: Via headers or subdomains
- **Data Isolation**: Tenant ID in all queries
- **Service Scoping**: Per-tenant service instances

Tenant context is accessible via `ITenantProvider`:
```csharp
var tenantId = tenantProvider.TenantId;
```

## Configuration

Key configuration sections in `appsettings.json`:

- **ConnectionStrings**: Database connection
- **Auth**: JWT and authentication settings
- **Redis**: Cache connection
- **ExternalServices**: Third-party API credentials
- **Hangfire**: Background job settings

## API Endpoints

### Health Check
```
GET /api/health
```

Response:
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "version": "1.0.0"
}
```

Additional controllers are ready to be implemented:
- `/api/auth` - Authentication endpoints
- `/api/products` - Product catalog
- `/api/orders` - Order management
- `/api/customers` - Customer management
- `/api/platform` - Platform administration

## Logging

Serilog is configured to log to:
- **Console**: Real-time output during development
- **File**: `logs/` directory with daily rolling files

Configure log level in `appsettings.json`:
```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning"
  }
}
```

## Security Considerations

1. **Input Validation**: All inputs validated with FluentValidation
2. **Authentication**: JWT Bearer token support
3. **Authorization**: Role-based access control ready
4. **Data Isolation**: Tenant-scoped data queries
5. **Secrets**: Configuration-based credential management

**Production Checklist:**
- [ ] Change default JWT secret key
- [ ] Enable HTTPS only
- [ ] Configure CORS properly
- [ ] Set up environment-specific secrets
- [ ] Enable rate limiting
- [ ] Configure firewall rules
- [ ] Audit external service integrations

## Testing

Run all tests:
```bash
dotnet test
```

Run specific test project:
```bash
dotnet test tests/KromicStore.Tests
```

Run with coverage (requires coverage tool):
```bash
dotnet test /p:CollectCoverage=true
```

## Extending the Solution

### Adding a New Entity

1. **Domain Layer**: Create entity in `Domain/Entities/`
2. **Application Layer**: Create DTOs in `Application/DTOs/`
3. **Infrastructure Layer**: Add `DbSet<T>` to `AppDbContext`
4. **API Layer**: Create controller in `Controllers/`

### Adding a New Service

1. **Application Layer**: Define interface in `Application/Interfaces/`
2. **Infrastructure Layer**: Implement service in `Infrastructure/Services/`
3. **API Layer**: Register in `Program.cs`

### Adding Validators

1. Create validator class extending `AbstractValidator<T>`
2. Register in `Program.cs` using:
   ```csharp
   builder.Services.AddValidatorsFromAssemblyContaining(typeof(YourValidator));
   ```

## Common Scenarios

### Implementing an API Endpoint
```csharp
[HttpGet("{id}")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
{
    var product = await _unitOfWork.Products.GetByIdAsync(id);
    if (product == null)
        return NotFound();
    
    return Ok(_mapper.Map<ProductDto>(product));
}
```

### Using the Repository
```csharp
var products = await _unitOfWork.Products.FindAsync(p => p.Status == ProductStatus.Active);
var count = await _unitOfWork.Products.CountAsync();
```

### Creating a Domain Entity
```csharp
var product = Product.Create(
    tenantId,
    "SKU-001",
    "Product Name",
    "Description",
    new Money(999.99m),
    100
);

product.Publish();
await _unitOfWork.Products.AddAsync(product);
await _unitOfWork.SaveChangesAsync();
```

## Troubleshooting

### Database Connection Issues
- Verify PostgreSQL is running
- Check connection string in `appsettings.json`
- Ensure database user has proper permissions

### Migration Errors
- Clear `Migrations` folder and start fresh
- Verify Entity Framework Core is installed
- Check for conflicting entity configurations

### Test Failures
- Ensure all NuGet packages are restored
- Clear `bin` and `obj` directories
- Rebuild solution

## Performance Considerations

1. **Caching**: Redis for frequently accessed data
2. **Pagination**: Implement for large datasets
3. **Indexing**: Database indexes on common queries
4. **Async/Await**: All I/O operations are async
5. **Lazy Loading**: Configure as needed in EF Core

## Deployment

### Docker
Create a `Dockerfile` for containerization:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "KromicStore.API.dll"]
```

### Azure / AWS / GCP
- Use managed PostgreSQL database services
- Deploy API as container or app service
- Use managed Redis for caching
- Configure auto-scaling

## Contributing

1. Follow Clean Architecture principles
2. Write unit tests for new features
3. Maintain SOLID principles
4. Document public APIs
5. Use consistent naming conventions

## License

[Add your license here]

## Support

For issues or questions:
- Check existing documentation
- Review test cases for usage examples
- Create an issue with detailed information

---

**Built with .NET 8 | Clean Architecture | Enterprise Patterns**

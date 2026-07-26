# Getting Started with KromicStore

## 5-Minute Quick Start

### Prerequisites
- **[.NET 8 SDK](https://dotnet.microsoft.com/download)** - Latest LTS framework
- **[PostgreSQL 12+](https://www.postgresql.org/download/)** - Database server
- **[Visual Studio 2022](https://visualstudio.microsoft.com/)** or **[VS Code](https://code.visualstudio.com/)** - Code editor

### Step 1: Clone or Extract the Solution
```bash
cd KromicStore
```

### Step 2: Verify .NET Installation
```bash
dotnet --version
# Should output: 8.0.x or higher
```

### Step 3: Restore NuGet Packages
```bash
dotnet restore
```

### Step 4: Run Tests (Verify Setup)
```bash
dotnet test
# Should output: Passed!  - Failed: 0, Passed: 25
```

### Step 5: Build the Solution
```bash
dotnet build
# Should output: Build succeeded. 0 Error(s)
```

### Step 6: Run the API
```bash
cd src/KromicStore.API
dotnet run
```

The API will start on `https://localhost:7001` (or `http://localhost:5000`)

### Step 7: Test the Health Endpoint
```bash
curl https://localhost:7001/api/health
# Or open in browser: https://localhost:7001/api/health
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "version": "1.0.0"
}
```

## Configuration

### Database Setup

Edit `src/KromicStore.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=kromicstore;Username=postgres;Password=your_password"
  }
}
```

**PostgreSQL Connection String Format:**
```
Host=<server>;Port=<port>;Database=<database>;Username=<user>;Password=<password>
```

**Example for PostgreSQL on localhost:**
```
Host=localhost;Port=5432;Database=kromicstore_dev;Username=postgres;Password=password
```

### Development Environment Setup

1. **Ensure PostgreSQL is running:**
   ```bash
   # Windows
   pg_ctl -D "C:\Program Files\PostgreSQL\data" start
   
   # macOS (if installed via Homebrew)
   brew services start postgresql
   
   # Linux
   sudo systemctl start postgresql
   ```

2. **Create database (optional - will auto-create on migration):**
   ```bash
   psql -U postgres -c "CREATE DATABASE kromicstore_dev;"
   ```

3. **Run in development mode:**
   ```bash
   dotnet run --environment Development
   ```

## Project Organization

```
src/
├── KromicStore.API              ← Start here for API endpoints
├── KromicStore.Application      ← Business logic & DTOs
├── KromicStore.Domain           ← Core entities
└── KromicStore.Infrastructure   ← Data access & services

tests/
└── KromicStore.Tests            ← Unit & integration tests
```

## Understanding the Architecture

### The Clean Architecture Layers

```
┌─────────────────────────────────────────────────┐
│  API Layer (HTTP, Controllers, Middleware)      │
│  ↓ Routes requests to                          │
├─────────────────────────────────────────────────┤
│  Application Layer (Business Logic, DTOs)       │
│  ↓ Orchestrates domain objects using           │
├─────────────────────────────────────────────────┤
│  Domain Layer (Entities, Value Objects)         │
│  ↓ Persisted by                                 │
├─────────────────────────────────────────────────┤
│  Infrastructure Layer (Data Access, Services)   │
└─────────────────────────────────────────────────┘
```

### Example: Product Creation Flow

1. **API Layer** receives POST request to `/api/products`
2. **Controller** validates with `CreateProductRequest` DTO
3. **Validator** ensures all fields are valid (FluentValidation)
4. **Product Entity** is created with business rules
5. **Repository** persists to database
6. **Response** DTO is returned

## Common Tasks

### Add a New Domain Entity

**1. Create the entity** (`src/KromicStore.Domain/Entities/YourEntity.cs`):
```csharp
public class YourEntity : BaseEntity
{
    public string Name { get; private set; }
    public Guid TenantId { get; private set; }
    
    public static YourEntity Create(Guid tenantId, string name)
    {
        return new YourEntity { TenantId = tenantId, Name = name };
    }
}
```

**2. Add to DbContext** (`src/KromicStore.Infrastructure/Data/AppDbContext.cs`):
```csharp
public DbSet<YourEntity> YourEntities { get; set; } = null!;
```

**3. Add to Unit of Work** (interfaces and implementation)

**4. Create API controller**

**5. Write tests**

### Add Validation Rules

Create a validator (`src/KromicStore.Application/Validators/YourValidator.cs`):
```csharp
public class CreateYourEntityRequestValidator : AbstractValidator<CreateYourEntityRequest>
{
    public CreateYourEntityRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");
    }
}
```

Register it in `Program.cs`:
```csharp
builder.Services.AddValidatorsFromAssemblyContaining(typeof(CreateYourEntityRequestValidator));
```

### Add a Background Job

Edit `src/KromicStore.API/Program.cs`:
```csharp
// Register Hangfire
builder.Services.AddHangfire(config => config.UsePostgreSqlStorage(connectionString));
builder.Services.AddHangfireServer();

// In app setup:
app.UseHangfireDashboard();
```

### Add Caching

Inject `ICacheService`:
```csharp
public async Task<ProductDto> GetProductAsync(Guid id)
{
    var cacheKey = $"product:{id}";
    var cached = await _cacheService.GetAsync<ProductDto>(cacheKey);
    if (cached != null) return cached;
    
    var product = await _unitOfWork.Products.GetByIdAsync(id);
    var dto = _mapper.Map<ProductDto>(product);
    
    await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(30));
    return dto;
}
```

## Debugging

### Enable Detailed Logging

Edit `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Debug",
      "Microsoft.AspNetCore": "Debug"
    }
  }
}
```

### View Database Schema

```bash
# Connect to PostgreSQL
psql -U postgres -d kromicstore -c "\dt"

# View specific table
psql -U postgres -d kromicstore -c "\d+ products"
```

### Run Tests with Verbose Output

```bash
dotnet test --verbosity detailed
```

## Troubleshooting

### Problem: "Unable to connect to database"

**Solution:**
1. Verify PostgreSQL is running
2. Check connection string in `appsettings.json`
3. Ensure database exists or let migration create it

### Problem: "Entity Framework migrations not found"

**Solution:**
```bash
cd src/KromicStore.API
dotnet ef migrations add InitialCreate --project ../KromicStore.Infrastructure
dotnet ef database update --project ../KromicStore.Infrastructure
```

### Problem: "Build failed - CS0234 namespace not found"

**Solution:**
```bash
dotnet clean
dotnet restore
dotnet build
```

### Problem: "Tests failing with InMemoryDatabase"

**Solution:**
Ensure `Microsoft.EntityFrameworkCore.InMemory` is installed:
```bash
dotnet add tests/KromicStore.Tests package Microsoft.EntityFrameworkCore.InMemory
```

## Project File Organization

### Key Files to Know

| File | Purpose |
|------|---------|
| `KromicStore.sln` | Solution file - open this in Visual Studio |
| `Directory.Build.props` | Shared build settings for all projects |
| `src/KromicStore.API/Program.cs` | Application startup & configuration |
| `src/KromicStore.API/appsettings.json` | Configuration for production |
| `src/KromicStore.API/appsettings.Development.json` | Configuration overrides for development |

### Important Classes

| Class | Location | Purpose |
|-------|----------|---------|
| `AppDbContext` | Infrastructure/Data | Entity Framework context |
| `BaseEntity` | Domain/Entities | Base class for all domain entities |
| `IUnitOfWork` | Application/Interfaces | Data access transaction management |
| `Repository<T>` | Infrastructure/Data | Generic data access implementation |

## Next Steps

1. **Explore the code** - Read through the domain entities to understand the model
2. **Add your first entity** - Create a new domain entity and add it to the database
3. **Create API endpoint** - Implement a controller with CRUD operations
4. **Write tests** - Add unit tests for your business logic
5. **Deploy** - Configure for your target environment

## Resources

- [Clean Architecture in .NET](https://docs.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Best Practices](https://docs.microsoft.com/en-us/aspnet/core/?view=aspnetcore-8.0)
- [FluentValidation Guide](https://docs.fluentvalidation.net/)

## Support & Documentation

- **Architecture Questions** → Read `SOLUTION_STRUCTURE.md`
- **How to extend** → See `README.md` "Extending the Solution" section
- **Code examples** → Check `tests/` folder for usage patterns

## Next: Running Your First Test

```bash
dotnet test --filter "MoneyTests"
```

This will run the Money value object tests and verify your setup is working!

---

**Ready to build? Start by exploring the Domain entities in `src/KromicStore.Domain/Entities/`**

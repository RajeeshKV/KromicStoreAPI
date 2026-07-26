# KromicStore Solution Structure

## Directory Tree

```
KromicStore/
├── src/
│   ├── KromicStore.API/
│   │   ├── Controllers/
│   │   │   ├── BaseController.cs
│   │   │   └── HealthController.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── Program.cs
│   │   └── KromicStore.API.csproj
│   │
│   ├── KromicStore.Application/
│   │   ├── DTOs/
│   │   │   ├── AuthDTOs.cs
│   │   │   ├── ProductDTOs.cs
│   │   │   ├── OrderDTOs.cs
│   │   │   └── CustomerDTOs.cs
│   │   ├── Exceptions/
│   │   │   ├── DomainException.cs
│   │   │   └── ValidationException.cs
│   │   ├── Interfaces/
│   │   │   ├── IRepository.cs
│   │   │   ├── IUnitOfWork.cs
│   │   │   ├── IAuthService.cs
│   │   │   ├── ITenantProvider.cs
│   │   │   ├── ICacheService.cs
│   │   │   ├── INotificationService.cs
│   │   │   ├── IPaymentService.cs
│   │   │   └── IMediaService.cs
│   │   ├── Validators/
│   │   │   ├── LoginRequestValidator.cs
│   │   │   ├── RegisterRequestValidator.cs
│   │   │   └── CreateProductRequestValidator.cs
│   │   └── KromicStore.Application.csproj
│   │
│   ├── KromicStore.Domain/
│   │   ├── Entities/
│   │   │   ├── BaseEntity.cs
│   │   │   ├── Tenant.cs
│   │   │   ├── User.cs
│   │   │   ├── Product.cs
│   │   │   ├── Customer.cs
│   │   │   ├── Order.cs
│   │   │   └── OrderItem.cs
│   │   ├── ValueObjects/
│   │   │   ├── Money.cs
│   │   │   └── Address.cs
│   │   ├── Enums/
│   │   │   ├── UserRole.cs
│   │   │   ├── OrderStatus.cs
│   │   │   ├── PaymentStatus.cs
│   │   │   └── ProductStatus.cs
│   │   └── KromicStore.Domain.csproj
│   │
│   └── KromicStore.Infrastructure/
│       ├── Data/
│       │   ├── AppDbContext.cs
│       │   ├── Repository.cs
│       │   └── UnitOfWork.cs
│       ├── Services/
│       │   ├── TenantProvider.cs
│       │   └── CacheService.cs
│       └── KromicStore.Infrastructure.csproj
│
├── tests/
│   └── KromicStore.Tests/
│       ├── Unit/
│       │   ├── Domain/
│       │   │   ├── ValueObjects/
│       │   │   │   └── MoneyTests.cs
│       │   │   └── Entities/
│       │   │       ├── ProductTests.cs
│       │   │       └── OrderTests.cs
│       │   └── Application/
│       │       └── Validators/
│       │           └── LoginRequestValidatorTests.cs
│       ├── Integration/
│       │   └── Data/
│       │       └── RepositoryTests.cs
│       └── KromicStore.Tests.csproj
│
├── .gitignore
├── Directory.Build.props
├── KromicStore.sln
├── README.md
└── SOLUTION_STRUCTURE.md
```

## File Organization by Layer

### Domain Layer (KromicStore.Domain)
No external dependencies. Pure business logic.

| Folder | Purpose |
|--------|---------|
| `Entities/` | Domain entities with business rules |
| `ValueObjects/` | Immutable value objects (Money, Address) |
| `Enums/` | Domain enumerations |

### Application Layer (KromicStore.Application)
Depends on Domain. Contains business logic orchestration.

| Folder | Purpose |
|--------|---------|
| `DTOs/` | Data transfer objects for API contracts |
| `Interfaces/` | Service contracts and abstractions |
| `Validators/` | FluentValidation rules |
| `Exceptions/` | Application-level exceptions |

### Infrastructure Layer (KromicStore.Infrastructure)
Depends on Domain and Application. External service implementations.

| Folder | Purpose |
|--------|---------|
| `Data/` | EF Core context, repository, unit of work |
| `Services/` | External service implementations |

### API Layer (KromicStore.API)
Depends on all layers. HTTP endpoint handlers.

| Folder | Purpose |
|--------|---------|
| `Controllers/` | API endpoint definitions |

### Test Layer (KromicStore.Tests)
Tests for all layers.

| Folder | Purpose |
|--------|---------|
| `Unit/` | Unit tests for individual components |
| `Integration/` | Integration tests with infrastructure |

## Key Files

### Configuration & Setup
- **Directory.Build.props** - Common project properties and versions
- **KromicStore.sln** - Solution file grouping all projects
- **.gitignore** - Git ignore rules for C# projects
- **README.md** - Complete documentation
- **SOLUTION_STRUCTURE.md** - This file

### Program Startup (API)
- **Program.cs** - Service registration and middleware configuration
- **appsettings.json** - Production configuration
- **appsettings.Development.json** - Development overrides

## Entity Relationships

```
Tenant (1) ──┬─── (N) User
             ├─── (N) Product
             ├─── (N) Customer
             └─── (N) Order ──┬─── (N) OrderItem
                               └─── (1) Customer

User (1) ───────── (N) Order
Order (1) ───────── (N) OrderItem
Product (1) ───────── (N) OrderItem
```

## Dependencies Between Layers

```
┌─────────────────────────────────┐
│      API Layer (Controllers)     │
└────────────────┬────────────────┘
                 │ depends on
┌────────────────▼────────────────┐
│   Application Layer (DTOs,       │
│   Validators, Exceptions)        │
└────────────────┬────────────────┘
                 │ depends on
┌────────────────┬────────────────┐
│  Domain Layer  │ Infrastructure  │
│  (Entities,    │  Layer (Data,   │
│  Values)       │  Services)      │
└────────────────┴────────────────┘
```

## Class Responsibilities

### Domain Entities
- Represent business concepts
- Contain business rules and logic
- No knowledge of persistence
- Immutable value objects

### DTOs
- Transfer data between layers
- API request/response contracts
- No business logic
- Can be partial representations

### Validators
- Input validation rules
- FluentValidation conventions
- Reusable validation logic
- Dependency injection support

### Repositories
- Data access abstraction
- Query by predicate
- CRUD operations
- Transaction management via Unit of Work

### Services
- Cross-cutting concerns
- External service integration
- Business logic orchestration
- Loosely coupled via interfaces

## Service Lifetimes

| Service | Lifetime |
|---------|----------|
| `IUnitOfWork` | Scoped (per request) |
| `IRepository<T>` | Scoped (per request) |
| `ITenantProvider` | Scoped (per request) |
| `ICacheService` | Singleton (shared) |
| `IAuthService` | Scoped (per request) |
| `IPaymentService` | Scoped (per request) |

## Testing Strategy

### Unit Tests
- Test domain entities and value objects
- Fast execution
- No external dependencies
- Located in `Unit/` folders

### Integration Tests
- Test data access and repositories
- Use in-memory database
- Test database operations
- Located in `Integration/` folders

### Test Structure
```
ClassName + "Tests"
├── [Fact] method with Given-When-Then structure
├── Arrange - Setup test data
├── Act - Execute method under test
└── Assert - Verify results
```

## NuGet Packages by Layer

### Domain
- None (pure C#)

### Application
- MediatR
- FluentValidation

### Infrastructure
- Microsoft.EntityFrameworkCore
- Npgsql.EntityFrameworkCore.PostgreSQL
- StackExchange.Redis
- Hangfire

### API
- Swashbuckle.AspNetCore
- Serilog.AspNetCore
- Microsoft.AspNetCore.Authentication.JwtBearer

### Tests
- xunit
- Moq
- Microsoft.EntityFrameworkCore.InMemory

## Folder Naming Conventions

- **Entities** - Domain objects with identity
- **ValueObjects** - Immutable domain objects
- **DTOs** - Data transfer objects
- **Services** - Business logic services
- **Interfaces** - Service contracts
- **Controllers** - API endpoints
- **Validators** - Input validation rules
- **Exceptions** - Custom exception types
- **Unit** - Unit tests
- **Integration** - Integration tests

## Quick Navigation

### To add a new feature:
1. Define Domain Entity → `Domain/Entities/`
2. Create DTO → `Application/DTOs/`
3. Add Validator → `Application/Validators/`
4. Implement Repository → in `Infrastructure/Data/`
5. Create Controller → `API/Controllers/`
6. Write Tests → `Tests/`

### To add a new service:
1. Define Interface → `Application/Interfaces/`
2. Implement Service → `Infrastructure/Services/`
3. Register in Program.cs
4. Inject into controllers/handlers

### To modify the database:
1. Update Entity in `Domain/Entities/`
2. Update AppDbContext in `Infrastructure/Data/`
3. Create migration
4. Run migration

---

**Total Structure: 50+ files organized in clean architecture layers**

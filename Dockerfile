# Multi-stage Dockerfile for KromicStore API
# Build stage: Compile the application using .NET SDK
# Runtime stage: Run the application using ASP.NET Core runtime (Alpine for minimal image size)

# ============================================
# STAGE 1: BUILD
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder

WORKDIR /src

# Copy solution and project files
COPY ["KromicStore.sln", "."]
COPY ["Directory.Build.props", "."]
COPY ["src/KromicStore.API/KromicStore.API.csproj", "src/KromicStore.API/"]
COPY ["src/KromicStore.Domain/KromicStore.Domain.csproj", "src/KromicStore.Domain/"]
COPY ["src/KromicStore.Application/KromicStore.Application.csproj", "src/KromicStore.Application/"]
COPY ["src/KromicStore.Infrastructure/KromicStore.Infrastructure.csproj", "src/KromicStore.Infrastructure/"]
COPY ["src/KromicStore.Contracts/KromicStore.Contracts.csproj", "src/KromicStore.Contracts/"]
COPY ["tests/KromicStore.Tests/KromicStore.Tests.csproj", "tests/KromicStore.Tests/"]

# Restore NuGet packages
RUN dotnet restore "KromicStore.sln"

# Copy source code
COPY . .

# Build the application in Release configuration
RUN dotnet build "KromicStore.sln" --configuration Release --no-restore

# Publish the application
RUN dotnet publish "src/KromicStore.API/KromicStore.API.csproj" \
    --configuration Release \
    --no-build \
    --output /app/publish

# ============================================
# STAGE 2: RUNTIME
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine

# Install curl for health checks and PostgreSQL client tools
RUN apk add --no-cache curl postgresql-client

WORKDIR /app

# Copy published application from builder
COPY --from=builder /app/publish .

# Copy startup script
COPY ["scripts/entrypoint.sh", "/app/entrypoint.sh"]
RUN chmod +x /app/entrypoint.sh

# Set environment variables
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check: Verify application is running and database is accessible
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:${PORT:-8080}/health || exit 1

# Expose default port (configurable via PORT environment variable)
EXPOSE 8080

# Run the application via startup script (handles migrations, database setup, etc.)
ENTRYPOINT ["/app/entrypoint.sh"]

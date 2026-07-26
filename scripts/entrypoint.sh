#!/bin/bash
set -e

echo "[entrypoint] Starting KromicStore API application..."

# Validate required environment variables
if [ -z "$DATABASE_URL" ]; then
    echo "[entrypoint] ERROR: DATABASE_URL environment variable is not set"
    exit 1
fi

if [ -z "$ASPNETCORE_ENVIRONMENT" ]; then
    echo "[entrypoint] WARNING: ASPNETCORE_ENVIRONMENT not set, defaulting to Production"
    export ASPNETCORE_ENVIRONMENT=Production
fi

echo "[entrypoint] Environment: $ASPNETCORE_ENVIRONMENT"

# Extract database host and port from DATABASE_URL
# Expected format: postgresql://user:password@host:port/database
DB_HOST=$(echo "$DATABASE_URL" | sed -E 's|.*@([^:]+):.*|\1|')
DB_PORT=$(echo "$DATABASE_URL" | sed -E 's|.*:([0-9]+)/.*|\1|')

# Default to standard PostgreSQL port if not specified
DB_PORT=${DB_PORT:-5432}

echo "[entrypoint] Waiting for PostgreSQL database at $DB_HOST:$DB_PORT..."

# Retry logic: attempt to connect 30 times with 2-second sleep between attempts
MAX_RETRIES=30
RETRY_COUNT=0
while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
    if pg_isready -h "$DB_HOST" -p "$DB_PORT" -U postgres &>/dev/null; then
        echo "[entrypoint] Database is ready!"
        break
    fi
    
    RETRY_COUNT=$((RETRY_COUNT + 1))
    if [ $RETRY_COUNT -eq $MAX_RETRIES ]; then
        echo "[entrypoint] ERROR: Failed to connect to database after $MAX_RETRIES attempts"
        exit 1
    fi
    
    echo "[entrypoint] Database not ready yet. Retry $RETRY_COUNT/$MAX_RETRIES..."
    sleep 2
done

# Execute database migrations
echo "[entrypoint] Executing database migrations..."

# Run Entity Framework Core migrations
if dotnet ef database update --project src/KromicStore.Infrastructure --startup-project src/KromicStore.API 2>&1; then
    echo "[entrypoint] Migrations completed successfully"
else
    MIGRATION_EXIT_CODE=$?
    echo "[entrypoint] ERROR: Database migrations failed with exit code $MIGRATION_EXIT_CODE"
    exit 1
fi

echo "[entrypoint] Application startup initialization complete"
echo "[entrypoint] Starting KromicStore API application..."

# Start the ASP.NET Core application
exec dotnet src/KromicStore.API/bin/Release/net8.0/KromicStore.API.dll

#!/bin/sh
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
    if nc -z "$DB_HOST" "$DB_PORT" 2>/dev/null; then
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

# Run Entity Framework Core migrations using dotnet ef
dotnet ef database update --no-build 2>&1 || {
    MIGRATION_EXIT_CODE=$?
    echo "[entrypoint] WARNING: Database migrations may have failed or already applied (exit code: $MIGRATION_EXIT_CODE)"
    # Don't exit - migrations might be already applied or DB might be in valid state
}

echo "[entrypoint] Application startup initialization complete"
echo "[entrypoint] Starting KromicStore API application..."

# Start the ASP.NET Core application
exec dotnet KromicStore.API.dll

#!/usr/bin/env bash
set -euo pipefail
echo "Running EF Core database migrations..."
dotnet KromicStore.API.dll --migrate-only
echo "EF Core database migrations completed."
echo "Starting KromicStore API application..."
dotnet KromicStore.API.dll

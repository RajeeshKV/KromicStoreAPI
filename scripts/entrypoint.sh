#!/usr/bin/env bash
set -euo pipefail
echo "Starting KromicStore API application (migrations run automatically on startup)..."
dotnet KromicStore.API.dll

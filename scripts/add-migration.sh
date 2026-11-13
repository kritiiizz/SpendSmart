#!/usr/bin/env bash
# Usage: ./scripts/add-migration.sh InitialCreate
set -euo pipefail
name="${1:?Usage: $0 <MigrationName>}"
dotnet tool restore
dotnet ef migrations add "$name" \
  --project src/SpendSmart.Infrastructure \
  --startup-project src/SpendSmart.Web \
  --output-dir Persistence/Migrations

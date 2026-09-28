#!/usr/bin/env bash
# Gera uma migration do ApplicationDbContext.  Uso:  ./scripts/add-migration.sh Initial
set -euo pipefail
NAME="${1:?informe o nome da migration, ex.: Initial}"
dotnet tool update --global dotnet-ef --version '9.*' >/dev/null
dotnet ef migrations add "$NAME" \
  --project backend/src/FieldFlow.Infrastructure \
  --startup-project backend/src/FieldFlow.Api \
  --context ApplicationDbContext \
  --output-dir Persistence/Migrations

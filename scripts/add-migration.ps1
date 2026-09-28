# Gera uma migration do ApplicationDbContext.  Uso:  ./scripts/add-migration.ps1 Initial
param([Parameter(Mandatory = $true)][string]$Name)
$ErrorActionPreference = "Stop"
dotnet tool update --global dotnet-ef --version 9.* | Out-Null
dotnet ef migrations add $Name `
  --project backend/src/FieldFlow.Infrastructure `
  --startup-project backend/src/FieldFlow.Api `
  --context ApplicationDbContext `
  --output-dir Persistence/Migrations

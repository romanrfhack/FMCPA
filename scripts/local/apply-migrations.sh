#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

print_local_convention
echo "Preparando SQL Server local y tooling .NET..."

docker_compose_cmd up -d "${FMCPA_SQL_SERVICE_NAME}"
wait_for_sqlserver
ensure_storage_directories
ensure_local_state_dir

cd "${ROOT_DIR}"

dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln
ensure_dotnet_local_tools
echo "dotnet-ef local: $(dotnet_ef_version)"
run_dotnet_ef database update \
  --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj \
  --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj \
  --context PlatformDbContext \
  --no-build

echo "Migraciones aplicadas sobre ${FMCPA_DB_NAME}."

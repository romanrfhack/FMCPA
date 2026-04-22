#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

docker_compose_cmd up -d "${FMCPA_SQL_SERVICE_NAME}"
wait_for_sqlserver
ensure_storage_directories
ensure_local_state_dir

cd "${ROOT_DIR}"

print_local_convention
echo "Levantando backend local..."
exec dotnet run --no-build --project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --urls "http://127.0.0.1:${FMCPA_API_PORT}"

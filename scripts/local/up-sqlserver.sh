#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

print_local_convention
echo "Levantando solo SQL Server local..."

docker_compose_cmd up -d "${FMCPA_SQL_SERVICE_NAME}"
wait_for_sqlserver
echo "SQL Server local listo."

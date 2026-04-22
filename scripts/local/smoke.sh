#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

print_local_convention
echo "Ejecutando smoke local basico..."

wait_for_sqlserver

database_probe="$(docker exec "${FMCPA_SQL_CONTAINER_NAME}" /opt/mssql-tools18/bin/sqlcmd -h -1 -W -C -S localhost -U sa -P "${FMCPA_SQL_SA_PASSWORD}" -d "${FMCPA_DB_NAME}" -Q "SET NOCOUNT ON; SELECT DB_NAME() AS CurrentDatabase;")"
health_probe="$(curl -fsS "http://127.0.0.1:${FMCPA_API_PORT}/health")"
dashboard_probe="$(curl -fsS "http://127.0.0.1:${FMCPA_API_PORT}/api/dashboard/summary")"
documents_probe="$(curl -fsS "http://127.0.0.1:${FMCPA_API_PORT}/api/documents/integrity?take=5")"
curl -fsS "http://127.0.0.1:${FMCPA_WEB_PORT}/" >/dev/null

echo "Smoke local OK"
echo "Database: ${database_probe}"
echo "Health: ${health_probe}"
echo "Dashboard summary: ${dashboard_probe}"
echo "Documents integrity: ${documents_probe}"

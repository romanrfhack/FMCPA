#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

API_BASE_URL="http://127.0.0.1:${FMCPA_API_PORT}"

require_auth_bootstrap_password() {
  if [[ -n "${FMCPA_AUTH_BOOTSTRAP_PASSWORD}" ]]; then
    return 0
  fi

  echo "FMCPA_AUTH_BOOTSTRAP_PASSWORD es requerida para ejecutar smoke con autenticacion local." >&2
  return 1
}

build_login_payload() {
  local user_name="${1:?usuario requerido}"
  local password="${2:?password requerida}"

  node -e '
const [userName, password] = process.argv.slice(1);
process.stdout.write(JSON.stringify({ userName, password }));
' "${user_name}" "${password}"
}

login_with_credentials() {
  local user_name="${1:?usuario requerido}"
  local password="${2:?password requerida}"
  curl -fsS -H 'Content-Type: application/json' -X POST -d "$(build_login_payload "${user_name}" "${password}")" "${API_BASE_URL}/api/auth/login"
}

extract_json_value() {
  local json_payload="${1:?json requerido}"
  local expression="${2:?expresion requerida}"

  JSON_QUERY_EXPRESSION="${expression}" node -e '
const fs = require("fs");
const source = fs.readFileSync(0, "utf8");
const data = source.trim() ? JSON.parse(source) : null;
const expression = process.env.JSON_QUERY_EXPRESSION;
const result = Function("data", `return (${expression});`)(data);

if (result === undefined || result === null) {
  process.exit(1);
}

process.stdout.write(typeof result === "object" ? JSON.stringify(result) : String(result));
' <<<"${json_payload}"
}

print_local_convention
echo "Ejecutando smoke local basico..."

wait_for_sqlserver
command_exists node >/dev/null 2>&1 || {
  echo "Node.js es requerido para el smoke autenticado." >&2
  exit 1
}
require_auth_bootstrap_password

database_probe="$(docker exec "${FMCPA_SQL_CONTAINER_NAME}" /opt/mssql-tools18/bin/sqlcmd -h -1 -W -C -S localhost -U sa -P "${FMCPA_SQL_SA_PASSWORD}" -d "${FMCPA_DB_NAME}" -Q "SET NOCOUNT ON; SELECT DB_NAME() AS CurrentDatabase;")"
health_probe="$(curl -fsS "${API_BASE_URL}/health")"
unauthorized_dashboard_status="$(curl -s -o /dev/null -w '%{http_code}' "${API_BASE_URL}/api/dashboard/summary")"

if [[ "${unauthorized_dashboard_status}" != "401" ]]; then
  echo "Se esperaba 401 sin token en /api/dashboard/summary y se obtuvo ${unauthorized_dashboard_status}." >&2
  exit 1
fi

login_response="$(login_with_credentials "${FMCPA_AUTH_BOOTSTRAP_USER_NAME}" "${FMCPA_AUTH_BOOTSTRAP_PASSWORD}")"
access_token="$(extract_json_value "${login_response}" 'data.accessToken')"
session_probe="$(curl -fsS -H "Authorization: Bearer ${access_token}" "${API_BASE_URL}/api/auth/session")"
dashboard_probe="$(curl -fsS -H "Authorization: Bearer ${access_token}" "${API_BASE_URL}/api/dashboard/summary")"
documents_probe="$(curl -fsS -H "Authorization: Bearer ${access_token}" "${API_BASE_URL}/api/documents/integrity?take=5")"
curl -fsS "http://127.0.0.1:${FMCPA_WEB_PORT}/" >/dev/null

readonly_session_probe=""
readonly_contacts_post_status=""

if [[ -n "${FMCPA_AUTH_READONLY_PASSWORD}" ]]; then
  readonly_login_response="$(login_with_credentials "${FMCPA_AUTH_READONLY_USER_NAME}" "${FMCPA_AUTH_READONLY_PASSWORD}")"
  readonly_access_token="$(extract_json_value "${readonly_login_response}" 'data.accessToken')"
  readonly_session_probe="$(curl -fsS -H "Authorization: Bearer ${readonly_access_token}" "${API_BASE_URL}/api/auth/session")"
  readonly_contacts_post_status="$(curl -s -o /dev/null -w '%{http_code}' -H "Authorization: Bearer ${readonly_access_token}" -H 'Content-Type: application/json' -X POST -d '{"name":"Readonly probe","contactTypeId":1}' "${API_BASE_URL}/api/contacts")"

  if [[ "${readonly_contacts_post_status}" != "403" ]]; then
    echo "Se esperaba 403 para READONLY al intentar POST /api/contacts y se obtuvo ${readonly_contacts_post_status}." >&2
    exit 1
  fi
fi

echo "Smoke local OK"
echo "Database: ${database_probe}"
echo "Health: ${health_probe}"
echo "Dashboard summary sin token: HTTP ${unauthorized_dashboard_status}"
echo "Session: ${session_probe}"
echo "Dashboard summary: ${dashboard_probe}"
echo "Documents integrity: ${documents_probe}"

if [[ -n "${readonly_session_probe}" ]]; then
  echo "Readonly session: ${readonly_session_probe}"
  echo "Readonly POST /api/contacts: HTTP ${readonly_contacts_post_status}"
fi

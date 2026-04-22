#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/../.." && pwd)"

if [[ -f "${ROOT_DIR}/.env.local" ]]; then
  set -a
  # shellcheck disable=SC1091
  source "${ROOT_DIR}/.env.local"
  set +a
fi

: "${FMCPA_SQL_SERVICE_NAME:=sqlserver}"
: "${FMCPA_SQL_CONTAINER_NAME:=fmcpa-sql}"
: "${FMCPA_SQL_PORT:=14333}"
: "${FMCPA_SQL_SA_PASSWORD:=Fmcpadev123!Aa}"
: "${FMCPA_DB_NAME:=FMCPA_Development}"
: "${FMCPA_API_PORT:=5080}"
: "${FMCPA_WEB_PORT:=4200}"
: "${FMCPA_STORAGE_ROOT:=App_Data}"
: "${FMCPA_LOCAL_STATE_ROOT:=/tmp/fmcpa-local}"
: "${FMCPA_FRONTEND_PROXY_CONFIG_PATH:=/tmp/fmcpa-angular-proxy.${FMCPA_WEB_PORT}.${FMCPA_API_PORT}.json}"

FMCPA_LOCAL_STACK_KEY="${FMCPA_SQL_CONTAINER_NAME}-${FMCPA_SQL_PORT}-${FMCPA_API_PORT}-${FMCPA_WEB_PORT}-${FMCPA_DB_NAME}"
FMCPA_LOCAL_STATE_DIR="${FMCPA_LOCAL_STATE_ROOT}/${FMCPA_LOCAL_STACK_KEY}"
FMCPA_DOTNET_TOOLS_MANIFEST_PATH="${ROOT_DIR}/.config/dotnet-tools.json"

if [[ "${FMCPA_STORAGE_ROOT}" != /* ]]; then
  FMCPA_STORAGE_ROOT="${ROOT_DIR}/${FMCPA_STORAGE_ROOT}"
fi

export ROOT_DIR
export FMCPA_SQL_SERVICE_NAME
export FMCPA_SQL_CONTAINER_NAME
export FMCPA_SQL_PORT
export FMCPA_SQL_SA_PASSWORD
export FMCPA_DB_NAME
export FMCPA_API_PORT
export FMCPA_WEB_PORT
export FMCPA_STORAGE_ROOT
export FMCPA_LOCAL_STATE_ROOT
export FMCPA_LOCAL_STATE_DIR
export FMCPA_FRONTEND_PROXY_CONFIG_PATH
export FMCPA_DOTNET_TOOLS_MANIFEST_PATH
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export ConnectionStrings__PlatformDatabase="Server=127.0.0.1,${FMCPA_SQL_PORT};Database=${FMCPA_DB_NAME};User Id=sa;Password=${FMCPA_SQL_SA_PASSWORD};TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True"
export Storage__Markets__MarketTenantCertificatesPath="${FMCPA_STORAGE_ROOT}/markets/tenant-certificates"
export Storage__Donations__ApplicationEvidencePath="${FMCPA_STORAGE_ROOT}/donations/application-evidences"
export Storage__Federation__ApplicationEvidencePath="${FMCPA_STORAGE_ROOT}/federation/application-evidences"

command_exists() {
  command -v "$1" >/dev/null 2>&1
}

docker_compose_cmd() {
  docker compose -f "${ROOT_DIR}/docker-compose.local.yml" "$@"
}

sqlserver_exec() {
  docker exec "${FMCPA_SQL_CONTAINER_NAME}" /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "${FMCPA_SQL_SA_PASSWORD}" "$@"
}

sqlserver_container_status() {
  docker ps -a --filter "name=^/${FMCPA_SQL_CONTAINER_NAME}$" --format '{{.Status}}' | head -n 1
}

sqlserver_container_exists() {
  [[ -n "$(sqlserver_container_status)" ]]
}

sqlserver_container_running() {
  docker ps --filter "name=^/${FMCPA_SQL_CONTAINER_NAME}$" --format '{{.Status}}' | grep -q .
}

port_listener_summary() {
  local port="${1:?port requerido}"

  if command_exists ss; then
    ss -ltnH "( sport = :${port} )" 2>/dev/null || true
    return 0
  fi

  if command_exists netstat; then
    netstat -ltn 2>/dev/null | awk -v expected=":${port}" '$4 ~ expected "$" { print }'
    return 0
  fi

  return 1
}

port_in_use() {
  local port="${1:?port requerido}"
  local summary

  if ! summary="$(port_listener_summary "${port}")"; then
    return 2
  fi

  [[ -n "${summary}" ]]
}

require_safe_database_name() {
  if [[ ! "${FMCPA_DB_NAME}" =~ ^[A-Za-z0-9_]+$ ]]; then
    echo "El nombre de base local '${FMCPA_DB_NAME}' no es seguro para el reset automatizado. Use solo letras, numeros y guion bajo." >&2
    return 1
  fi
}

delete_local_database_files() {
  require_safe_database_name >/dev/null
  docker exec "${FMCPA_SQL_CONTAINER_NAME}" /bin/bash -lc "rm -f /var/opt/mssql/data/${FMCPA_DB_NAME}.mdf /var/opt/mssql/data/${FMCPA_DB_NAME}_log.ldf"
}

ensure_storage_directories() {
  mkdir -p \
    "${Storage__Markets__MarketTenantCertificatesPath}" \
    "${Storage__Donations__ApplicationEvidencePath}" \
    "${Storage__Federation__ApplicationEvidencePath}"
}

ensure_local_state_dir() {
  mkdir -p "${FMCPA_LOCAL_STATE_DIR}"
}

wait_for_sqlserver() {
  local attempts="${1:-40}"
  local sleep_seconds="${2:-3}"
  local attempt=1

  while (( attempt <= attempts )); do
    if sqlserver_exec -Q "SELECT 1 AS Ready;" >/dev/null 2>&1; then
      return 0
    fi

    sleep "${sleep_seconds}"
    attempt=$(( attempt + 1 ))
  done

  echo "SQL Server local no respondio a tiempo en ${FMCPA_SQL_CONTAINER_NAME}." >&2
  return 1
}

wait_for_http() {
  local url="${1:?url requerida}"
  local attempts="${2:-40}"
  local sleep_seconds="${3:-2}"
  local attempt=1

  while (( attempt <= attempts )); do
    if curl -fsS "${url}" >/dev/null 2>&1; then
      return 0
    fi

    sleep "${sleep_seconds}"
    attempt=$(( attempt + 1 ))
  done

  echo "La URL '${url}' no respondio a tiempo." >&2
  return 1
}

state_file_path() {
  local file_name="${1:?nombre de archivo requerido}"
  printf '%s/%s' "${FMCPA_LOCAL_STATE_DIR}" "${file_name}"
}

backend_pid_file_path() {
  state_file_path "backend.pid"
}

frontend_pid_file_path() {
  state_file_path "frontend.pid"
}

backend_log_file_path() {
  state_file_path "backend.log"
}

frontend_log_file_path() {
  state_file_path "frontend.log"
}

pid_is_running() {
  local pid="${1:?pid requerido}"
  kill -0 "${pid}" >/dev/null 2>&1
}

stop_managed_process() {
  local pid_file="${1:?pid file requerido}"
  local label="${2:?label requerido}"

  if [[ ! -f "${pid_file}" ]]; then
    echo "No hay PID registrado para ${label} en ${pid_file}."
    return 0
  fi

  local pid
  pid="$(cat "${pid_file}")"

  if [[ -z "${pid}" ]]; then
    rm -f "${pid_file}"
    echo "PID vacio para ${label}; archivo limpiado."
    return 0
  fi

  if pid_is_running "${pid}"; then
    kill "${pid}" >/dev/null 2>&1 || true

    local attempt=1
    while (( attempt <= 20 )); do
      if ! pid_is_running "${pid}"; then
        break
      fi

      sleep 1
      attempt=$(( attempt + 1 ))
    done

    if pid_is_running "${pid}"; then
      echo "No fue posible detener ${label} con PID ${pid} de forma limpia." >&2
      return 1
    fi

    echo "${label} detenido (PID ${pid})."
  else
    echo "${label} no estaba corriendo; se limpia el PID registrado (${pid})."
  fi

  rm -f "${pid_file}"
}

ensure_dotnet_local_tools() {
  if [[ ! -f "${FMCPA_DOTNET_TOOLS_MANIFEST_PATH}" ]]; then
    echo "No existe el manifiesto local de herramientas .NET en ${FMCPA_DOTNET_TOOLS_MANIFEST_PATH}." >&2
    return 1
  fi

  (
    cd "${ROOT_DIR}"
    dotnet tool restore
  )
}

run_dotnet_ef() {
  (
    cd "${ROOT_DIR}"
    dotnet tool run dotnet-ef -- "$@"
  )
}

dotnet_ef_version() {
  (
    cd "${ROOT_DIR}"
    dotnet tool run dotnet-ef -- --version
  )
}

write_frontend_proxy_config() {
  mkdir -p "$(dirname "${FMCPA_FRONTEND_PROXY_CONFIG_PATH}")"

  cat > "${FMCPA_FRONTEND_PROXY_CONFIG_PATH}" <<EOF
{
  "/api": {
    "target": "http://127.0.0.1:${FMCPA_API_PORT}",
    "secure": false,
    "changeOrigin": false
  },
  "/health": {
    "target": "http://127.0.0.1:${FMCPA_API_PORT}",
    "secure": false,
    "changeOrigin": false
  }
}
EOF

  printf '%s' "${FMCPA_FRONTEND_PROXY_CONFIG_PATH}"
}

print_local_convention() {
  cat <<EOF
Convencion local FMCPA
- FMCPA_SQL_CONTAINER_NAME=${FMCPA_SQL_CONTAINER_NAME} (127.0.0.1:${FMCPA_SQL_PORT})
- FMCPA_DB_NAME=${FMCPA_DB_NAME}
- FMCPA_API_PORT=${FMCPA_API_PORT} -> http://127.0.0.1:${FMCPA_API_PORT}
- FMCPA_WEB_PORT=${FMCPA_WEB_PORT} -> http://127.0.0.1:${FMCPA_WEB_PORT}
- FMCPA_FRONTEND_PROXY_CONFIG_PATH=${FMCPA_FRONTEND_PROXY_CONFIG_PATH}
- FMCPA_STORAGE_ROOT=${FMCPA_STORAGE_ROOT}
- FMCPA_LOCAL_STATE_DIR=${FMCPA_LOCAL_STATE_DIR}
- FMCPA_DOTNET_TOOLS_MANIFEST_PATH=${FMCPA_DOTNET_TOOLS_MANIFEST_PATH}
EOF
}

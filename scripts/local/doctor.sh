#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/../.." && pwd)"
ENV_FILE="${ROOT_DIR}/.env.local"

ok_count=0
warning_count=0
error_count=0

report_ok() {
  ok_count=$(( ok_count + 1 ))
  echo "[OK] $*"
}

report_warning() {
  warning_count=$(( warning_count + 1 ))
  echo "[WARNING] $*"
}

report_error() {
  error_count=$(( error_count + 1 ))
  echo "[ERROR] $*"
}

if [[ -f "${ENV_FILE}" ]]; then
  if bash -n "${ENV_FILE}" >/dev/null 2>&1; then
    env_mode="custom"
  else
    report_error "'.env.local' existe pero no pudo validarse sintacticamente."
    env_mode="invalid"
  fi
else
  env_mode="defaults"
fi

# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

echo "Doctor local FMCPA"
print_local_convention

if [[ "${env_mode}" == "custom" ]]; then
  report_ok "'.env.local' existe y se puede leer."
elif [[ "${env_mode}" == "defaults" ]]; then
  report_warning "'.env.local' no existe; se usaran defaults y variables de entorno del proceso."
fi

if [[ -f "${ROOT_DIR}/docker-compose.local.yml" ]]; then
  report_ok "'docker-compose.local.yml' disponible."
else
  report_error "No existe 'docker-compose.local.yml'."
fi

if command_exists docker; then
  report_ok "'docker' disponible."
  if docker version >/dev/null 2>&1; then
    report_ok "Docker daemon accesible."
  else
    report_error "Docker esta instalado pero el daemon no responde."
  fi
else
  report_error "'docker' no esta disponible en PATH."
fi

if command_exists docker && docker compose version >/dev/null 2>&1; then
  report_ok "'docker compose' disponible."
else
  report_error "'docker compose' no esta disponible."
fi

if command_exists dotnet; then
  report_ok "'.NET SDK' disponible: $(dotnet --version)"
else
  report_error "'.NET SDK' no esta disponible."
fi

if [[ -f "${FMCPA_DOTNET_TOOLS_MANIFEST_PATH}" ]]; then
  report_ok "Manifiesto local de herramientas .NET disponible en '${FMCPA_DOTNET_TOOLS_MANIFEST_PATH}'."

  if dotnet_ef_tool_version="$(dotnet_ef_version 2>/dev/null)"; then
    report_ok "'dotnet-ef' local disponible: ${dotnet_ef_tool_version}"
  else
    report_warning "'dotnet-ef' local aun no esta restaurado; ejecute './scripts/local/apply-migrations.sh' o 'dotnet tool restore'."
  fi
else
  report_warning "No existe manifiesto local de herramientas .NET en '${FMCPA_DOTNET_TOOLS_MANIFEST_PATH}'."
fi

if command_exists npm; then
  report_ok "'npm' disponible: $(npm --version)"
else
  report_error "'npm' no esta disponible."
fi

if command_exists ss || command_exists netstat; then
  if port_in_use "${FMCPA_SQL_PORT}"; then
    if sqlserver_container_running; then
      report_ok "El puerto SQL ${FMCPA_SQL_PORT} ya esta atendido por el contenedor esperado '${FMCPA_SQL_CONTAINER_NAME}'."
    else
      report_warning "El puerto SQL ${FMCPA_SQL_PORT} ya esta en uso y no coincide con el contenedor esperado '${FMCPA_SQL_CONTAINER_NAME}'."
    fi
  else
    report_ok "El puerto SQL ${FMCPA_SQL_PORT} esta libre."
  fi

  if port_in_use "${FMCPA_API_PORT}"; then
    report_warning "El puerto API ${FMCPA_API_PORT} ya esta en uso; si quiere otra instancia, use override o detenga el proceso actual."
  else
    report_ok "El puerto API ${FMCPA_API_PORT} esta libre."
  fi

  if port_in_use "${FMCPA_WEB_PORT}"; then
    report_warning "El puerto frontend ${FMCPA_WEB_PORT} ya esta en uso; probablemente ya existe un servidor web local activo."
  else
    report_ok "El puerto frontend ${FMCPA_WEB_PORT} esta libre."
  fi
else
  report_warning "No se encontro 'ss' ni 'netstat'; no fue posible revisar puertos."
fi

if sqlserver_container_running; then
  report_ok "El contenedor SQL esperado '${FMCPA_SQL_CONTAINER_NAME}' esta corriendo."
elif sqlserver_container_exists; then
  report_warning "El contenedor SQL esperado '${FMCPA_SQL_CONTAINER_NAME}' existe pero no esta corriendo. Use './scripts/local/up-sqlserver.sh'."
else
  report_warning "El contenedor SQL esperado '${FMCPA_SQL_CONTAINER_NAME}' no existe aun; puede crearse con './scripts/local/up-sqlserver.sh'."
fi

if require_safe_database_name >/dev/null 2>&1; then
  report_ok "El nombre de base '${FMCPA_DB_NAME}' es valido para el reset controlado."
else
  report_warning "El nombre de base '${FMCPA_DB_NAME}' no cumple la convencion segura del reset automatizado."
fi

if ensure_storage_directories >/dev/null 2>&1; then
  report_ok "Las rutas de storage local existen o pueden crearse bajo '${FMCPA_STORAGE_ROOT}'."
else
  report_error "No fue posible preparar las rutas de storage bajo '${FMCPA_STORAGE_ROOT}'."
fi

proxy_config_path="$(write_frontend_proxy_config)"
report_ok "El proxy local de Angular puede generarse en '${proxy_config_path}' para apuntar al API configurado."

echo
echo "Resumen doctor: OK=${ok_count} WARNING=${warning_count} ERROR=${error_count}"

if (( error_count > 0 )); then
  exit 1
fi

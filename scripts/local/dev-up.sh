#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

usage() {
  cat <<EOF
Uso:
  ./scripts/local/dev-up.sh [--dry-run] [--no-backend] [--no-frontend]

Descripcion:
  Levanta el stack local gestionado por FMCPA: SQL Server, migraciones y, por defecto, backend y frontend en background.

Opciones:
  --dry-run      Muestra los pasos sin ejecutarlos.
  --no-backend   No arranca backend en background.
  --no-frontend  No arranca frontend en background.
EOF
}

dry_run=0
start_backend=1
start_frontend=1

for arg in "$@"; do
  case "${arg}" in
    --dry-run)
      dry_run=1
      ;;
    --no-backend)
      start_backend=0
      ;;
    --no-frontend)
      start_frontend=0
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Argumento no reconocido: ${arg}" >&2
      usage >&2
      exit 1
      ;;
  esac
done

run_or_echo() {
  if (( dry_run == 1 )); then
    echo "[DRY-RUN] $*"
    return 0
  fi

  "$@"
}

start_background_service() {
  local label="${1:?label requerido}"
  local pid_file="${2:?pid file requerido}"
  local log_file="${3:?log file requerido}"
  shift 3

  if [[ -f "${pid_file}" ]]; then
    local existing_pid
    existing_pid="$(cat "${pid_file}")"
    if [[ -n "${existing_pid}" ]] && pid_is_running "${existing_pid}"; then
      echo "${label} ya esta corriendo con PID ${existing_pid}. Log: ${log_file}"
      return 0
    fi

    rm -f "${pid_file}"
  fi

  : > "${log_file}"
  nohup "$@" </dev/null >"${log_file}" 2>&1 &
  echo "$!" > "${pid_file}"
  echo "${label} iniciado en background. PID $(cat "${pid_file}"). Log: ${log_file}"
}

print_local_convention
echo "Preparando sesion local compuesta..."
ensure_local_state_dir

if (( dry_run == 1 )); then
  echo "[DRY-RUN] Estado local gestionado en ${FMCPA_LOCAL_STATE_DIR}"
  echo "[DRY-RUN] SQL + migraciones: ${SCRIPT_DIR}/apply-migrations.sh"
  if (( start_backend == 1 )); then
    echo "[DRY-RUN] Backend en background -> ${SCRIPT_DIR}/run-backend.sh"
  fi
  if (( start_frontend == 1 )); then
    echo "[DRY-RUN] Frontend en background -> ${SCRIPT_DIR}/run-frontend.sh"
  fi
  exit 0
fi

run_or_echo "${SCRIPT_DIR}/apply-migrations.sh"

if (( start_backend == 1 )); then
  if port_in_use "${FMCPA_API_PORT}"; then
    echo "El puerto API ${FMCPA_API_PORT} ya esta en uso. Libere el puerto o use override antes de ejecutar dev-up." >&2
    exit 1
  fi

  start_background_service \
    "Backend local" \
    "$(backend_pid_file_path)" \
    "$(backend_log_file_path)" \
    "${SCRIPT_DIR}/run-backend.sh"

  wait_for_http "http://127.0.0.1:${FMCPA_API_PORT}/health" 40 2
  echo "Backend disponible en http://127.0.0.1:${FMCPA_API_PORT}/health"
fi

if (( start_frontend == 1 )); then
  if port_in_use "${FMCPA_WEB_PORT}"; then
    echo "El puerto frontend ${FMCPA_WEB_PORT} ya esta en uso. Libere el puerto o use override antes de ejecutar dev-up." >&2
    exit 1
  fi

  start_background_service \
    "Frontend local" \
    "$(frontend_pid_file_path)" \
    "$(frontend_log_file_path)" \
    "${SCRIPT_DIR}/run-frontend.sh"

  wait_for_http "http://127.0.0.1:${FMCPA_WEB_PORT}/" 60 2
  echo "Frontend disponible en http://127.0.0.1:${FMCPA_WEB_PORT}/"
fi

echo "dev-up completado."
echo "Estado gestionado: ${FMCPA_LOCAL_STATE_DIR}"

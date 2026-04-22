#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

usage() {
  cat <<EOF
Uso:
  ./scripts/local/dev-down.sh [--dry-run]

Descripcion:
  Detiene backend y frontend gestionados por dev-up y apaga el contenedor SQL configurado, sin tocar procesos ajenos al proyecto.

Opciones:
  --dry-run   Muestra lo que haria sin ejecutar cambios.
EOF
}

dry_run=0

for arg in "$@"; do
  case "${arg}" in
    --dry-run)
      dry_run=1
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

print_local_convention
echo "Apagando sesion local compuesta..."
ensure_local_state_dir

if (( dry_run == 1 )); then
  echo "[DRY-RUN] Detener backend gestionado: $(backend_pid_file_path)"
  echo "[DRY-RUN] Detener frontend gestionado: $(frontend_pid_file_path)"
  echo "[DRY-RUN] Detener contenedor SQL si esta corriendo: ${FMCPA_SQL_CONTAINER_NAME}"
  exit 0
fi

stop_managed_process "$(frontend_pid_file_path)" "Frontend local"
stop_managed_process "$(backend_pid_file_path)" "Backend local"

if sqlserver_container_running; then
  docker stop "${FMCPA_SQL_CONTAINER_NAME}" >/dev/null
  echo "SQL Server local detenido (${FMCPA_SQL_CONTAINER_NAME})."
else
  echo "El contenedor SQL '${FMCPA_SQL_CONTAINER_NAME}' no estaba corriendo."
fi

echo "dev-down completado."
echo "Logs preservados en ${FMCPA_LOCAL_STATE_DIR}"

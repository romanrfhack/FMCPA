#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

usage() {
  cat <<EOF
Uso:
  ./scripts/local/reset-db.sh [--force] [--dry-run]

Descripcion:
  Elimina y recrea solo la base local configurada en FMCPA_DB_NAME dentro del contenedor SQL configurado.

Opciones:
  --force    Ejecuta el reset sin confirmacion interactiva.
  --dry-run  Muestra el alcance del reset sin ejecutarlo.
EOF
}

force=0
dry_run=0

for arg in "$@"; do
  case "${arg}" in
    --force)
      force=1
      ;;
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

require_safe_database_name
print_local_convention
echo "Preparando reset controlado de base local..."

if (( dry_run == 1 )); then
  echo "[DRY-RUN] Se recrearia la base local '${FMCPA_DB_NAME}' dentro del contenedor '${FMCPA_SQL_CONTAINER_NAME}'."
  echo "[DRY-RUN] Se eliminarian los archivos /var/opt/mssql/data/${FMCPA_DB_NAME}.mdf y /var/opt/mssql/data/${FMCPA_DB_NAME}_log.ldf del contenedor."
  echo "[DRY-RUN] Luego se ejecutaria ${SCRIPT_DIR}/apply-migrations.sh con la configuracion efectiva mostrada arriba."
  exit 0
fi

if (( force == 0 )); then
  if [[ ! -t 0 ]]; then
    echo "Reset destructivo bloqueado. Use '--force' o ejecute el script de forma interactiva." >&2
    exit 1
  fi

  echo "Advertencia: esta operacion eliminara y recreara la base local '${FMCPA_DB_NAME}' en el contenedor '${FMCPA_SQL_CONTAINER_NAME}'."
  read -r -p "Escriba '${FMCPA_DB_NAME}' para continuar: " confirmation
  if [[ "${confirmation}" != "${FMCPA_DB_NAME}" ]]; then
    echo "Reset cancelado."
    exit 1
  fi
fi

docker_compose_cmd up -d "${FMCPA_SQL_SERVICE_NAME}"
wait_for_sqlserver
ensure_local_state_dir

sqlserver_exec -d master -Q "SET NOCOUNT ON; IF DB_ID(N'${FMCPA_DB_NAME}') IS NOT NULL BEGIN ALTER DATABASE [${FMCPA_DB_NAME}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [${FMCPA_DB_NAME}]; END;"
delete_local_database_files
sqlserver_exec -d master -Q "SET NOCOUNT ON; CREATE DATABASE [${FMCPA_DB_NAME}];"

"${SCRIPT_DIR}/apply-migrations.sh"

echo "Reset de base local completado para '${FMCPA_DB_NAME}'."

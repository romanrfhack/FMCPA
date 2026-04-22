#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

usage() {
  cat <<EOF
Uso:
  ./scripts/local/normalize-legacy-history.sh [--dry-run]

Descripcion:
  Ejecuta la regularizacion retrospectiva de cierres heredados usando el backend local en Development.

Opciones:
  --dry-run   Muestra los candidatos elegibles sin persistir eventos normalizados.
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
echo "Ejecutando regularizacion retrospectiva de cierres heredados..."

wait_for_http "http://127.0.0.1:${FMCPA_API_PORT}/health" 20 1

request_url="http://127.0.0.1:${FMCPA_API_PORT}/api/history/normalize-legacy-closures"
if (( dry_run == 1 )); then
  request_url="${request_url}?dryRun=true"
fi

response_file="$(mktemp)"
trap 'rm -f "${response_file}"' EXIT

curl -fsS -X POST "${request_url}" -H 'Accept: application/json' -o "${response_file}"

node - "${response_file}" <<'EOF'
const fs = require('fs');

const responsePath = process.argv[2];
const payload = JSON.parse(fs.readFileSync(responsePath, 'utf8'));
const items = Array.isArray(payload.items) ? payload.items : [];

console.log(`Regularizacion completada${payload.dryRun ? ' (dry-run)' : ''}`);
console.log(`Scanned closed count: ${payload.scannedClosedCount}`);
console.log(`Eligible count: ${payload.eligibleCount}`);
console.log(`Normalized count: ${payload.normalizedCount}`);
console.log(`Skipped count: ${payload.skippedCount}`);

if (items.length === 0) {
  console.log('No hubo elementos para reportar.');
  process.exit(0);
}

console.log('Items:');
for (const item of items) {
  console.log(`- [${item.outcome}] ${item.moduleCode}/${item.itemType} :: ${item.title} :: fuente=${item.historicalTimestampSource} :: ${item.historicalTimestampUtc}`);
}
EOF

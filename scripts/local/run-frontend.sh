#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

proxy_config_path="$(write_frontend_proxy_config)"
ensure_local_state_dir

cd "${ROOT_DIR}/src/frontend"

print_local_convention
echo "Proxy Angular local generado en: ${proxy_config_path}"
echo "El frontend de desarrollo resolvera /api y /health hacia http://127.0.0.1:${FMCPA_API_PORT}."
echo "Levantando frontend local..."
exec npm run start -- --host 127.0.0.1 --port "${FMCPA_WEB_PORT}" --proxy-config "${proxy_config_path}"

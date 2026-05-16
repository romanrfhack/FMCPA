#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(git -C "${SCRIPT_DIR}" rev-parse --show-toplevel 2>/dev/null || true)"
if [[ -z "${ROOT_DIR}" ]]; then
  ROOT_DIR="$(cd "${SCRIPT_DIR}/../.." && pwd)"
fi
ARTIFACT_ROOT="${HOME}/fmcpa-artifacts"
BACKEND_PROJECT="src/backend/src/FMCPA.Api/FMCPA.Api.csproj"
FRONTEND_DIR="src/frontend"
FRONTEND_OUTPUT="src/frontend/dist/frontend/browser"
SMOKE_PORT="55111"
SMOKE_PID=""
SMOKE_LOG=""
CONTACTS_COMPATIBILITY_ALIAS_SOURCE=""

cleanup() {
  if [[ -n "${SMOKE_PID}" ]] && kill -0 "${SMOKE_PID}" >/dev/null 2>&1; then
    kill "${SMOKE_PID}" >/dev/null 2>&1 || true
    wait "${SMOKE_PID}" >/dev/null 2>&1 || true
  fi
}

trap cleanup EXIT

require_command() {
  local command_name="${1:?command name required}"
  if ! command -v "${command_name}" >/dev/null 2>&1; then
    echo "ERROR: required command '${command_name}' was not found." >&2
    exit 1
  fi
}

wait_for_health() {
  local url="${1:?health url required}"
  local attempts="${2:-40}"
  local sleep_seconds="${3:-1}"

  for ((attempt = 1; attempt <= attempts; attempt++)); do
    if curl -fsS "${url}" >/dev/null 2>&1; then
      return 0
    fi

    if [[ -n "${SMOKE_PID}" ]] && ! kill -0 "${SMOKE_PID}" >/dev/null 2>&1; then
      echo "ERROR: published backend smoke process exited before health check passed." >&2
      if [[ -f "${SMOKE_LOG}" ]]; then
        sed -n '1,160p' "${SMOKE_LOG}" >&2
      fi
      return 1
    fi

    sleep "${sleep_seconds}"
  done

  echo "ERROR: health check did not pass at ${url}." >&2
  if [[ -f "${SMOKE_LOG}" ]]; then
    sed -n '1,160p' "${SMOKE_LOG}" >&2
  fi
  return 1
}

scan_for_forbidden_strings() {
  local release_dir="${1:?release dir required}"
  local frontend_dir="${release_dir}/frontend"
  local backend_dir="${release_dir}/backend"
  local forbidden_strings=(
    "localhost:5080"
    "http://localhost"
    "127.0.0.1:4200"
    "localhost:4200"
  )
  local matches_file="/tmp/fmcpa-package-forbidden.txt"
  local scan_files=()

  if [[ -d "${frontend_dir}" ]]; then
    while IFS= read -r -d '' file; do
      scan_files+=("${file}")
    done < <(
      find "${frontend_dir}" -type f \
        \( -name '*.html' \
        -o -name '*.js' \
        -o -name '*.css' \
        -o -name '*.json' \
        -o -name '*.map' \
        -o -name '*.txt' \) \
        -print0
    )
  fi

  if [[ -d "${backend_dir}" ]]; then
    while IFS= read -r -d '' file; do
      scan_files+=("${file}")
    done < <(find "${backend_dir}" -maxdepth 1 -type f -name 'appsettings*.json' -print0)
  fi

  if [[ -f "${release_dir}/manifest.txt" ]]; then
    scan_files+=("${release_dir}/manifest.txt")
  fi

  if ((${#scan_files[@]} == 0)); then
    return 0
  fi

  for forbidden in "${forbidden_strings[@]}"; do
    if grep -nF -- "${forbidden}" "${scan_files[@]}" >"${matches_file}" 2>/dev/null; then
      echo "ERROR: forbidden string found in release package: ${forbidden}" >&2
      cat "${matches_file}" >&2
      exit 1
    fi
  done
}

add_frontend_chunk_compatibility_aliases() {
  local contacts_stale_chunk="chunk-IISUAAIB.js"
  local contacts_chunks=()

  mapfile -t contacts_chunks < <(grep -l "ContactsPageComponent" "${FRONTEND_RELEASE_DIR}"/chunk-*.js || true)
  if ((${#contacts_chunks[@]} != 1)); then
    echo "ERROR: expected exactly one contacts lazy chunk and found ${#contacts_chunks[@]}." >&2
    printf '%s\n' "${contacts_chunks[@]}" >&2
    exit 1
  fi

  CONTACTS_COMPATIBILITY_ALIAS_SOURCE="$(basename "${contacts_chunks[0]}")"
  if [[ "${CONTACTS_COMPATIBILITY_ALIAS_SOURCE}" != "${contacts_stale_chunk}" ]]; then
    cp "${contacts_chunks[0]}" "${FRONTEND_RELEASE_DIR}/${contacts_stale_chunk}"
  fi
}

verify_frontend_dynamic_imports() {
  local main_files=("${FRONTEND_RELEASE_DIR}"/main-*.js)
  local referenced_chunks=()
  local missing_chunks=()

  if [[ ! -f "${main_files[0]}" ]]; then
    echo "ERROR: frontend main bundle was not found in ${FRONTEND_RELEASE_DIR}." >&2
    exit 1
  fi

  mapfile -t referenced_chunks < <(grep -hoE 'chunk-[A-Z0-9]+\.js' "${main_files[@]}" | sort -u || true)
  for chunk in "${referenced_chunks[@]}"; do
    if [[ ! -f "${FRONTEND_RELEASE_DIR}/${chunk}" ]]; then
      missing_chunks+=("${chunk}")
    fi
  done

  if ((${#missing_chunks[@]} > 0)); then
    echo "ERROR: frontend build references missing chunks:" >&2
    printf '%s\n' "${missing_chunks[@]}" >&2
    exit 1
  fi
}

write_frontend_cache_snippet() {
  cat > "${RELEASE_DIR}/nginx-fmcpa-frontend-cache.conf" <<'NGINX'
# Place these locations inside the fmcpa.com.mx server block.
# HTML must revalidate on every deploy; hashed JS/CSS/assets can be cached long-term.
location = /index.html {
    add_header Cache-Control "no-store" always;
    try_files /index.html =404;
}

location / {
    add_header Cache-Control "no-store" always;
    try_files $uri $uri/ /index.html;
}

location ~* \.(?:js|css|webp|ico|png|jpg|jpeg|svg|woff2?)$ {
    add_header Cache-Control "public, max-age=31536000, immutable" always;
    try_files $uri =404;
}
NGINX
}

random_signing_key() {
  od -An -N64 -tx1 /dev/urandom | tr -d ' \n'
}

require_command dotnet
require_command npm
require_command git
require_command curl
require_command tar
require_command sha256sum
require_command od

cd "${ROOT_DIR}"

GIT_SHORT_SHA="$(git rev-parse --short HEAD)"
GIT_FULL_SHA="$(git rev-parse HEAD)"
GIT_BRANCH="$(git branch --show-current)"
if [[ -z "${GIT_BRANCH}" ]]; then
  GIT_BRANCH="detached"
fi

TIMESTAMP="$(date -u +%Y%m%d%H%M%S)"
RELEASE_BASE_NAME="fmcpa-${TIMESTAMP}-${GIT_SHORT_SHA}"
RELEASE_NAME="${RELEASE_BASE_NAME}"
RELEASE_INDEX=1

mkdir -p "${ARTIFACT_ROOT}"
while [[ -e "${ARTIFACT_ROOT}/${RELEASE_NAME}" || -e "${ARTIFACT_ROOT}/${RELEASE_NAME}.tar.gz" ]]; do
  RELEASE_NAME="${RELEASE_BASE_NAME}-${RELEASE_INDEX}"
  RELEASE_INDEX=$((RELEASE_INDEX + 1))
done

RELEASE_DIR="${ARTIFACT_ROOT}/${RELEASE_NAME}"
BACKEND_DIR="${RELEASE_DIR}/backend"
FRONTEND_RELEASE_DIR="${RELEASE_DIR}/frontend"
SMOKE_STORAGE_DIR="${RELEASE_DIR}/smoke-storage"
SMOKE_LOG="${RELEASE_DIR}/smoke-backend.log"
ARCHIVE="${ARTIFACT_ROOT}/${RELEASE_NAME}.tar.gz"

mkdir -p \
  "${BACKEND_DIR}" \
  "${FRONTEND_RELEASE_DIR}" \
  "${SMOKE_STORAGE_DIR}/markets/tenant-certificates" \
  "${SMOKE_STORAGE_DIR}/donations/application-evidences" \
  "${SMOKE_STORAGE_DIR}/federation/application-evidences"

echo "Packaging FMCPA release ${RELEASE_NAME}"
echo "Release directory: ${RELEASE_DIR}"

dotnet publish "${BACKEND_PROJECT}" -c Release -o "${BACKEND_DIR}"

find "${BACKEND_DIR}" -type f \
  \( -name 'appsettings.json' -o -name 'appsettings.Development.json' \) \
  -delete

cat > "${BACKEND_DIR}/appsettings.Production.json" <<'JSON'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "fmcpa.com.mx;www.fmcpa.com.mx"
}
JSON

(
  cd "${FRONTEND_DIR}"
  npm run build
)

if [[ ! -d "${FRONTEND_OUTPUT}" ]]; then
  echo "ERROR: frontend output path was not found: ${FRONTEND_OUTPUT}" >&2
  exit 1
fi

cp -a "${FRONTEND_OUTPUT}/." "${FRONTEND_RELEASE_DIR}/"
add_frontend_chunk_compatibility_aliases
verify_frontend_dynamic_imports
write_frontend_cache_snippet

SMOKE_JWT_SIGNING_KEY="$(random_signing_key)"
pushd "${BACKEND_DIR}" >/dev/null
env \
  ASPNETCORE_ENVIRONMENT=Production \
  ASPNETCORE_URLS="http://127.0.0.1:${SMOKE_PORT}" \
  ConnectionStrings__PlatformDatabase="Server=127.0.0.1,1433;Database=FMCPA_SMOKE;User Id=fmcpa_smoke;Password=not-a-real-secret;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" \
  Cors__AllowedOrigins__0="https://fmcpa.com.mx" \
  Cors__AllowedOrigins__1="https://www.fmcpa.com.mx" \
  Auth__Jwt__Issuer="FMCPA.Production" \
  Auth__Jwt__Audience="FMCPA.Web.Production" \
  Auth__Jwt__SigningKey="${SMOKE_JWT_SIGNING_KEY}" \
  Storage__Markets__MarketTenantCertificatesPath="${SMOKE_STORAGE_DIR}/markets/tenant-certificates" \
  Storage__Donations__ApplicationEvidencePath="${SMOKE_STORAGE_DIR}/donations/application-evidences" \
  Storage__Federation__ApplicationEvidencePath="${SMOKE_STORAGE_DIR}/federation/application-evidences" \
  Security__WebOriginProtection__ClientHeaderName="X-FMCPA-Client" \
  Security__WebOriginProtection__ClientHeaderValue="FMCPA-Web" \
  AllowedHosts="localhost;127.0.0.1;fmcpa.com.mx;www.fmcpa.com.mx" \
  dotnet FMCPA.Api.dll > "${SMOKE_LOG}" 2>&1 &
SMOKE_PID="$!"
popd >/dev/null

wait_for_health "http://127.0.0.1:${SMOKE_PORT}/health" 40 1
curl -fsS "http://127.0.0.1:${SMOKE_PORT}/health" >/dev/null
cleanup
SMOKE_PID=""
rm -f "${SMOKE_LOG}"

cat > "${RELEASE_DIR}/manifest.txt" <<EOF
release name: ${RELEASE_NAME}
fecha UTC: $(date -u +%Y-%m-%dT%H:%M:%SZ)
branch: ${GIT_BRANCH}
git SHA completo: ${GIT_FULL_SHA}

git status --short --branch:
$(git status --short --branch)

git diff --stat:
$(git diff --stat)

backend project: ${BACKEND_PROJECT}
frontend output path: ${FRONTEND_OUTPUT}

validaciones realizadas:
- dotnet publish ${BACKEND_PROJECT} -c Release -o ${BACKEND_DIR}
- appsettings.json y appsettings.Development.json eliminados del publish
- appsettings.Production.json minimo sin secretos creado en backend
- npm run build en ${FRONTEND_DIR}
- frontend copiado desde ${FRONTEND_OUTPUT}
- alias temporal Contactos chunk-IISUAAIB.js -> ${CONTACTS_COMPATIBILITY_ALIAS_SOURCE}
- chunks dinamicos referenciados por main verificados en frontend
- snippet nginx-fmcpa-frontend-cache.conf generado para cache HTML/assets
- scan de strings prohibidos en frontend textual y backend appsettings/manifest
- smoke backend publicado en Production sobre 127.0.0.1:${SMOKE_PORT}
- curl /health exitoso
EOF

scan_for_forbidden_strings "${RELEASE_DIR}"

(
  cd "${RELEASE_DIR}"
  find . -type f ! -name SHA256SUMS.txt -print0 \
    | sort -z \
    | xargs -0 sha256sum > SHA256SUMS.txt
)

tar -C "${ARTIFACT_ROOT}" -czf "${ARCHIVE}" "${RELEASE_NAME}"
SHA256="$(sha256sum "${ARCHIVE}" | awk '{print $1}')"
printf '%s  %s\n' "${SHA256}" "$(basename "${ARCHIVE}")" > "${ARCHIVE}.sha256"

echo "Release package created successfully."
echo "RELEASE_NAME=${RELEASE_NAME}"
echo "RELEASE_DIR=${RELEASE_DIR}"
echo "ARCHIVE=${ARCHIVE}"
echo "SHA256=${SHA256}"

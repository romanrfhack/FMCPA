#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

API_BASE_URL="http://127.0.0.1:${FMCPA_API_PORT}"
SMOKE_MVP_TAG="${SMOKE_MVP_TAG:-SMOKE-MVP-$(date +%Y%m%d%H%M%S)}"
today_utc="$(date -u +%F)"
permit_valid_to="$(date -u -d '+10 days' +%F)"

ok_count=0
fail_count=0

summary_on_exit() {
  echo
  if (( fail_count > 0 )); then
    echo "Smoke MVP FAIL"
  else
    echo "Smoke MVP OK"
  fi
  echo "Tag tecnico: ${SMOKE_MVP_TAG}"
  echo "Resumen: OK=${ok_count} FAIL=${fail_count}"
}

trap summary_on_exit EXIT

print_local_convention
echo "Ejecutando smoke MVP con tag tecnico '${SMOKE_MVP_TAG}'..."

report_step() {
  echo
  echo "[STEP] $*"
}

report_ok() {
  ok_count=$(( ok_count + 1 ))
  echo "[OK] $*"
}

report_fail() {
  fail_count=$(( fail_count + 1 ))
  echo "[FAIL] $*" >&2
}

require_command() {
  local command_name="${1:?command requerido}"
  if command_exists "${command_name}"; then
    report_ok "Dependencia disponible: ${command_name}"
    return 0
  fi

  report_fail "Dependencia faltante: ${command_name}"
  exit 1
}

api_get() {
  local path="${1:?path requerido}"
  curl -fsS "${API_BASE_URL}${path}"
}

api_post_json() {
  local path="${1:?path requerido}"
  local payload="${2:?payload requerido}"
  curl -fsS -H 'Content-Type: application/json' -X POST -d "${payload}" "${API_BASE_URL}${path}"
}

json_query() {
  local expression="${1:?expresion requerida}"
  JSON_QUERY_EXPRESSION="${expression}" node -e '
const fs = require("fs");
const source = fs.readFileSync(0, "utf8");
const data = source.trim() ? JSON.parse(source) : null;
const expression = process.env.JSON_QUERY_EXPRESSION;

let result;
try {
  result = Function("data", `return (${expression});`)(data);
} catch (error) {
  console.error(error.message);
  process.exit(2);
}

if (result === undefined || result === null) {
  process.exit(3);
}

if (typeof result === "object") {
  process.stdout.write(JSON.stringify(result));
} else {
  process.stdout.write(String(result));
}
'
}

extract_json_value() {
  local json_payload="${1:?json requerido}"
  local expression="${2:?expresion requerida}"
  local description="${3:?descripcion requerida}"
  local value

  if ! value="$(printf '%s' "${json_payload}" | json_query "${expression}" 2>&1)"; then
    report_fail "${description}: ${value}"
    exit 1
  fi

  ok_count=$(( ok_count + 1 ))
  echo "[OK] ${description}: ${value}" >&2
  printf '%s' "${value}"
}

assert_json_predicate() {
  local json_payload="${1:?json requerido}"
  local expression="${2:?expresion requerida}"
  local description="${3:?descripcion requerida}"
  local result

  if ! result="$(printf '%s' "${json_payload}" | json_query "${expression}" 2>&1)"; then
    report_fail "${description}: ${result}"
    exit 1
  fi

  if [[ "${result}" != "true" ]]; then
    report_fail "${description}: resultado=${result}"
    exit 1
  fi

  report_ok "${description}"
}

assert_contains() {
  local haystack="${1:?contenido requerido}"
  local needle="${2:?texto requerido}"
  local description="${3:?descripcion requerida}"

  if printf '%s' "${haystack}" | grep -Fq "${needle}"; then
    report_ok "${description}"
    return 0
  fi

  report_fail "${description}: no se encontro '${needle}'"
  exit 1
}

assert_not_contains() {
  local haystack="${1:?contenido requerido}"
  local needle="${2:?texto requerido}"
  local description="${3:?descripcion requerida}"

  if printf '%s' "${haystack}" | grep -Fq "${needle}"; then
    report_fail "${description}: se encontro '${needle}' cuando ya deberia estar cerrado"
    exit 1
  fi

  report_ok "${description}"
}

report_step "Prerequisitos y wiring basico"
require_command curl
require_command docker
require_command dotnet
require_command node
wait_for_sqlserver
report_ok "SQL Server local accesible en '${FMCPA_SQL_CONTAINER_NAME}'"

health_response="$(api_get "/health")"
assert_contains "${health_response}" "Healthy" "El endpoint /health responde sano"

dashboard_summary_initial="$(api_get "/api/dashboard/summary")"
assert_json_predicate "${dashboard_summary_initial}" "data.totals.activeAlertCount >= 0 && data.totals.closedRecordsCount >= 0" "dashboard/summary responde con totales validos"

dashboard_alerts_initial="$(api_get "/api/dashboard/alerts")"
assert_json_predicate "${dashboard_alerts_initial}" "Array.isArray(data.marketCertificates) && Array.isArray(data.donations) && Array.isArray(data.financialPermits) && Array.isArray(data.federationActions) && Array.isArray(data.federationDonations)" "dashboard/alerts responde con colecciones validas"

bitacora_initial="$(api_get "/api/bitacora?take=5")"
assert_json_predicate "${bitacora_initial}" "Array.isArray(data)" "bitacora responde con listado"

history_initial="$(api_get "/api/history/closed-items")"
assert_json_predicate "${history_initial}" "Array.isArray(data)" "history/closed-items responde con listado"

documents_initial="$(api_get "/api/documents/integrity?take=5")"
assert_json_predicate "${documents_initial}" "data.summary.totalDocumentRecords >= 0 && Array.isArray(data.records)" "documents/integrity responde con resumen documental"

report_step "Datos tecnicos minimos para el smoke MVP"
internal_contact_response="$(api_post_json "/api/contacts" "$(cat <<EOF
{"name":"${SMOKE_MVP_TAG} Contacto Interno","contactTypeId":1,"organizationOrDependency":"Operación MVP","roleTitle":"Coordinación técnica","mobilePhone":"5550001000","whatsAppPhone":"5550001000","email":"${SMOKE_MVP_TAG,,}.interno@example.com","notes":"Contacto técnico para smoke MVP."}
EOF
)")"
internal_contact_id="$(extract_json_value "${internal_contact_response}" "data.id" "Contacto interno creado")"

external_contact_response="$(api_post_json "/api/contacts" "$(cat <<EOF
{"name":"${SMOKE_MVP_TAG} Contacto Externo","contactTypeId":2,"organizationOrDependency":"Aliado operativo","roleTitle":"Enlace externo","mobilePhone":"5550002000","whatsAppPhone":"5550002000","email":"${SMOKE_MVP_TAG,,}.externo@example.com","notes":"Contacto externo técnico para smoke MVP."}
EOF
)")"
external_contact_id="$(extract_json_value "${external_contact_response}" "data.id" "Contacto externo creado")"

market_response="$(api_post_json "/api/markets" "$(cat <<EOF
{"name":"${SMOKE_MVP_TAG} Mercado","borough":"Cuauhtemoc","statusCatalogEntryId":1001,"secretaryGeneralContactId":"${internal_contact_id}","secretaryGeneralName":"${SMOKE_MVP_TAG} Secretario","notes":"Registro técnico de smoke MVP."}
EOF
)")"
market_id="$(extract_json_value "${market_response}" "data.id" "Mercado tecnico creado")"

market_detail="$(api_get "/api/markets/${market_id}")"
assert_json_predicate "${market_detail}" "data.id === \"${market_id}\" && data.name.includes(\"${SMOKE_MVP_TAG}\")" "Detalle de mercado accesible"

markets_list="$(api_get "/api/markets")"
assert_contains "${markets_list}" "${market_id}" "Listado de mercados incluye el registro tecnico"

markets_tenant_alerts="$(api_get "/api/markets/alerts/tenants")"
assert_json_predicate "${markets_tenant_alerts}" "Array.isArray(data)" "Markets alerts responde aunque no existan cédulas de prueba"

donation_response="$(api_post_json "/api/donations" "$(cat <<EOF
{"donorEntityName":"${SMOKE_MVP_TAG} Donante","donationDate":"${today_utc}","donationType":"Apoyo técnico","baseAmount":1000,"reference":"${SMOKE_MVP_TAG}-DON","notes":"Donación técnica para smoke MVP.","statusCatalogEntryId":1201}
EOF
)")"
donation_id="$(extract_json_value "${donation_response}" "data.id" "Donación tecnica creada")"

donation_detail="$(api_get "/api/donations/${donation_id}")"
assert_json_predicate "${donation_detail}" "data.id === \"${donation_id}\" && data.reference === \"${SMOKE_MVP_TAG}-DON\"" "Detalle de donacion accesible"

financial_permit_response="$(api_post_json "/api/financials" "$(cat <<EOF
{"financialName":"${SMOKE_MVP_TAG} Financiera","institutionOrDependency":"${SMOKE_MVP_TAG} Institución","placeOrStand":"${SMOKE_MVP_TAG} Stand","validFrom":"${today_utc}","validTo":"${permit_valid_to}","schedule":"09:00-17:00","negotiatedTerms":"Convenio técnico mínimo para smoke MVP.","statusCatalogEntryId":1401,"notes":"Oficio técnico para smoke MVP."}
EOF
)")"
financial_permit_id="$(extract_json_value "${financial_permit_response}" "data.id" "Oficio tecnico creado")"

financial_detail="$(api_get "/api/financials/${financial_permit_id}")"
assert_json_predicate "${financial_detail}" "data.id === \"${financial_permit_id}\" && data.financialName.includes(\"${SMOKE_MVP_TAG}\")" "Detalle de oficio accesible"

federation_action_response="$(api_post_json "/api/federation/actions" "$(cat <<EOF
{"actionTypeCode":"MEETING","counterpartyOrInstitution":"${SMOKE_MVP_TAG} Institución","actionDate":"${today_utc}","objective":"${SMOKE_MVP_TAG} Objetivo operativo","statusCatalogEntryId":1501,"notes":"Gestión técnica para smoke MVP."}
EOF
)")"
federation_action_id="$(extract_json_value "${federation_action_response}" "data.id" "Gestion tecnica creada")"

federation_action_detail="$(api_get "/api/federation/actions/${federation_action_id}")"
assert_json_predicate "${federation_action_detail}" "data.id === \"${federation_action_id}\" && data.counterpartyOrInstitution.includes(\"${SMOKE_MVP_TAG}\")" "Detalle de gestion accesible"

federation_donation_response="$(api_post_json "/api/federation/donations" "$(cat <<EOF
{"donorName":"${SMOKE_MVP_TAG} Donante Federación","donationDate":"${today_utc}","donationType":"Apoyo institucional","baseAmount":2000,"reference":"${SMOKE_MVP_TAG}-FED","notes":"Donación técnica de Federación para smoke MVP.","statusCatalogEntryId":1601}
EOF
)")"
federation_donation_id="$(extract_json_value "${federation_donation_response}" "data.id" "Donacion de Federación creada")"

federation_donation_detail="$(api_get "/api/federation/donations/${federation_donation_id}")"
assert_json_predicate "${federation_donation_detail}" "data.id === \"${federation_donation_id}\" && data.reference === \"${SMOKE_MVP_TAG}-FED\"" "Detalle de donacion de Federación accesible"

report_step "Alertas activas y wiring por modulo antes del cierre"
dashboard_alerts_active="$(api_get "/api/dashboard/alerts")"
assert_contains "${dashboard_alerts_active}" "${SMOKE_MVP_TAG}" "dashboard/alerts refleja registros activos del smoke"

donation_alerts_active="$(api_get "/api/donations/alerts")"
assert_contains "${donation_alerts_active}" "${SMOKE_MVP_TAG}" "Donatarias alerta sobre la donacion tecnica no aplicada"

financial_alerts_active="$(api_get "/api/financials/alerts/permits")"
assert_contains "${financial_alerts_active}" "${SMOKE_MVP_TAG}" "Financieras alerta sobre el oficio tecnico vigente por vencer"

federation_alerts_active="$(api_get "/api/federation/alerts")"
assert_contains "${federation_alerts_active}" "${SMOKE_MVP_TAG}" "Federacion alerta sobre gestion o donacion tecnica activa"

report_step "Aplicaciones y comisiones tecnicas"
donation_application_response="$(api_post_json "/api/donations/${donation_id}/applications" "$(cat <<EOF
{"beneficiaryName":"${SMOKE_MVP_TAG} Beneficiario","responsibleContactId":"${internal_contact_id}","responsibleName":"${SMOKE_MVP_TAG} Responsable","applicationDate":"${today_utc}","appliedAmount":400,"statusCatalogEntryId":1301,"verificationDetails":"Aplicación técnica parcial del smoke MVP.","closingDetails":null}
EOF
)")"
donation_application_id="$(extract_json_value "${donation_application_response}" "data.id" "Aplicacion de Donatarias creada")"

donation_progress="$(api_get "/api/donations/${donation_id}/progress")"
assert_json_predicate "${donation_progress}" "data.donationId === \"${donation_id}\" && data.applicationCount >= 1 && data.appliedPercentage > 0" "Progreso de Donatarias actualizado"

financial_credit_response="$(api_post_json "/api/financials/${financial_permit_id}/credits" "$(cat <<EOF
{"promoterContactId":"${internal_contact_id}","promoterName":"${SMOKE_MVP_TAG} Promotor","beneficiaryContactId":"${external_contact_id}","beneficiaryName":"${SMOKE_MVP_TAG} Beneficiario Crédito","phoneNumber":"5550003000","whatsAppPhone":"5550003000","authorizationDate":"${today_utc}","amount":5000,"notes":"Credito tecnico para smoke MVP."}
EOF
)")"
financial_credit_id="$(extract_json_value "${financial_credit_response}" "data.id" "Credito individual creado")"

financial_credit_detail="$(api_get "/api/financials/${financial_permit_id}/credits")"
assert_contains "${financial_credit_detail}" "${financial_credit_id}" "Listado de creditos incluye el credito tecnico"

financial_commission_response="$(api_post_json "/api/financials/credits/${financial_credit_id}/commissions" "$(cat <<EOF
{"commissionTypeId":1,"recipientCategory":"COMPANY","recipientContactId":null,"recipientName":"${SMOKE_MVP_TAG} Empresa","baseAmount":5000,"commissionAmount":250,"notes":"Comision tecnica de Financieras para smoke MVP."}
EOF
)")"
financial_commission_id="$(extract_json_value "${financial_commission_response}" "data.id" "Comision de Financieras creada")"

financial_commissions_detail="$(api_get "/api/financials/credits/${financial_credit_id}/commissions")"
assert_contains "${financial_commissions_detail}" "${financial_commission_id}" "Listado de comisiones financieras incluye el registro tecnico"

federation_participant_response="$(api_post_json "/api/federation/actions/${federation_action_id}/participants" "$(cat <<EOF
{"contactId":"${internal_contact_id}","participantSide":"INTERNAL","notes":"Participante técnico para smoke MVP."}
EOF
)")"
federation_participant_id="$(extract_json_value "${federation_participant_response}" "data.id" "Participante de Federacion creado")"

federation_action_detail_with_participant="$(api_get "/api/federation/actions/${federation_action_id}")"
assert_contains "${federation_action_detail_with_participant}" "${federation_participant_id}" "Detalle de gestion incluye al participante tecnico"

federation_application_response="$(api_post_json "/api/federation/donations/${federation_donation_id}/applications" "$(cat <<EOF
{"beneficiaryOrDestinationName":"${SMOKE_MVP_TAG} Destino","applicationDate":"${today_utc}","appliedAmount":1000,"statusCatalogEntryId":1701,"verificationDetails":"Aplicación técnica parcial de Federación para smoke MVP.","closingDetails":null}
EOF
)")"
federation_application_id="$(extract_json_value "${federation_application_response}" "data.id" "Aplicacion de Federación creada")"

federation_commission_response="$(api_post_json "/api/federation/applications/${federation_application_id}/commissions" "$(cat <<EOF
{"commissionTypeId":7,"recipientCategory":"THIRD_PARTY","recipientContactId":"${external_contact_id}","recipientName":"${SMOKE_MVP_TAG} Tercero","baseAmount":1000,"commissionAmount":75,"notes":"Comision tecnica de Federación para smoke MVP."}
EOF
)")"
federation_commission_id="$(extract_json_value "${federation_commission_response}" "data.id" "Comision de Federación creada")"

federation_commissions_detail="$(api_get "/api/federation/applications/${federation_application_id}/commissions")"
assert_contains "${federation_commissions_detail}" "${federation_commission_id}" "Listado de comisiones de Federación incluye el registro tecnico"

commissions_consolidated="$(api_get "/api/commissions/consolidated?q=${SMOKE_MVP_TAG}")"
assert_json_predicate "${commissions_consolidated}" "data.totalCount >= 2 && data.items.some(item => item.sourceModuleCode === \"FINANCIALS\") && data.items.some(item => item.sourceModuleCode === \"FEDERATION\")" "Comisiones consolidadas responden con origenes de Financieras y Federación"

report_step "Cierres formales y verificacion historica"
market_close_response="$(api_post_json "/api/markets/${market_id}/close" "$(cat <<EOF
{"reason":"Cierre técnico ${SMOKE_MVP_TAG}"}
EOF
)")"
assert_json_predicate "${market_close_response}" "data.statusCode === \"CLOSED\"" "Cierre formal de mercado ejecutado"

donation_close_response="$(api_post_json "/api/donations/${donation_id}/close" "$(cat <<EOF
{"reason":"Cierre técnico ${SMOKE_MVP_TAG}"}
EOF
)")"
assert_json_predicate "${donation_close_response}" "data.statusCode === \"CLOSED\"" "Cierre formal de Donatarias ejecutado"

financial_close_response="$(api_post_json "/api/financials/${financial_permit_id}/close" "$(cat <<EOF
{"reason":"Cierre técnico ${SMOKE_MVP_TAG}"}
EOF
)")"
assert_json_predicate "${financial_close_response}" "data.statusCode === \"CLOSED\"" "Cierre formal de Financieras ejecutado"

federation_action_close_response="$(api_post_json "/api/federation/actions/${federation_action_id}/close" "$(cat <<EOF
{"reason":"Cierre técnico ${SMOKE_MVP_TAG}"}
EOF
)")"
assert_json_predicate "${federation_action_close_response}" "data.statusCode === \"CLOSED\"" "Cierre formal de gestion de Federación ejecutado"

federation_donation_close_response="$(api_post_json "/api/federation/donations/${federation_donation_id}/close" "$(cat <<EOF
{"reason":"Cierre técnico ${SMOKE_MVP_TAG}"}
EOF
)")"
assert_json_predicate "${federation_donation_close_response}" "data.statusCode === \"CLOSED\"" "Cierre formal de donacion de Federación ejecutado"

dashboard_alerts_after_close="$(api_get "/api/dashboard/alerts")"
assert_not_contains "${dashboard_alerts_after_close}" "${SMOKE_MVP_TAG}" "dashboard/alerts ya no muestra registros cerrados del smoke"

bitacora_tagged="$(api_get "/api/bitacora?q=${SMOKE_MVP_TAG}&take=50")"
assert_json_predicate "${bitacora_tagged}" "Array.isArray(data) && data.length >= 10 && data.some(item => item.isCloseEvent === true)" "Bitacora devuelve eventos reales y cierres del smoke"

history_tagged="$(api_get "/api/history/closed-items?q=${SMOKE_MVP_TAG}")"
assert_json_predicate "${history_tagged}" "Array.isArray(data) && data.length >= 5 && data.every(item => item.hasFormalCloseEvent === true && item.historicalTimestampSource === \"FORMAL_CLOSE_EVENT\")" "Historico usa cierre formal para los registros tecnicos"

documents_after="$(api_get "/api/documents/integrity?take=5")"
assert_json_predicate "${documents_after}" "data.summary.totalDocumentRecords >= 0" "Integridad documental sigue accesible tras el smoke MVP"

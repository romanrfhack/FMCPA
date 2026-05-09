# Functional Track - Financial Permit Active Conflict Implementation Note

## Resumen
- Subetapa de `Track 5` para evitar permisos vigentes/conflictivos en Financieras.
- Se implementa validacion minima en `POST /api/financials` y `POST /api/financials/{permitId}/renew`.
- El bloqueo devuelve `409 Conflict` con `reasonCode=FINANCIAL_PERMIT_ACTIVE_CONFLICT`, `conflictingPermitId` y `currentRootPermitId`.
- Angular muestra el mensaje devuelto por backend y permite abrir el oficio vigente conflictivo desde la pantalla de Financieras.

## Regla exacta de conflicto
Existe conflicto cuando hay otro `FinancialPermit` que cumple todo lo siguiente:
- `IsCurrentVersion=true`.
- Su estatus asociado no es terminal (`ModuleStatusCatalogEntry.IsClosed=false`).
- Tiene la misma combinacion operativa normalizada:
  - `FinancialName`.
  - `InstitutionOrDependency`.
  - `PlaceOrStand`.
- En renovacion, se excluye la misma `CurrentRootPermitId` para que la cadena valida no choque consigo misma.

En alta nueva, la validacion se aplica cuando el estatus solicitado no es terminal. Un alta creada directamente en estatus terminal no representa permiso operativo activo y queda fuera del bloqueo minimo de esta subetapa.

La regla no usa traslape de fechas. Decision tecnica: en este modelo `IsCurrentVersion=true` es la marca operativa vigente de una cadena; permitir otra cadena vigente con la misma combinacion, aunque no traslape fechas, dejaria ambigua la operacion y debilitaria la renovacion formal como mecanismo correcto.

## Comparacion operativa
La normalizacion minima aplicada a los tres campos de la combinacion:
- `trim`.
- Colapso de espacios internos consecutivos a un solo espacio.
- Comparacion case-insensitive mediante mayusculas invariantes.
- Remocion basica de diacriticos por normalizacion Unicode.

Queda fuera de esta regla:
- Catalogo maestro de financieras, dependencias o stands.
- Sinonimos, alias, abreviaturas o equivalencias por puntuacion.
- Matching difuso o reglas por institucion.

## Cambios implementados
- Backend:
  - `FinancialsEndpoints` busca conflictos activos antes de crear un permiso nuevo no terminal.
  - `FinancialsEndpoints` busca conflictos activos antes de crear una renovacion, excluyendo la raiz de la cadena actual.
  - Nuevo contrato `FinancialPermitActiveConflictResponse`.
  - Prueba de regresion para alta conflictiva normalizada, alta no conflictiva, renovacion conflictiva y renovacion valida.
- Frontend:
  - Nuevo modelo `FinancialPermitActiveConflict`.
  - La pantalla de Financieras detecta `FINANCIAL_PERMIT_ACTIVE_CONFLICT`.
  - El formulario de alta y el formulario de renovacion muestran el error y ofrecen abrir el permiso conflictivo.
- Persistencia:
  - No se agrega migracion.
  - No se agrega indice ni constraint SQL en esta subetapa.
- Auditoria:
  - No se auditan intentos bloqueados; se conserva la bitacora solo para operaciones realmente registradas.

## Validacion local
Comandos base:

```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter "Financial_permit_create_and_renewal_block_active_operational_conflicts"
cd src/frontend
npm run build
```

Validacion runtime sugerida sobre base local aislada:

```bash
FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-hold-runtime \
FMCPA_SQL_PORT=14345 \
FMCPA_DB_NAME=FMCPA_Track5ActiveConflict_Runtime \
FMCPA_API_PORT=5106 \
FMCPA_WEB_PORT=4214 \
FMCPA_AUTH_BOOTSTRAP_PASSWORD='Track5Admin123' \
FMCPA_AUTH_OPERATOR_PASSWORD='Track5Operator123' \
FMCPA_AUTH_READONLY_PASSWORD='Track5Readonly123' \
FMCPA_AUTH_JWT_SIGNING_KEY='Track5ActiveConflictSigningKeyLocalOnly1234567890' \
bash scripts/local/reset-db.sh --force

FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-hold-runtime \
FMCPA_SQL_PORT=14345 \
FMCPA_DB_NAME=FMCPA_Track5ActiveConflict_Runtime \
FMCPA_API_PORT=5106 \
FMCPA_WEB_PORT=4214 \
FMCPA_AUTH_BOOTSTRAP_PASSWORD='Track5Admin123' \
FMCPA_AUTH_OPERATOR_PASSWORD='Track5Operator123' \
FMCPA_AUTH_READONLY_PASSWORD='Track5Readonly123' \
FMCPA_AUTH_JWT_SIGNING_KEY='Track5ActiveConflictSigningKeyLocalOnly1234567890' \
bash scripts/local/dev-up.sh
```

Casos a validar por HTTP:
- Crear permiso vigente base: esperado `201`.
- Crear otro permiso con misma combinacion normalizada: esperado `409 FINANCIAL_PERMIT_ACTIVE_CONFLICT`.
- Crear permiso con otra combinacion operativa: esperado `201`.
- Renovar hacia combinacion usada por otra cadena vigente: esperado `409 FINANCIAL_PERMIT_ACTIVE_CONFLICT`.
- Renovar hacia combinacion libre: esperado `201`; la cadena conserva un solo vigente.
- Abrir `/financials` en Angular y confirmar que el conflicto muestra mensaje y boton para abrir el oficio vigente conflictivo.

## Validacion ejecutada en esta sesion
- `dotnet restore src/backend/FMCPA.Backend.sln`: exitoso.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: exitoso, `0` warnings, `0` errores.
- `dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter "Financial_permit_create_and_renewal_block_active_operational_conflicts"`: `1/1` exitoso.
- `dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter "FullyQualifiedName~FinancialsPermitRenewalTests"`: `2/2` exitoso.
- `npm run build` en `src/frontend`: exitoso sin warnings.
- Runtime local aislado:
  - SQL Server local existente `fmcpa-sql-hold-runtime` en `14345`.
  - Base `FMCPA_Track5ActiveConflict_Runtime`.
  - API en `http://127.0.0.1:5106`.
  - Angular en `http://127.0.0.1:4214`.
  - `POST /api/financials` base: `201`.
  - `POST /api/financials` con misma combinacion normalizada: `409 FINANCIAL_PERMIT_ACTIVE_CONFLICT`.
  - `POST /api/financials` con stand/lugar distinto: `201`.
  - `POST /api/financials/{permitId}/renew` hacia combinacion usada por otra cadena vigente: `409 FINANCIAL_PERMIT_ACTIVE_CONFLICT`.
  - `POST /api/financials/{permitId}/renew` hacia combinacion libre: `201`; `renewal-chain.currentPermitId` apunta al nuevo permiso vigente.
  - Validacion UI con Playwright local: `ui_conflict_validation=passed`, confirmando mensaje y boton `Abrir oficio vigente`.

## Fuera de alcance
- Produccion y CI/CD.
- Catalogo maestro de financieras/dependencias.
- Workflow de aprobaciones o tareas.
- Versionado contractual completo.
- Constraint SQL sobre clave normalizada.
- Auditoria forense de intentos bloqueados.
- Ajustes historicos retroactivos sobre permisos ya duplicados.

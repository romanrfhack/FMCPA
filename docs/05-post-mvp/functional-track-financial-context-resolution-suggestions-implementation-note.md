# Track 5 - Financial Context Resolution Suggestions Implementation Note

## Que se implemento
- `GET /api/financials/current-permit` ahora devuelve una resolucion contextual enriquecida para casos no ambiguos.
- La respuesta incluye `currentPermit` cuando existe vigente/no terminal, `lastKnownPermit` cuando no hay vigente pero existe antecedente, `currentRootPermitId`, `suggestionCode`, `suggestionMessage` y `routeHint`.
- La UI de Financieras muestra una sugerencia operativa en `Buscar vigente` y en `Captura contextual de credito`.
- Con vigente, la UI permite abrir el permiso y continuar con la captura existente.
- Con antecedente historico o terminal, la UI permite abrir el ultimo permiso/cadena.
- Sin antecedente, la UI muestra que debe crearse un oficio nuevo y permite copiar el contexto al alta.

## Regla de sugerencia
- `USE_CURRENT_PERMIT`: existe permiso `IsCurrentVersion=true` y no terminal para la combinacion operativa normalizada.
- `RENEW_LAST_PERMIT`: no existe vigente para el contexto, pero existe antecedente historico no terminal relacionado por la misma combinacion normalizada.
- `REVIEW_TERMINAL_CHAIN`: el antecedente aplicable esta en estado terminal.
- `CREATE_NEW_PERMIT`: no existe permiso vigente ni antecedente para la combinacion consultada.

La sugerencia es solo orientacion de lectura. No crea permisos, no renueva automaticamente y no omite las reglas existentes de captura solo sobre permiso vigente/no terminal.

## Como validar localmente
1. Ejecutar `dotnet restore src/backend/FMCPA.Backend.sln`.
2. Ejecutar `dotnet build src/backend/FMCPA.Backend.sln`.
3. Ejecutar la prueba focal de Financieras:
   `dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --filter FullyQualifiedName~FinancialsPermitRenewalTests`.
4. Ejecutar `npm run build` en `src/frontend`.
5. Levantar backend local con una base aislada y consultar `GET /api/financials/current-permit` para:
   - contexto con vigente: `suggestionCode=USE_CURRENT_PERMIT`
   - contexto sin vigente pero con historico: `suggestionCode=RENEW_LAST_PERMIT`
   - contexto terminal: `suggestionCode=REVIEW_TERMINAL_CHAIN`
   - contexto sin antecedente: `suggestionCode=CREATE_NEW_PERMIT`
6. Abrir `/financials` y validar que la busqueda contextual muestre la sugerencia y las acciones minimas esperadas.

## Validacion ejecutada
- `dotnet restore src/backend/FMCPA.Backend.sln`: correcto, proyectos actualizados.
- `dotnet build src/backend/FMCPA.Backend.sln`: correcto, `0 Warning(s)`, `0 Error(s)`.
- `dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --filter FullyQualifiedName~FinancialsPermitRenewalTests`: correcto, `3/3` pruebas.
- `npm run build` en `src/frontend`: correcto, bundle generado sin warnings.
- `./scripts/local/reset-db.sh --force` con `FMCPA_DB_NAME=FMCPA_ContextSuggestions_20260508`, `FMCPA_SQL_PORT=14334`: correcto, migraciones aplicadas.
- `./scripts/local/run-backend.sh` con API `http://127.0.0.1:5111`: backend levantado y `/health` disponible.
- Script runtime `/tmp/fmcpa-context-suggestions-runtime.js`: confirma `USE_CURRENT_PERMIT`, `RENEW_LAST_PERMIT`, `CREATE_NEW_PERMIT` y `REVIEW_TERMINAL_CHAIN`.
- `./scripts/local/run-frontend.sh` con frontend `http://127.0.0.1:4215`: frontend levantado con proxy local.
- Script UI headless `/tmp/fmcpa-context-suggestions-ui.js`: confirma que Financieras muestra `CREATE_NEW_PERMIT` y copia el contexto al alta de oficio.

## Que quedo fuera
- Catalogo maestro de financieras, dependencias o stands.
- Workflow de renovacion, aprobaciones o asignaciones.
- Versionado contractual completo.
- Automatizacion de alta o renovacion desde la sugerencia.
- Auditoria de consultas o sugerencias.
- Migracion nueva; la respuesta se compone en lectura sobre datos existentes.

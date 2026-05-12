# Financial Context Card Implementation Note

## Contexto
- Fecha: 2026-05-12
- Track: Track 5 post-MVP, evolucion funcional acotada de Financieras
- Objetivo: consultar una combinacion `financialName` + `institutionOrDependency` + `placeOrStand` y mostrar una ficha operativa minima con estado, antecedente, cadena, creditos, comisiones y acciones razonables.

## Que se implemento
- Backend: `GET /api/financials/context-card`.
- Contratos de respuesta para ficha, resumen de cadena, resumen de creditos y resumen de comisiones.
- Reutilizacion de la resolucion contextual actual con `suggestionCode`.
- Reutilizacion de la cadena de renovacion y sus agregados de creditos/comisiones cuando existe permiso vigente o antecedente.
- UI Angular en Financieras con panel compacto de ficha operativa contextual.
- Prueba de regresion que cubre:
  - `USE_CURRENT_PERMIT`
  - `RENEW_LAST_PERMIT`
  - `CREATE_NEW_PERMIT`
  - `REVIEW_TERMINAL_CHAIN`

## Datos que consolida la ficha
- `resolution`: `suggestionCode`, `suggestionMessage`, `currentPermit`, `lastKnownPermit`, `currentRootPermitId` y `routeHint`.
- `renewalChainSummary`: permiso vigente, total de permisos de la cadena, secuencia vigente y periodo global.
- `creditSummary`: total de creditos y monto total.
- `commissionSummary`: comision total de promotor, administracion y terceros.
- `availableActions`: acciones visibles para la UI.

El periodo global de `renewalChainSummary` se calcula desde las vigencias de los permisos de la cadena. No representa fechas de creditos ni un reporte historico-contable.

## Acciones disponibles
- `USE_CURRENT_PERMIT`: `CAPTURE_CREDIT`, `VIEW_CURRENT_PERMIT`, `VIEW_CHAIN`.
- `RENEW_LAST_PERMIT`: `PREPARE_RENEWAL`, `VIEW_CURRENT_PERMIT`, `VIEW_CHAIN`.
- `CREATE_NEW_PERMIT`: `CREATE_PERMIT`.
- `REVIEW_TERMINAL_CHAIN`: `VIEW_CURRENT_PERMIT`, `VIEW_CHAIN`.

Las acciones son orientacion de UI. Las mutaciones reales conservan sus endpoints y validaciones actuales:
- capturar credito: `POST /api/financials/{permitId}/credits`
- crear oficio: `POST /api/financials`
- renovar: `POST /api/financials/{permitId}/renew`

## Autorizacion
- La ficha requiere `FINANCIALS_READ`.
- Las acciones reales siguen requiriendo `FINANCIALS_WRITE` cuando modifican datos.
- La consulta de ficha no se audita.

## Como validar localmente
1. Restaurar y compilar backend.
2. Compilar frontend si se toca Angular.
3. Ejecutar prueba focal de Financieras.
4. Levantar backend y frontend locales con una base aislada.
5. Consultar `GET /api/financials/context-card` con contextos que produzcan `USE_CURRENT_PERMIT`, `RENEW_LAST_PERMIT`, `CREATE_NEW_PERMIT` y `REVIEW_TERMINAL_CHAIN`.
6. Verificar en UI que la ficha muestre estado, resumenes y acciones correctas.

## Validacion ejecutada
- `dotnet restore src/backend/FMCPA.Backend.sln`: exitoso.
- `dotnet build src/backend/FMCPA.Backend.sln`: primer intento bloqueado por artefactos locales `bin/obj` con ruta recursiva previa; se limpiaron artefactos locales y el segundo intento fue exitoso con `0` warnings y `0` errores.
- `npm run build` en `src/frontend`: exitoso. Un primer intento reporto warning de presupuesto CSS en `financials-page.component.ts` por `259 bytes`; se retiraron estilos redundantes nuevos y el rerun quedo sin warnings.
- `dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --filter FinancialsPermitRenewalTests`: exitoso, `6/6`.
- `dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj`: exitoso, `249/249`.
- `FMCPA_API_PORT=5097 FMCPA_WEB_PORT=4207 FMCPA_SQL_PORT=14347 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-context-card FMCPA_DB_NAME=FMCPA_ContextCard FMCPA_AUTH_BOOTSTRAP_PASSWORD=Track5Context123 FMCPA_AUTH_OPERATOR_PASSWORD=Track5Operator123 FMCPA_AUTH_READONLY_PASSWORD=Track5Readonly123 FMCPA_AUTH_JWT_SIGNING_KEY=Track5ContextCardSigningKeyLocalOnly1234567890 ./scripts/local/dev-up.sh --no-frontend`: aplico migraciones, pero el backend no pudo iniciar porque `5097` ya estaba ocupado por un proceso local previo.
- `FMCPA_API_PORT=5098 FMCPA_WEB_PORT=4207 FMCPA_SQL_PORT=14347 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-context-card FMCPA_DB_NAME=FMCPA_ContextCard FMCPA_AUTH_BOOTSTRAP_PASSWORD=Track5Context123 FMCPA_AUTH_OPERATOR_PASSWORD=Track5Operator123 FMCPA_AUTH_READONLY_PASSWORD=Track5Readonly123 FMCPA_AUTH_JWT_SIGNING_KEY=Track5ContextCardSigningKeyLocalOnly1234567890 ./scripts/local/run-backend.sh`: exitoso, backend en `http://127.0.0.1:5098`.
- `node /tmp/fmcpa-context-card-validation.js`: exitoso tras incluir `X-FMCPA-Client` en mutaciones de seed; valido `USE_CURRENT_PERMIT`, `RENEW_LAST_PERMIT`, `CREATE_NEW_PERMIT` y `REVIEW_TERMINAL_CHAIN`.
- `FMCPA_API_PORT=5098 FMCPA_WEB_PORT=4207 FMCPA_SQL_PORT=14347 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-context-card FMCPA_DB_NAME=FMCPA_ContextCard FMCPA_AUTH_BOOTSTRAP_PASSWORD=Track5Context123 FMCPA_AUTH_OPERATOR_PASSWORD=Track5Operator123 FMCPA_AUTH_READONLY_PASSWORD=Track5Readonly123 FMCPA_AUTH_JWT_SIGNING_KEY=Track5ContextCardSigningKeyLocalOnly1234567890 ./scripts/local/run-frontend.sh`: exitoso, Angular en `http://127.0.0.1:4207`.
- `npx playwright install chromium`: exitoso.
- `npx playwright test --config=/tmp/fmcpa-playwright.config.js --reporter=line`: exitoso, `1 passed`.
- `git diff --check`: sin errores.
- `jq empty docs/05-post-mvp/security-authorization-surface-guardrails.json`: no ejecutable en este entorno porque `jq` no esta instalado; el manifiesto quedo cubierto por la suite de regresion de autorizacion.

## Fuera de alcance
- Catalogo maestro de financieras, dependencias o stands.
- Workflow complejo, aprobaciones, asignaciones o tareas persistidas.
- BI, reportes avanzados o snapshot historico persistido.
- Migracion o nueva entidad persistida de contexto.
- Auditoria de la consulta de ficha.
- Produccion y CI/CD.

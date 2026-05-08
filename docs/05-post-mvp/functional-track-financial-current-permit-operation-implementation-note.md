# Functional Track - Financial Current Permit Operation Implementation Note

## Que se implemento
- Se endurecio la captura operativa de Financieras para que los creditos nuevos solo puedan registrarse sobre permisos vigentes y no terminales.
- Se endurecio la captura de comisiones para que solo puedan agregarse a creditos cuyo `FinancialPermit` asociado sigue vigente y no terminal.
- `POST /api/financials/{permitId}/credits` devuelve `409 Conflict` cuando el permiso es historico/no vigente o terminal, antes de crear credito, participaciones o auditoria.
- `POST /api/financials/credits/{creditId}/commissions` devuelve `409 Conflict` cuando el credito pertenece a un permiso historico/no vigente o terminal, antes de crear comision, participaciones o auditoria.
- Las respuestas de bloqueo incluyen `message`, `reasonCode`, `permitId`, `currentRootPermitId` y `currentPermitId` cuando puede sugerirse el permiso vigente de la cadena.
- La pantalla de Financieras mantiene la consulta de historicos, pero oculta formularios de alta de credito/comision en permisos historicos o terminales, muestra modo `Solo lectura` y permite navegar al permiso vigente cuando existe.
- No se creo migracion nueva; la regla usa `IsCurrentVersion`, `CurrentRootPermitId` y el estatus existente del permiso.

## Regla elegida para captura
- Un permiso puede recibir nuevos creditos solo si cumple ambas condiciones:
  - `IsCurrentVersion = true`.
  - `StatusCatalogEntry.IsClosed = false`.
- Una comision nueva puede registrarse solo si el credito pertenece a un permiso que cumple esas mismas condiciones.
- Los permisos historicos renovados y los permisos terminales quedan disponibles para lectura, cadena, resumen, creditos existentes y comisiones existentes, pero no para captura nueva.
- La validacion se aplica en backend antes de validaciones dependientes de datos externos y antes de cualquier `AuditEvent`, para evitar auditoria falsa de operaciones bloqueadas.

## Historico vs vigente
- Historico renovado:
  - responde `409 Conflict`;
  - usa `reasonCode = FINANCIAL_PERMIT_NOT_CURRENT`;
  - devuelve la raiz (`currentRootPermitId`) y el `currentPermitId` sugerido;
  - el mensaje indica capturar en el permiso vigente de la cadena.
- Permiso terminal:
  - responde `409 Conflict`;
  - usa `reasonCode = FINANCIAL_PERMIT_TERMINAL`;
  - conserva el mensaje terminal existente de la plataforma;
  - no intenta reabrir ni derivar una operacion nueva.
- El historico sigue visible en `GET /api/financials/{permitId}`, `GET /api/financials/{permitId}/credits` y `GET /api/financials/{permitId}/renewal-chain`.

## Como validar localmente
1. Restaurar y compilar backend:
   ```bash
   dotnet restore src/backend/FMCPA.Backend.sln
   dotnet build src/backend/FMCPA.Backend.sln --no-restore
   ```
2. Ejecutar la prueba focal:
   ```bash
   dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter FinancialsPermitRenewalTests
   ```
3. Compilar frontend:
   ```bash
   cd src/frontend
   npm run build
   ```
4. Levantar backend y frontend contra una base local aislada.
5. Crear un permiso inicial, registrar un credito y una comision.
6. Renovar el permiso para dejar el permiso inicial como historico y el nuevo como vigente.
7. Validar:
   ```bash
   curl -i http://127.0.0.1:5099/api/financials/${HISTORICAL_PERMIT_ID}/credits \
     -H "Authorization: Bearer ${ADMIN_TOKEN}" \
     -H "Content-Type: application/json" \
     -d '{"promoterName":"Promotor historico","beneficiaryName":"Beneficiario historico","authorizationDate":"2026-04-15","amount":1000}'
   ```
   Debe responder `409` con `reasonCode=FINANCIAL_PERMIT_NOT_CURRENT`.
8. Capturar credito en `${CURRENT_PERMIT_ID}` y confirmar `201`.
9. Intentar una comision sobre un credito del permiso historico y confirmar `409`.
10. Cerrar formalmente el permiso vigente y confirmar que nuevas altas de credito/comision responden `409 FINANCIAL_PERMIT_TERMINAL`.
11. Abrir `/financials`, seleccionar el permiso historico o terminal y confirmar que se muestra `Solo lectura` sin formularios de alta.

## Validacion ejecutada
- `dotnet restore src/backend/FMCPA.Backend.sln`: exitoso, proyectos al dia.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: exitoso, `0` warnings y `0` errores.
- `dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter FinancialsPermitRenewalTests`: exitoso, `1/1`.
- `dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build`: exitoso, `232/232`.
- `npm run build` en `src/frontend`: exitoso.
- `dotnet ef database update --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --context PlatformDbContext --no-build` contra SQL local `127.0.0.1:14345`, base `FMCPA_Track5CurrentPermitOperation_Runtime`: exitoso.
- API local levantada en `http://127.0.0.1:5099`: `/health` respondio `Healthy`.
- Validacion HTTP runtime con cadena de dos permisos:
  - permiso historico: `bf31423b-cab2-402c-bf72-00dba86b1f0e`;
  - permiso vigente: `d143037c-520d-4a0d-ad5f-741b7643b6b9`;
  - alta de credito en historico: `409 FINANCIAL_PERMIT_NOT_CURRENT`;
  - alta de credito en vigente: `201`;
  - alta de comision sobre credito historico: `409 FINANCIAL_PERMIT_NOT_CURRENT`;
  - alta de comision sobre credito vigente: `201`;
  - cierre formal del vigente: `200`;
  - alta de credito en permiso terminal: `409 FINANCIAL_PERMIT_TERMINAL`;
  - alta de comision sobre credito de permiso terminal: `409 FINANCIAL_PERMIT_TERMINAL`;
  - consulta de cadena con `READONLY`: `200`, `permitsCount=2`, `totalCreditsCount=2`, `totalCommissionsCount=2`.
- Frontend local levantado con `FMCPA_WEB_PORT=4203 FMCPA_API_PORT=5099 ./scripts/local/run-frontend.sh`.
- Validacion UI con Playwright: exitosa, `1/1`; la pantalla `/financials` mostro `Solo lectura / histórico` y `Solo lectura / terminal`, ocultando `Registrar crédito` y `Registrar comisión`.

## Que quedo fuera
- Workflow complejo de ajustes historicos.
- Aprobaciones multinivel.
- Reapertura de permisos terminales.
- Versionado contractual completo.
- Comparativos avanzados o BI.
- Auditoria forense de intentos fallidos.
- Migracion nueva.
- Cambios de produccion o CI/CD.

# Functional Track - Financial Permit Renewal Chain Implementation Note

## Que se implemento
- Se agrego `GET /api/financials/{permitId}/renewal-chain` para consultar una autorizacion renovada como una cadena operativa unica.
- La respuesta incluye `currentRootPermitId`, permiso vigente, permisos ordenados de la cadena, `renewalSequence`, periodos, estatus e indicador de vigente.
- La misma respuesta incluye un resumen agregado de creditos y comisiones ya existentes en todos los permisos de la cadena.
- La respuesta devuelve tambien los creditos de la cadena usando el contrato existente de creditos de Financieras.
- La pantalla de Financieras agrega un panel minimo de cadena con historial de renovaciones, vigente vs historicos, resumen agregado y creditos de la cadena.
- No se creo migracion nueva en este paso; se reutiliza la migracion previa `Track5FinancialPermitRenewal` y las relaciones ya existentes en `FinancialPermit`.

## Como se determina la cadena
- El permiso consultado se carga primero por `permitId`.
- La raiz se resuelve con `CurrentRootPermitId`; si el valor no esta poblado, se usa el `Id` del propio permiso como fallback defensivo.
- Los permisos de la cadena se obtienen por la raiz: `CurrentRootPermitId == rootPermitId` o `Id == rootPermitId`.
- El orden operativo se define por `RenewalSequence` y despues por `CreatedUtc`.
- El permiso vigente se determina primero por `IsCurrentVersion=true`; si hubiera datos inconsistentes, se toma el de mayor `RenewalSequence`.
- La consulta no reescribe permisos historicos ni mueve creditos/comisiones existentes.

## Agregados incluidos
- `permitsCount`.
- `totalCreditsCount`.
- `totalCreditsAmount`.
- `totalCommissionsCount`.
- `totalCommissionsAmount`.
- `totalPromoterCommission`, tomando comisiones con `CommissionType.Code = PROMOTER`.
- `totalAdminCommission`, tomando comisiones con `CommissionType.Code = ADMINISTRATION`.
- `totalThirdPartyCommission`, tomando comisiones con `RecipientCategory = THIRD_PARTY`.
- `operationFrom` y `operationTo`, calculadas desde la fecha de autorizacion minima y maxima de los creditos de la cadena cuando existen creditos.

## Como validar localmente
1. Restaurar y compilar backend:
   ```bash
   dotnet restore src/backend/FMCPA.Backend.sln
   dotnet build src/backend/FMCPA.Backend.sln --no-restore
   ```
2. Ejecutar prueba focal de renovacion:
   ```bash
   dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter FinancialsPermitRenewalTests
   ```
3. Compilar frontend:
   ```bash
   cd src/frontend
   npm run build
   ```
4. Aplicar migraciones sobre una base SQL local aislada:
   ```bash
   ConnectionStrings__PlatformDatabase="Server=127.0.0.1,14345;Database=FMCPA_Track5PermitChain_Runtime;User Id=sa;Password=<password>;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" \
     dotnet ef database update \
       --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj \
       --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj \
       --context PlatformDbContext \
       --no-build
   ```
5. Levantar backend con bootstrap local y frontend con proxy local:
   ```bash
   ConnectionStrings__PlatformDatabase="Server=127.0.0.1,14345;Database=FMCPA_Track5PermitChain_Runtime;User Id=sa;Password=<password>;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" \
   Auth__Jwt__SigningKey="<clave-local-de-al-menos-32-bytes>" \
   Auth__Bootstrap__Password="<admin-password>" \
   Auth__Bootstrap__Operator__Password="<operator-password>" \
   Auth__Bootstrap__ReadOnly__Password="<readonly-password>" \
     dotnet run --project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --no-build --urls http://127.0.0.1:5098

   FMCPA_WEB_PORT=4202 FMCPA_API_PORT=5098 ./scripts/local/run-frontend.sh
   ```
6. Crear un permiso inicial, un credito y comision; renovar el permiso; crear otro credito y comisiones sobre el permiso renovado.
7. Consultar:
   ```bash
   curl -s http://127.0.0.1:5098/api/financials/${PERMIT_ID}/renewal-chain \
     -H "Authorization: Bearer ${ADMIN_TOKEN}"
   ```
8. Confirmar que la respuesta tenga dos permisos ordenados, uno vigente, dos creditos y los acumulados esperados de montos y comisiones.
9. Abrir `http://127.0.0.1:4202/financials`, seleccionar el permiso y confirmar que el panel `Resumen de cadena` muestra el historial, totales y creditos agregados.

## Validacion ejecutada
- `dotnet restore src/backend/FMCPA.Backend.sln`: exitoso.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: exitoso, `0` warnings y `0` errores.
- `dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter FinancialsPermitRenewalTests`: exitoso, `1/1`.
- `dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build`: exitoso, `232/232`.
- `npm run build` en `src/frontend`: exitoso.
- `dotnet ef database update` contra SQL local aislado `127.0.0.1:14345`, base `FMCPA_Track5PermitChain_Runtime`: exitoso.
- API local levantada en `http://127.0.0.1:5098`: `/health` respondio `Healthy`.
- Validacion HTTP runtime:
  - login `ADMIN`: `200`;
  - login `READONLY`: `200`;
  - alta de permiso inicial: `201`;
  - alta de credito inicial y comision administrativa: exitosas;
  - renovacion del permiso: `201`;
  - alta de credito renovado y comisiones de promotor/tercero: exitosas;
  - `GET /api/financials/{permitId}/renewal-chain`: `200`;
  - respuesta con `permitsCount=2`, `totalCreditsCount=2`, `totalCreditsAmount=3000`, `totalCommissionsCount=3`, `totalCommissionsAmount=425`, `totalPromoterCommission=150`, `totalAdminCommission=200`, `totalThirdPartyCommission=75`, `operationFrom=2026-03-15`, `operationTo=2026-04-10`;
  - `READONLY` pudo consultar la cadena con `200` y recibio `403` al intentar renovar.
- Validacion UI con Playwright contra `http://127.0.0.1:4202/financials`: exitosa; el panel de cadena mostro montos `3,000.00`, `150.00`, `200.00` y `75.00`.

## Que quedo fuera
- Comparativos avanzados entre renovaciones.
- BI, graficas o analitica pesada.
- Workflow complejo, aprobaciones multinivel o tareas persistidas.
- Versionado contractual completo.
- Reglas nuevas de negocio sobre vigencia contractual.
- Migracion nueva para este paso.
- Reescritura, movimiento o normalizacion de creditos/comisiones historicas.
- Cambios de produccion o CI/CD.

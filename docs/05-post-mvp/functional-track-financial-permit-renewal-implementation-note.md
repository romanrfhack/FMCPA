# Functional Track - Financial Permit Renewal Implementation Note

## Que se implemento
- Se abrio `Track 5` con una renovacion formal minima de oficios/autorizaciones de Financieras sobre `FinancialPermit`.
- `FinancialPermit` ahora registra `RenewedFromPermitId`, `CurrentRootPermitId`, `IsCurrentVersion` y `RenewalSequence`.
- Se agrego `POST /api/financials/{permitId}/renew` para crear un nuevo permiso desde uno vigente, con nuevo periodo y valores opcionales de lugar/stand, horario, terminos negociados y observaciones.
- Listado y detalle de Financieras exponen vigente/historico, raiz de renovacion, secuencia y relacion con el permiso anterior.
- El detalle incluye un historial simple de renovaciones de la misma raiz, ordenado por secuencia.
- Las alertas de permisos y el dashboard financiero operan solo sobre permisos vigentes (`IsCurrentVersion=true`).
- La UI de Financieras agrega accion `Renovar`, formulario minimo, indicador de vigente/historico y vista simple del historial.
- La bitacora registra `FINANCIAL_PERMIT_RENEWED` reutilizando `AuditEvent`.
- Se agrego la migracion `Track5FinancialPermitRenewal`.

## Politica de renovacion
- Renovar no edita ni sobrescribe el oficio anterior.
- El permiso anterior permanece como historico no vigente.
- El nuevo permiso queda vigente dentro de la misma raiz de renovacion.
- La cadena se determina por `CurrentRootPermitId`; el orden se determina por `RenewalSequence`.
- Solo se puede renovar un permiso vigente y no terminal.
- Un permiso historico renovado no participa en alertas operativas de vencimiento/renovacion.
- La renovacion usa `FINANCIALS_WRITE`: `ADMIN` y `OPERATOR` pueden renovar; `READONLY` recibe `403`.

## Como se determina el vigente
- Para permisos existentes antes de la migracion, `CurrentRootPermitId = Id`, `RenewalSequence = 0` e `IsCurrentVersion = true`.
- Al renovar:
  - el nuevo permiso recibe `RenewedFromPermitId = permiso anterior`;
  - hereda `CurrentRootPermitId` de la cadena;
  - recibe `RenewalSequence = anterior + 1`;
  - queda `IsCurrentVersion = true`;
  - el anterior pasa a `IsCurrentVersion = false`.
- La API no crea `RenewedToPermitId`; la relacion hacia adelante se consulta por `RenewedFromPermitId` o por historial de la raiz.

## Impacto en alertas
- `GET /api/financials/alerts/permits` filtra permisos no vigentes.
- El resumen/alertas financieras del dashboard tambien filtran permisos no vigentes.
- El permiso renovado anterior se sigue viendo en listado/detalle como historico, pero no ensucia la cola operativa como vencido vigente.

## Como validar localmente
1. Restaurar y compilar backend:
   ```bash
   dotnet restore src/backend/FMCPA.Backend.sln
   dotnet build src/backend/FMCPA.Backend.sln
   ```
2. Compilar frontend:
   ```bash
   cd src/frontend
   npm run build
   ```
3. Aplicar migracion sobre una base SQL aislada:
   ```bash
   ConnectionStrings__PlatformDatabase="Server=127.0.0.1,14345;Database=FMCPA_Track5PermitRenewal_Runtime;User Id=sa;Password=<password>;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" \
     dotnet ef database update \
       --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj \
       --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj \
       --context PlatformDbContext
   ```
4. Levantar API local con bootstrap de roles:
   ```bash
   ConnectionStrings__PlatformDatabase="Server=127.0.0.1,14345;Database=FMCPA_Track5PermitRenewal_Runtime;User Id=sa;Password=<password>;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" \
   ASPNETCORE_URLS="http://127.0.0.1:5080" \
   Auth__Jwt__SigningKey="<clave-local-de-al-menos-32-bytes>" \
   Auth__Bootstrap__Password="<admin-password>" \
   Auth__Bootstrap__Operator__Password="<operator-password>" \
   Auth__Bootstrap__ReadOnly__Password="<readonly-password>" \
     dotnet run --project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --no-build
   ```
5. Iniciar sesion como `ADMIN`, crear un oficio inicial, consultar alertas, renovar, consultar detalle/historial/listado/alertas/dashboard y verificar `FINANCIAL_PERMIT_RENEWED` en bitacora.
6. Iniciar sesion como `READONLY` y confirmar `403` en `POST /api/financials/{permitId}/renew`.

## Validacion ejecutada
- `dotnet restore src/backend/FMCPA.Backend.sln`: exitoso.
- `dotnet build src/backend/FMCPA.Backend.sln`: exitoso, `0` warnings, `0` errores.
- `dotnet ef migrations add Track5FinancialPermitRenewal ...`: exitoso.
- `dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build`: exitoso, `228/228`.
- `npm run build` en `src/frontend`: exitoso.
- `dotnet ef database update` contra `localhost,1433`: fallo por SQL Server no accesible; no fue bug funcional.
- `dotnet ef database update` contra SQL aislado `fmcpa-sql-hold-runtime` en `127.0.0.1:14345`, base `FMCPA_Track5PermitRenewal_Runtime`: exitoso.
- API local levantada sobre la base aislada: `/health` `Healthy`.
- Validacion HTTP runtime:
  - login `ADMIN`: `200`;
  - alta de oficio inicial: `201`;
  - alerta vencida antes de renovar: `1`;
  - renovacion ADMIN: `201`;
  - detalle anterior: `isCurrentVersion=false`, `alertState=HISTORICAL`;
  - detalle/listado/historial: cadena con `2` permisos y nuevo vigente;
  - alertas despues: el permiso anterior renovado ya no aparece;
  - dashboard financiero: el permiso anterior renovado no aparece como alerta;
  - bitacora: `FINANCIAL_PERMIT_RENEWED` presente;
  - renovacion con `READONLY`: `403`.

## Fuera de alcance
- Workflow complejo.
- Aprobaciones multinivel.
- Versionado contractual completo.
- Comparacion de versiones.
- Borrado o sobrescritura de oficios historicos.
- Scheduler/notificaciones.
- Cambios de produccion o CI/CD.

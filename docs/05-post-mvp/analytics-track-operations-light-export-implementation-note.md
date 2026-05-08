# Track 4 - Operations Light Export Implementation Note

## Que se implemento
- Se agrego `GET /api/operations/summary/export` para exportar CSV ligero del resumen operativo transversal.
- Se agrego `GET /api/operations/work-queue/export` para exportar CSV ligero de la bandeja transversal.
- Ambos endpoints reutilizan la misma composicion de `/api/operations/summary` y `/api/operations/work-queue`.
- La UI `/operations` agrega acciones `Exportar summary` y `Exportar bandeja`.
- Las descargas responden con `Content-Disposition: attachment`, `text/csv; charset=utf-8` y cache `no-store`.

## Que exporta cada endpoint
`GET /api/operations/summary/export`:
- `timeWindowCode`
- `sectionCode`
- `metricCode`
- `categoryCode`
- `moduleCode`
- `moduleName`
- `label`
- `value`
- `amount`
- `severityCode`
- `routeHint`

Incluye KPIs de negocio permitidos, metricas documentales visibles y metricas de seguridad solo cuando el usuario tiene permisos de administracion de seguridad.

`GET /api/operations/work-queue/export`:
- `timeWindowCode`
- `workItemKey`
- `categoryCode`
- `workItemType`
- `severity`
- `moduleCode`
- `moduleName`
- `title`
- `summary`
- `reasonCode`
- `actionKind`
- `actionLabel`
- `routeHint`
- `contextLabel`
- `quickActionCode`
- `quickActionLabel`
- `entityType`
- `entityId`
- `documentId`
- `relevantUtc`

## Filtros que respeta
- Summary export respeta `timeWindowCode`, `fromUtc` y `toUtc`.
- Work queue export respeta `categoryCode`, `severityCode`, `timeWindowCode`, `fromUtc`, `toUtc`, `skip` y `take`.
- `take` se normaliza con el mismo limite existente de work queue: maximo `200`.
- El filtrado por permisos es el mismo del centro operativo: negocio/documentos segun permisos efectivos y seguridad solo con `USERS_ADMIN`.

## Como validar localmente
Comandos de build/pruebas:

```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter OperationsCenterTests
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter "AuthorizationRegressionTests|AuthorizationSurfaceGuardrailTests"
npm run build
npm test -- --watch=false
git diff --check
```

Resultados de la corrida local:
- `dotnet restore`: exitoso, proyectos al dia.
- `dotnet build`: exitoso, `0` warnings y `0` errores.
- `OperationsCenterTests`: `4/4` exitosos, incluyendo CSV, headers y permisos de seguridad.
- Suite focal de autorizacion/guardrails: `223/223` exitosos.
- `npm run build`: exitoso.
- `npm test -- --watch=false`: `22/22` exitosos.
- `git diff --check`: exitoso, sin errores de whitespace.

Validacion HTTP manual con backend local:

```bash
curl -i "http://127.0.0.1:5080/api/operations/summary/export?timeWindowCode=NEXT_30_DAYS" -H "Authorization: Bearer ${ADMIN_TOKEN}"
curl -i "http://127.0.0.1:5080/api/operations/work-queue/export?timeWindowCode=NEXT_30_DAYS&severityCode=HIGH&take=200" -H "Authorization: Bearer ${ADMIN_TOKEN}"
curl -i "http://127.0.0.1:5080/api/operations/work-queue/export?categoryCode=SECURITY&take=200" -H "Authorization: Bearer ${READONLY_TOKEN}"
```

Resultado esperado:
- `200 OK`
- `Content-Type: text/csv; charset=utf-8`
- `Content-Disposition` con `operations-summary` u `operations-work-queue`
- `Cache-Control: no-store`
- El rol sin `USERS_ADMIN` no recibe items de seguridad.

## Que quedo fuera
- XLSX.
- BI pesado o reportes ejecutivos complejos.
- Exportaciones masivas sin limite.
- Scheduler, snapshots o persistencia de metricas.
- Nuevas quick actions o workflow.

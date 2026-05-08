# Track 4 - Operations Actionability Implementation Note

## Que se implemento
- `GET /api/operations/work-queue` ahora expone metadatos de accion por item:
  - `routeHint`
  - `actionKind`
  - `actionLabel`
  - `contextLabel`
  - `quickActionCode`
  - `quickActionLabel`
- La UI `/operations` muestra un boton claro de `Ir a resolver`, badge de accion y quick action cuando el backend la declara.
- Las pruebas de `OperationsCenterTests` validan items de negocio, documentos y seguridad con rutas utiles, action metadata, quick action de desbloqueo y exclusion de seguridad para usuarios sin `USERS_ADMIN`.

## Deep links
- Negocio reutiliza `NavigationPath` de las alertas del dashboard cuando es una ruta local valida; si falta o es invalida, usa la ruta base permitida del modulo (`/markets`, `/donatarias`, `/financials`, `/federation`).
- Documentos enruta:
  - completitud hacia el contexto de origen cuando existe;
  - integridad hacia `/documents/work-queue`;
  - retencion hacia `/documents/review` solo para `ADMIN`, o `/documents/work-queue` para otros roles con lectura documental.
- Seguridad enruta a `/admin/security` y solo aparece para usuarios con `USERS_ADMIN`.

## Quick actions permitidas
- `UNLOCK_USER`: desbloqueo de usuario bloqueado desde la bandeja transversal.
- Reutiliza `POST /api/admin/users/{id}/unlock`.
- La autorizacion real sigue en backend con `USERS_ADMIN`; el frontend solo muestra la accion cuando el item trae `quickActionCode`.

## Fuera de alcance
- Workflow complejo.
- Asignaciones, ownership o SLA.
- Aprobaciones.
- Acciones masivas.
- Ediciones complejas desde `/operations`.
- Nuevos endpoints destructivos.
- BI pesado, exportaciones complejas o analitica avanzada.

## Validacion local
Comandos ejecutados durante la implementacion:

```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
npm run build
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter OperationsCenterTests
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter "AuthorizationRegressionTests|AuthorizationSurfaceGuardrailTests"
npm test -- --watch=false
```

Resultados:
- `dotnet restore`: proyectos restaurados correctamente.
- Backend build: exitoso, `0 warnings`, `0 errors`.
- Frontend build: exitoso.
- `OperationsCenterTests`: `2/2` aprobados; cubre negocio, documentos, seguridad, deep links y quick action `UNLOCK_USER`.
- Autorizacion/guardrails focales: `200/200` aprobados.
- Frontend tests: `22/22` aprobados.

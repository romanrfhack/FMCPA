# Track 4 - Operations Center Implementation Note

## Que se implemento
- `GET /api/operations/summary` como resumen operativo transversal calculado en lectura.
- `GET /api/operations/work-queue` como bandeja transversal de lectura y priorizacion.
- Contratos backend bajo `FMCPA.Api.Contracts.Operations`.
- Vista Angular `/operations` con KPIs principales, secciones de operacion, documentos y seguridad, y bandeja unificada.
- Pruebas de regresion para composicion, permisos y exclusion de modulos/superficies no permitidas.
- La accionabilidad posterior de la bandeja queda documentada en `analytics-track-operations-actionability-implementation-note.md`.

## Senales consolidadas
- Negocio: KPIs ya derivados del dashboard ejecutivo, filtrados por permisos de modulo.
- Documentos: `GET /api/documents/summary` y composicion de `GET /api/documents/work-queue`, filtrados por permisos `MARKETS_READ`, `DONATIONS_READ` y `FEDERATION_READ`.
- Seguridad: summary de observabilidad y usuarios bloqueados solo para usuarios con `USERS_ADMIN`.

## Priorizacion
- `HIGH`: bloqueo operativo, integridad documental rota, vencimiento critico, donacion sin aplicar o usuario bloqueado.
- `MEDIUM`: atencion prioritaria sin bloqueo inmediato, por ejemplo vencimientos proximos, parciales o actividad de seguridad reciente.
- `LOW`: seguimiento, revision o contexto informativo accionable.

La severidad se expone en la respuesta como convencion reutilizable; no se persiste ni abre scoring.

## Permisos
- La entrada a `/api/operations/*` usa `DASHBOARD_READ`.
- Cada seccion se filtra por permisos efectivos:
  - negocio por permisos de modulo;
  - documentos por permisos documentales derivados del modulo;
  - seguridad solo con `USERS_ADMIN`.
- Si una superficie no esta permitida, no se incluye en la respuesta ni en la bandeja.

## Validacion local
Comandos ejecutados durante la implementacion:

```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter OperationsCenterTests
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter "AuthorizationRegressionTests|AuthorizationSurfaceGuardrailTests"
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build
npm run build
npm test -- --watch=false
```

Resultados:
- `dotnet restore`: proyectos actualizados.
- Backend build: exitoso, `0 warnings`, `0 errors`.
- `OperationsCenterTests`: `2/2` aprobados.
- Autorizacion/guardrails focales: `200/200` aprobados.
- Suite backend completa `FMCPA.Api.AuthorizationRegressionTests`: `200/200` aprobados.
- Frontend build: exitoso, sin warnings finales.
- Frontend tests: `22/22` aprobados.

## Fuera de alcance
- BI pesado.
- Exportaciones complejas.
- Graficas avanzadas.
- Scheduler.
- Notificaciones automaticas.
- Workflow con asignaciones, SLA u ownership.
- Nuevas reglas de negocio sobre los modulos fuente.
- Persistencia de metricas o snapshots analiticos.

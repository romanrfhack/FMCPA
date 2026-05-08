# Track 4 - Operations Time Window Implementation Note

## Que se implemento
- Se extendieron `GET /api/operations/summary` y `GET /api/operations/work-queue` para aceptar filtros temporales operativos.
- Se agrego metadata `timeWindow` a las respuestas para que el cliente sepa que ventana se aplico.
- Se agrego un selector simple en `/operations` con presets `Hoy`, `Ultimos 7 dias`, `Proximos 30 dias` y `Todo`.
- Se agrego `relevantUtc` a los items documentales usados por la composicion transversal para poder filtrar senales documentales con fecha clara.
- Se agrego cobertura de regresion que valida dos ventanas distintas y comprueba coherencia entre summary y work queue.

## Ventanas soportadas
- `ALL`: comportamiento global existente; es el default cuando no se envia filtro temporal.
- `TODAY`: desde el inicio hasta el final del dia UTC actual.
- `LAST_7_DAYS`: desde `UtcNow - 7 dias` hasta `UtcNow`.
- `NEXT_30_DAYS`: desde `UtcNow` hasta `UtcNow + 30 dias`.
- `CUSTOM`: se activa cuando se envian `fromUtc` y `toUtc`; el rango explicito tiene precedencia sobre `timeWindowCode`.

La API conserva `ALL` como default para compatibilidad. La UI inicia en `NEXT_30_DAYS` como horizonte operativo de priorizacion, con opcion visible para cambiar a `Hoy`, `Ultimos 7 dias` o `Todo`.

Reglas de validacion:
- Si se envia `fromUtc` o `toUtc`, deben enviarse ambos.
- `fromUtc` no puede ser posterior a `toUtc`.
- Un `timeWindowCode` desconocido devuelve `400`.

## Fecha usada por senal
- Negocio: usa `DashboardAlertItemResponse.RelevantDate` cuando la alerta lo expone.
- Documentos: usa fecha objetivo de retencion/revision (`NextRetentionReviewUtc` o retencion efectiva) para items de revision de retencion.
- Seguridad: usa `AuditEvent.OccurredUtc` para contar eventos recientes y `ApplicationUser.LockoutEndUtc` para usuarios bloqueados.
- Los items agregados de actividad de seguridad usan el inicio de la ventana (`SinceUtc`) como fecha operativa del item, porque resumen varios eventos en una sola fila.
- Senales sin fecha operativa clara quedan fuera cuando la ventana temporal esta activa.

## Impacto en summary y work queue
- Sin ventana (`ALL`), summary y work queue conservan el comportamiento global previo.
- Con ventana activa, la work queue solo incluye items con `RelevantUtc` dentro del rango.
- Con ventana activa, el summary se calcula sobre la misma ventana para evitar mezclar totales globales con una bandeja filtrada.
- Las metricas de inventario documental sin fecha operativa natural no se fuerzan dentro de la ventana; se mantienen fuera del conteo temporal.

## Validacion local
Comandos ejecutados para esta subetapa:

```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
npm run build
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter OperationsCenterTests
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter "AuthorizationRegressionTests|AuthorizationSurfaceGuardrailTests"
npm test -- --watch=false
git diff --check
```

Resultados:
- `dotnet restore`: exitoso, proyectos al dia.
- Backend build: exitoso, `0` warnings y `0` errores.
- Frontend build: exitoso.
- `OperationsCenterTests`: `3/3` exitosos.
- Suite focal de autorizacion/guardrails: `214/214` exitosos.
- Frontend tests: `22/22` exitosos.
- `git diff --check`: exitoso, sin errores de whitespace.

Validacion manual recomendada con backend local:

```bash
curl -s "http://127.0.0.1:5080/api/operations/summary?timeWindowCode=TODAY" -H "Authorization: Bearer ${ADMIN_TOKEN}"
curl -s "http://127.0.0.1:5080/api/operations/work-queue?timeWindowCode=LAST_7_DAYS&take=20" -H "Authorization: Bearer ${ADMIN_TOKEN}"
curl -s "http://127.0.0.1:5080/api/operations/work-queue?timeWindowCode=NEXT_30_DAYS&take=20" -H "Authorization: Bearer ${ADMIN_TOKEN}"
curl -s "http://127.0.0.1:5080/api/operations/summary?fromUtc=2026-05-01T00:00:00Z&toUtc=2026-05-07T23:59:59Z" -H "Authorization: Bearer ${ADMIN_TOKEN}"
```

## Fuera de alcance
- BI pesado o cubos historicos.
- Reporting complejo o exportaciones nuevas.
- Scheduler, snapshots, notificaciones o alertas automaticas.
- Workflow, asignaciones, SLA, aprobaciones o acciones masivas.
- Persistencia de metricas o migraciones nuevas.

# Hardening Track Legacy History Normalization Implementation Note

## Objetivo del subpaso
- Reducir la dependencia del historico respecto de `LEGACY_TIMESTAMP_FALLBACK` para registros cerrados o archivados anteriores al hardening.
- Mantener una diferencia explicita y honesta entre:
  - cierre formal real capturado operativamente
  - cierre retrospectivo normalizado a partir de metadata legado
  - fallback legado sin regularizar

## Que se implemento
- Se extendio `AuditEvent` para permitir fijar `OccurredUtc` explicitamente al crear eventos retrospectivos.
- Se introdujo el tipo de accion `LEGACY_CLOSE_NORMALIZED` como evento retrospectivo de cierre normalizado.
- Se ajusto la bitacora para exponer `closeEventSource` y distinguir visualmente:
  - `FORMAL_CLOSE_EVENT`
  - `LEGACY_CLOSE_NORMALIZED`
- Se ajusto la consulta de historico para priorizar:
  1. `FORMAL_CLOSE_EVENT`
  2. `LEGACY_CLOSE_NORMALIZED`
  3. `LEGACY_TIMESTAMP_FALLBACK`
- Se agrego el script `scripts/local/normalize-legacy-history.sh` para ejecutar la regularizacion de forma controlada y explicita.
- Se agrego soporte `--dry-run` en la regularizacion para revisar candidatos antes de persistir los eventos.
- La regularizacion cubre, al menos:
  - `Market`
  - `Donation`
  - `FinancialPermit`
  - `FederationAction`
  - `FederationDonation`

## Decisiones tomadas
- No se creo una tabla nueva ni una migracion adicional para esta regularizacion; se reutilizo `AuditEvent`.
- La normalizacion retrospectiva se marca con metadata explicita para no confundirse con un cierre real capturado en operacion.
- La fecha del evento retrospectivo se toma del mejor dato legado disponible:
  - `UpdatedUtc` si existe
  - `CreatedUtc` en caso contrario
- No se inventan motivos de cierre ni detalles historicos no conocidos.
- La regularizacion no corre automaticamente en el arranque de la API; solo se ejecuta por accion explicita.
- La ejecucion es idempotente en la practica:
  - si ya existe un cierre formal, se omite
  - si ya existe un cierre retrospectivo normalizado, se omite

## Diferencia entre cierre formal y cierre retrospectivo normalizado
- `FORMAL_CLOSE_EVENT`
  - se genera por una operacion explicita de cierre dentro del modulo correspondiente
  - representa el cierre operativo real capturado por el sistema
  - tiene prioridad maxima en el historico
- `LEGACY_CLOSE_NORMALIZED`
  - se genera retrospectivamente para registros del legado ya cerrados o archivados antes del hardening
  - no equivale a una evidencia exacta del momento real de cierre
  - usa el mejor timestamp legado disponible y metadata para dejar claro su origen
- `LEGACY_TIMESTAMP_FALLBACK`
  - permanece como ultimo recurso cuando no existe ni cierre formal ni regularizacion retrospectiva

## Como ejecutar la regularizacion localmente
### Prerequisitos
- SQL Server local levantado con la convencion del proyecto
- migraciones aplicadas
- backend corriendo

### Dry-run
```bash
FMCPA_SQL_PORT=14334 \
FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-local \
FMCPA_API_PORT=5090 \
./scripts/local/normalize-legacy-history.sh --dry-run
```

### Ejecucion real
```bash
FMCPA_SQL_PORT=14334 \
FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-local \
FMCPA_API_PORT=5090 \
./scripts/local/normalize-legacy-history.sh
```

### Resultado esperado
- El script imprime:
  - `scannedClosedCount`
  - `eligibleCount`
  - `normalizedCount`
  - `skippedCount`
- Cada item muestra:
  - modulo
  - tipo
  - referencia
  - fuente historica base
  - resultado (`NORMALIZED`, `SKIPPED_FORMAL_CLOSE_EXISTS`, `SKIPPED_ALREADY_NORMALIZED`)

## Validacion local ejecutada
- `dotnet restore src/backend/FMCPA.Backend.sln`
- `dotnet build src/backend/FMCPA.Backend.sln`
- `npm run build`
- `FMCPA_SQL_PORT=14334 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-local FMCPA_API_PORT=5090 ./scripts/local/reset-db.sh --force`
- `FMCPA_SQL_PORT=14334 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-local FMCPA_API_PORT=5090 ./scripts/local/dev-up.sh --no-frontend`
- Creacion controlada por API de:
  - registros cerrados heredados sin cierre formal
  - registros equivalentes con cierre formal real
- `FMCPA_SQL_PORT=14334 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-local FMCPA_API_PORT=5090 ./scripts/local/normalize-legacy-history.sh --dry-run`
- `FMCPA_SQL_PORT=14334 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-local FMCPA_API_PORT=5090 ./scripts/local/normalize-legacy-history.sh`
- Reejecucion del mismo comando para validar idempotencia
- Consulta de:
  - `/api/history/closed-items?q=NORM-T1`
  - `/api/bitacora?q=NORM-T1&take=20`

## Resultado de la validacion
- El dry-run encontro `7` registros cerrados controlados:
  - `5` elegibles para normalizacion retrospectiva
  - `2` ya cubiertos por cierre formal real
- La ejecucion real genero `5` eventos `LEGACY_CLOSE_NORMALIZED`.
- La reejecucion no duplico eventos:
  - los registros formales quedaron como `SKIPPED_FORMAL_CLOSE_EXISTS`
  - los ya normalizados quedaron como `SKIPPED_ALREADY_NORMALIZED`
- En el historico, los registros formales siguieron mostrando `FORMAL_CLOSE_EVENT`.
- Los registros heredados regularizados pasaron a `LEGACY_CLOSE_NORMALIZED`.
- Para los registros de prueba ya no quedo `LEGACY_TIMESTAMP_FALLBACK`.

## Que quedo fuera
- Backfill forense completo de todos los eventos historicos del MVP.
- Regularizacion automatica al arranque.
- Motivos de cierre retrospectivos mas ricos de los que el legado realmente conserva.
- Un proceso global de saneamiento historico masivo fuera del alcance de este subpaso.

# MVP Local Runbook

## Objetivo
- Dejar una guia minima, repetible y consistente para levantar el entorno local del MVP ya implementado.

## Convencion local estandar
- SQL Server local en Docker: `127.0.0.1:14333`
- Contenedor SQL Server: `fmcpa-sql`
- Base de datos de desarrollo: `FMCPA_Development`
- Backend local: `http://127.0.0.1:5080`
- Frontend local: `http://127.0.0.1:4200`
- Proxy local de Angular generado por `run-frontend.sh`: resuelve `/api` y `/health` hacia `FMCPA_API_PORT`
- Storage local esperado: `App_Data/` en la raiz del repositorio

## Prerrequisitos
- Docker con `docker compose`
- SDK de .NET 10
- Node.js y npm compatibles con Angular 21
- Puertos locales libres: `14333`, `5080`, `4200`

## Artefactos de soporte
- Compose local: `docker-compose.local.yml`
- Plantilla de variables locales: `.env.local.example`
- Tooling local .NET: `.config/dotnet-tools.json`
- Scripts operativos: `scripts/local/doctor.sh`, `scripts/local/up-sqlserver.sh`, `scripts/local/apply-migrations.sh`, `scripts/local/run-backend.sh`, `scripts/local/run-frontend.sh`, `scripts/local/smoke.sh`, `scripts/local/smoke-mvp.sh`, `scripts/local/reset-db.sh`, `scripts/local/dev-up.sh`, `scripts/local/dev-down.sh`

## Variables locales
- La convencion puede usarse sin overrides adicionales.
- Si se necesita ajustar puertos o password local, usar `.env.local` con el mismo formato de `.env.local.example`.
- `.env.local` esta pensado solo para overrides locales y no debe versionarse.
- Si cambia `FMCPA_API_PORT`, no hace falta editar `environment.development.ts`; `run-frontend.sh` genera el proxy local apuntando al puerto configurado.

## Arranque rapido recomendado

### 1. Ejecutar preflight local
```bash
./scripts/local/doctor.sh
```

### 2. Levantar SQL Server local
```bash
./scripts/local/up-sqlserver.sh
```

### 3. Aplicar migraciones sobre la base local
```bash
./scripts/local/apply-migrations.sh
```

### 4. Levantar backend
```bash
./scripts/local/run-backend.sh
```

### 5. Levantar frontend
```bash
./scripts/local/run-frontend.sh
```

### 6. Ejecutar smoke operativo minimo
```bash
./scripts/local/smoke.sh
```

### 7. Ejecutar smoke funcional minimo del MVP
```bash
./scripts/local/smoke-mvp.sh
```

## Flujo corto recomendado para iniciar sesion
```bash
./scripts/local/doctor.sh
./scripts/local/dev-up.sh
```

## Flujo corto recomendado para apagar sesion
```bash
./scripts/local/dev-down.sh
```

## Cuando usar `doctor.sh`
- Antes de empezar una sesion nueva.
- Cuando no quede claro si los puertos locales ya estan ocupados.
- Cuando se haya cambiado `.env.local`.
- Cuando falle el arranque de SQL Server, backend o frontend y se necesite una lectura rapida del entorno.

## Cuando usar `reset-db.sh`
- Cuando la base local de desarrollo quede en un estado inconsistente y se quiera volver a un estado limpio con migraciones reaplicadas.
- Solo sobre la base local configurada en `FMCPA_DB_NAME`.
- El script no corre sin confirmacion interactiva o `--force`.

Ejemplos:
```bash
./scripts/local/reset-db.sh
./scripts/local/reset-db.sh --force
./scripts/local/reset-db.sh --dry-run
```

## Cuando usar `dev-up.sh`
- Cuando se quiera levantar rapido el stack local gestionado por el proyecto.
- Por defecto, prepara SQL local, aplica migraciones y arranca backend y frontend en background.
- Si solo se quiere dejar lista la base y el backend, usar `--no-frontend`.
- Si se quiere solo ver el plan, usar `--dry-run`.

Ejemplos:
```bash
./scripts/local/dev-up.sh
./scripts/local/dev-up.sh --dry-run
FMCPA_SQL_PORT=14334 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-local FMCPA_API_PORT=5090 FMCPA_WEB_PORT=4201 ./scripts/local/dev-up.sh --no-frontend
```

## Cuando usar `dev-down.sh`
- Cuando se quiera bajar backend, frontend y SQL local gestionados por `dev-up.sh`.
- Solo actua sobre PID files y el contenedor configurado para la sesion actual.
- Si se quiere revisar el alcance sin ejecutar cambios, usar `--dry-run`.

Ejemplos:
```bash
./scripts/local/dev-down.sh
./scripts/local/dev-down.sh --dry-run
```

## Cuando usar `smoke.sh`
- Cuando solo se necesita una señal rapida de plataforma local.
- Cuando se quiere validar health, base configurada, dashboard summary, integridad documental y shell web.
- Cuando ya existe una instancia de frontend local corriendo y se desea una comprobacion ligera.

## Cuando usar `smoke-mvp.sh`
- Cuando se necesita una validacion funcional minima del MVP por API.
- Cuando se quiere verificar que Markets, Donations, Financials y Federation siguen cableados correctamente.
- Cuando se quiere validar cierres formales, bitacora, historico y comisiones consolidadas sin abrir una suite completa.
- Cuando se busca una corrida mas determinista y se puede preceder con `reset-db.sh --force`.

## Wiring local frontend/API
- En desarrollo, Angular usa `apiBaseUrl` relativo.
- `run-frontend.sh` genera un archivo de proxy local a partir de `FMCPA_API_PORT`.
- El flujo soportado para desarrollo local es levantar el frontend con `./scripts/local/run-frontend.sh`.
- Si el backend corre en un puerto override, por ejemplo `5090`, basta usar el mismo override al correr el backend y el frontend.

Ejemplo:
```bash
FMCPA_SQL_PORT=14334 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-local ./scripts/local/up-sqlserver.sh
FMCPA_SQL_PORT=14334 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-local ./scripts/local/apply-migrations.sh
FMCPA_SQL_PORT=14334 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-local FMCPA_API_PORT=5090 ./scripts/local/run-backend.sh
FMCPA_SQL_PORT=14334 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-local FMCPA_API_PORT=5090 FMCPA_WEB_PORT=4201 ./scripts/local/run-frontend.sh
curl -s http://127.0.0.1:4201/health
curl -s http://127.0.0.1:4201/api/dashboard/summary
```

## Tooling .NET local
- Las migraciones locales ya no dependen de un `dotnet-ef` global arbitrario.
- El repositorio versiona `.config/dotnet-tools.json` con `dotnet-ef` `10.0.6`.
- `apply-migrations.sh` restaura y usa ese tooling local automaticamente.

Comandos utiles:
```bash
dotnet tool restore
dotnet tool run dotnet-ef -- --version
```

## Smoke esperado
- La base local acepta conexion y responde `DB_NAME()`.
- El backend responde `http://127.0.0.1:5080/health`.
- El backend responde al menos:
  - `GET /api/dashboard/summary`
  - `GET /api/documents/integrity?take=5`
- El frontend responde en `http://127.0.0.1:4200/`.

## Smoke MVP esperado
- El backend responde a:
  - `GET /health`
  - `GET /api/dashboard/summary`
  - `GET /api/dashboard/alerts`
  - `GET /api/commissions/consolidated`
  - `GET /api/bitacora`
  - `GET /api/history/closed-items`
  - `GET /api/documents/integrity`
- El script crea datos tecnicos minimos con un prefijo unico por corrida.
- El script valida cierres formales e historico sobre esos registros.
- El script deja salida `OK` o `FAIL` por chequeo y devuelve codigo de salida util.
- El script no requiere frontend levantado para ejecutarse.

## Endpoints utiles para verificacion manual
```bash
curl -s http://127.0.0.1:5080/health
curl -s http://127.0.0.1:5080/api/dashboard/summary
curl -s http://127.0.0.1:5080/api/dashboard/alerts
curl -s http://127.0.0.1:5080/api/commissions/consolidated
curl -s http://127.0.0.1:5080/api/bitacora
curl -s http://127.0.0.1:5080/api/history/closed-items
curl -s http://127.0.0.1:5080/api/documents/integrity?take=5
curl -s http://127.0.0.1:4200/
./scripts/local/smoke-mvp.sh
```

## Troubleshooting minimo
- Si `doctor.sh` marca `WARNING` por puertos ocupados, revisar si ya existe una instancia previa del proyecto o usar overrides en `.env.local`.
- Si `docker compose` no puede levantar `fmcpa-sql`, revisar Docker y liberar el puerto `14333`.
- Si `run-backend.sh` falla por conexion, volver a correr `up-sqlserver.sh` y `apply-migrations.sh`.
- Si `run-frontend.sh` levanta pero no conecta, verificar que backend y frontend compartan el mismo `FMCPA_API_PORT`; el proxy se genera con ese valor.
- Si se necesita limpiar solo la base local del proyecto, usar `reset-db.sh` en vez de tocar contenedores o volumenes manualmente.
- Si ya existe un `App_Data/` previo, los scripts reutilizan esa ruta y crean subcarpetas faltantes sin limpiar contenido existente.
- Si `smoke-mvp.sh` falla creando registros tecnicos, revisar que las migraciones y seeds esten aplicadas; la forma mas rapida de volver a una base limpia es `./scripts/local/reset-db.sh --force`.
- Si `smoke-mvp.sh` falla por puertos ocupados, usar primero `doctor.sh` y despues overrides en `.env.local` o variables de entorno del proceso.
- Si se quiere una corrida de smoke MVP mas limpia y repetible, usar `reset-db.sh --force` antes de correrlo.
- Si se ejecuta `ng serve` manualmente sin `run-frontend.sh`, el frontend de desarrollo no tendra el proxy local y fallaran `/api` y `/health`.
- Si `dotnet tool restore` falla, revisar conectividad de paquetes antes de volver a correr `apply-migrations.sh`.
- `dev-up.sh` y `dev-down.sh` gestionan estado temporal bajo `FMCPA_LOCAL_STATE_DIR`; si una corrida previa queda interrumpida, usar `dev-down.sh` para limpiar PIDs obsoletos.

## Referencias
- [MVP Release Note](./mvp-release-note.md)
- [Validation Summary](./mvp-validation-summary.md)
- [Current Phase](../00-governance/current-phase.md)
- [Hardening Track Local Environment Implementation Note](../05-post-mvp/hardening-track-local-environment-implementation-note.md)
- [Hardening Track Doctor and Reset Implementation Note](../05-post-mvp/hardening-track-doctor-and-reset-implementation-note.md)
- [Hardening Track Smoke MVP Implementation Note](../05-post-mvp/hardening-track-smoke-mvp-implementation-note.md)
- [Hardening Track Frontend Local Wiring Implementation Note](../05-post-mvp/hardening-track-frontend-local-wiring-implementation-note.md)
- [Hardening Track Tooling and Ergonomics Implementation Note](../05-post-mvp/hardening-track-tooling-and-ergonomics-implementation-note.md)

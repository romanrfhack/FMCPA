# Hardening Track Tooling and Ergonomics Implementation Note

## Objetivo
- Reducir friccion operativa en sesiones locales de desarrollo y validacion sin tocar produccion, CI/CD ni modulos de negocio.

## Que se implemento
- Manifiesto local `.config/dotnet-tools.json` con `dotnet-ef` `10.0.6`.
- `apply-migrations.sh` ahora restaura y usa `dotnet-ef` local versionado en el repositorio.
- `common.sh` ahora expone mejor la configuracion efectiva, directorio de estado local y helpers comunes para tooling, health checks y procesos gestionados.
- `dev-up.sh` para preparar SQL local, aplicar migraciones y arrancar backend y frontend en background.
- `dev-down.sh` para detener backend, frontend y SQL local gestionados por el proyecto.
- `reset-db.sh` ahora soporta `--dry-run`.

## Decisiones tomadas
- El warning de `dotnet-ef` vs runtime se resolvio versionando el tooling local en repo.
- Los scripts siguen siendo shell simple; no se introdujo un orquestador complejo.
- `dev-up.sh` arranca backend y frontend en background por defecto para acortar la apertura de sesion.
- El estado temporal del stack gestionado se guarda en `FMCPA_LOCAL_STATE_DIR`, aislado por configuracion efectiva.

## Como usarlo

### Flujo recomendado
```bash
./scripts/local/doctor.sh
./scripts/local/dev-up.sh
./scripts/local/dev-down.sh
```

### Vista previa del flujo
```bash
./scripts/local/dev-up.sh --dry-run
./scripts/local/reset-db.sh --dry-run
./scripts/local/dev-down.sh --dry-run
```

### Flujo con overrides
```bash
FMCPA_SQL_PORT=14334 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-local FMCPA_API_PORT=5090 FMCPA_WEB_PORT=4201 ./scripts/local/dev-up.sh --no-frontend
FMCPA_SQL_PORT=14334 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-local FMCPA_API_PORT=5090 FMCPA_WEB_PORT=4201 ./scripts/local/dev-down.sh
```

## Que quedo fuera
- Produccion.
- CI/CD.
- Cualquier cambio funcional del MVP.
- Orquestacion compleja o manejo de procesos ajenos al proyecto.

# Hardening Track Doctor and Reset Implementation Note

## Objetivo
- Agregar un preflight simple para saber si el entorno local esta listo y un reset controlado para recuperar la base local de desarrollo sin tocar contenedores o bases ajenas.

## Que se implemento
- `scripts/local/doctor.sh`
  - valida `docker`, `docker compose`, `dotnet` y `npm`
  - revisa `.env.local` o fallback a defaults
  - revisa puertos configurados para SQL, API y frontend
  - informa si el contenedor SQL esperado existe, esta corriendo o puede crearse
  - verifica que las rutas de storage local existan o puedan crearse
  - entrega salida `OK`, `WARNING` y `ERROR` con resumen final
- `scripts/local/reset-db.sh`
  - opera solo sobre `FMCPA_DB_NAME` y `FMCPA_SQL_CONTAINER_NAME`
  - no corre sin confirmacion interactiva o `--force`
  - elimina y recrea solo la base local configurada
  - limpia archivos `mdf` y `ldf` huerfanos del mismo nombre si quedaron remanentes dentro del contenedor local
  - reaplica migraciones reutilizando `apply-migrations.sh`
- `scripts/local/common.sh`
  - incorpora helpers reutilizables para estado de contenedor, chequeo de puertos, ejecucion de `sqlcmd` y validacion segura del nombre de base

## Decisiones tomadas
- El reset no destruye contenedores ni volumenes por defecto; destruye solo la base configurada y la vuelve a dejar lista via migraciones.
- El preflight es operativo y accionable, no una garantia completa de salud end-to-end.
- El reset automatizado acepta solo nombres de base seguros para evitar inyeccion accidental en la sentencia SQL.
- La logica comun se centraliza en `common.sh` para no duplicar wiring local.

## Como usarlo
```bash
./scripts/local/doctor.sh
./scripts/local/reset-db.sh
./scripts/local/reset-db.sh --force
```

## Cuando usarlo
- `doctor.sh`: antes de iniciar una sesion, despues de cambiar `.env.local` o cuando el entorno local este dudoso.
- `reset-db.sh`: cuando la base local quede inconsistente y se quiera volver a un estado limpio de desarrollo.

## Advertencias
- `reset-db.sh` es destructivo para la base configurada en `FMCPA_DB_NAME`.
- Si `.env.local` apunta a otro contenedor o base, el alcance del reset sigue esa configuracion local.
- El script no esta pensado para produccion ni para CI/CD.

## Que quedo fuera
- Reset de volumenes Docker.
- Limpieza de storage documental.
- Recuperacion automatica de seeds de prueba.
- Monitoreo continuo del entorno local.

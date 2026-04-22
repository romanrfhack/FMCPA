# Hardening Track Frontend Local Wiring Implementation Note

## Objetivo
- Estabilizar el wiring local entre Angular y la API para que el frontend de desarrollo siga al backend configurado sin editar codigo fuente cuando cambie `FMCPA_API_PORT`.

## Que se implemento
- `environment.development.ts` ahora usa `apiBaseUrl` relativo.
- `scripts/local/common.sh` genera un proxy local de Angular con `write_frontend_proxy_config`.
- `scripts/local/run-frontend.sh` usa ese proxy automaticamente al levantar `ng serve`.
- `scripts/local/doctor.sh` ahora valida la capacidad de generar el proxy local y deja de tratar el override del API como una limitacion estructural.

## Decisiones tomadas
- La solucion se limito a desarrollo local.
- El build de produccion no se cambia; `environment.ts` permanece intacto.
- No se refactorizaron servicios HTTP; se reaprovecharon tal como estan usando `environment.apiBaseUrl`.
- El flujo soportado para desarrollo local queda centralizado en `run-frontend.sh`.

## Como usarlo

### Caso estandar
```bash
./scripts/local/run-backend.sh
./scripts/local/run-frontend.sh
```

### Caso con backend en puerto override
```bash
FMCPA_API_PORT=5090 ./scripts/local/run-backend.sh
FMCPA_API_PORT=5090 FMCPA_WEB_PORT=4201 ./scripts/local/run-frontend.sh
curl -s http://127.0.0.1:4201/health
curl -s http://127.0.0.1:4201/api/dashboard/summary
```

## Que valida esta mejora
- El frontend de desarrollo puede levantar con `ng serve` usando el backend configurado localmente.
- No hace falta editar `environment.development.ts` cada vez que cambia el puerto del backend.
- El wiring local queda integrado a la convencion ya existente de `scripts/local/*`.

## Que no cambia
- No cambia el comportamiento de produccion.
- No agrega runtime config global.
- No modifica logica de negocio.
- No agrega frameworks ni tooling externo adicional.

## Que quedo fuera
- Soporte a `ng serve` manual sin proxy.
- Runtime config transversal para produccion.
- Cualquier reestructuracion mas profunda del frontend o de los servicios HTTP.

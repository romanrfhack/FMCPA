# Hardening Track Local Environment Implementation Note

## Objetivo
- Reducir la friccion operativa de futuras sesiones dejando una convencion local estable para base, backend, frontend y smoke checks.

## Que se implemento
- `docker-compose.local.yml` con un unico servicio `sqlserver` para SQL Server local en Docker.
- `.env.local.example` como plantilla de overrides locales no versionados.
- Scripts `scripts/local/*` para:
  - levantar SQL Server local
  - aplicar migraciones
  - levantar backend
  - levantar frontend
  - ejecutar smoke operativo minimo
- Convencion local explicita:
  - SQL Server Docker en `127.0.0.1:14333`
  - base `FMCPA_Development`
  - backend en `127.0.0.1:5080`
  - frontend en `127.0.0.1:4200`
  - storage local bajo `App_Data/`
- Refuerzo del runbook local del MVP para que esta convencion quede documentada en un solo punto de partida.

## Decisiones tomadas
- Se reutilizaron los puertos y la base ya usados en validaciones previas para evitar retrabajo.
- La configuracion local nueva vive como compose, variables de entorno y scripts, sin tocar despliegue productivo ni CI/CD.
- El backend recibe cadena de conexion y rutas de storage por variables de entorno exportadas por `scripts/local/common.sh`.
- El frontend conserva `5080` como API base por defecto; si se cambia ese puerto, el ajuste se considera override local y debe hacerse explicitamente.

## Como usarlo
```bash
./scripts/local/up-sqlserver.sh
./scripts/local/apply-migrations.sh
./scripts/local/run-backend.sh
./scripts/local/run-frontend.sh
./scripts/local/smoke.sh
```

## Validacion local esperada
- SQL Server local responde en `127.0.0.1:14333`.
- Las migraciones aplican sobre `FMCPA_Development`.
- El backend responde `GET /health`.
- El frontend responde en `http://127.0.0.1:4200/`.
- El smoke consulta `dashboard/summary` e integridad documental minima.

## Que quedo fuera
- Produccion y despliegue real.
- CI/CD.
- Autenticacion y autorizacion.
- Storage externo.
- Politica documental transversal completa.
- Reset destructivo de base local o automatizacion de limpieza avanzada.

## Convivencia con el entorno actual
- La convencion nueva no invalida el wiring existente del MVP; lo encapsula en scripts y documentacion.
- Los procesos o puertos locales previos aun pueden coexistir; por eso la convencion documenta puertos fijos y permite override en `.env.local`.
- `App_Data/` sigue siendo el storage local actual; este paso solo normaliza su ubicacion y la forma de exportarla al backend.

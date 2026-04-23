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
- Para autenticacion y roles locales minimos:
  - `FMCPA_AUTH_BOOTSTRAP_USER_NAME` controla el usuario bootstrap local; por defecto `admin`
  - `FMCPA_AUTH_BOOTSTRAP_DISPLAY_NAME` controla el nombre visible del bootstrap local
  - `FMCPA_AUTH_BOOTSTRAP_PASSWORD` es obligatoria para crear el usuario bootstrap local y ejecutar los smoke autenticados
  - `FMCPA_AUTH_OPERATOR_USER_NAME` y `FMCPA_AUTH_OPERATOR_DISPLAY_NAME` controlan el usuario local `OPERATOR`
  - `FMCPA_AUTH_OPERATOR_PASSWORD` aprovisiona opcionalmente el usuario local `OPERATOR`
  - `FMCPA_AUTH_READONLY_USER_NAME` y `FMCPA_AUTH_READONLY_DISPLAY_NAME` controlan el usuario local `READONLY`
  - `FMCPA_AUTH_READONLY_PASSWORD` aprovisiona opcionalmente el usuario local `READONLY`
  - `FMCPA_AUTH_JWT_SIGNING_KEY` es opcional en `Development`; si se omite, el backend usa una llave efimera por arranque

## Autenticacion y roles locales minimos
- La app exige autenticacion para `/api` salvo `/api/auth/login`, `health` y la raiz del servicio.
- El usuario bootstrap principal `admin` queda como `ADMIN`.
- Desde esta subetapa, `ADMIN` ya puede crear usuarios internos desde `/admin/users` o `/api/admin/users`; los usuarios bootstrap `operator` y `readonly` quedan como una conveniencia opcional, no como requisito para validar roles gestionados.
- Opcionalmente se pueden aprovisionar:
  - `operator` como `OPERATOR`
  - `readonly` como `READONLY`
- No se versionan contrasenas reales en el repo; las passwords locales deben definirse en variables de entorno o `.env.local`.
- Politicas actuales:
  - lectura: `READONLY`, `OPERATOR`, `ADMIN`
  - escritura funcional: `OPERATOR`, `ADMIN`
  - administracion/cierre formal: `ADMIN`
- Cambios de rol, activacion/desactivacion y reset administrativo de password invalidan tokens previos del usuario afectado; ese usuario debe iniciar sesion de nuevo.

Ejemplo de `.env.local` minimo para auth y roles:
```bash
FMCPA_AUTH_BOOTSTRAP_USER_NAME=admin
FMCPA_AUTH_BOOTSTRAP_DISPLAY_NAME=Administrador local
FMCPA_AUTH_BOOTSTRAP_PASSWORD=LocalAdmin123!Aa
FMCPA_AUTH_OPERATOR_USER_NAME=operator
FMCPA_AUTH_OPERATOR_DISPLAY_NAME=Operador local
FMCPA_AUTH_OPERATOR_PASSWORD=LocalOperator123!Aa
FMCPA_AUTH_READONLY_USER_NAME=readonly
FMCPA_AUTH_READONLY_DISPLAY_NAME=Consulta local
FMCPA_AUTH_READONLY_PASSWORD=LocalReadonly123!Aa
```

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

Despues, iniciar sesion en `http://127.0.0.1:4200/login` con el usuario configurado localmente.

Si el login es como `ADMIN`, la pantalla minima de administracion de usuarios queda disponible en `http://127.0.0.1:4200/admin/users`.

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
- Cuando se quiere validar health, base configurada, login local, rechazo `401` sin token, dashboard summary autenticado, integridad documental y shell web.
- Si existe `FMCPA_AUTH_READONLY_PASSWORD`, el smoke tambien valida un `403` real de `READONLY` contra una escritura funcional.
- Cuando ya existe una instancia de frontend local corriendo y se desea una comprobacion ligera.
- No valida por si solo la gestion minima de usuarios; esa verificacion sigue siendo manual/API en esta etapa.

## Cuando usar `smoke-mvp.sh`
- Cuando se necesita una validacion funcional minima del MVP por API.
- Cuando se quiere verificar que Markets, Donations, Financials y Federation siguen cableados correctamente.
- Cuando se quiere validar login, `401` sin token, cierres formales, bitacora, historico y comisiones consolidadas sin abrir una suite completa.
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
- `GET /api/dashboard/summary` sin token devuelve `401`.
- El login local responde en `POST /api/auth/login`.
- El backend autenticado responde al menos:
  - `GET /api/auth/session`
  - `GET /api/dashboard/summary`
  - `GET /api/documents/integrity?take=5`
- Si se usa la gestion minima de usuarios:
  - `GET /api/admin/users` responde para `ADMIN`
  - `OPERATOR` y `READONLY` reciben `403` en `/api/admin/users`
- Si existe un usuario `READONLY` local configurado:
  - `GET /api/auth/session` devuelve `roleCode = READONLY`
  - `POST /api/contacts` devuelve `403`
- El frontend responde en `http://127.0.0.1:4200/`.

## Smoke MVP esperado
- El backend responde a:
  - `GET /health`
  - `POST /api/auth/login`
  - `GET /api/auth/session`
  - `GET /api/dashboard/summary` autenticado
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
curl -i http://127.0.0.1:5080/api/dashboard/summary
curl -s http://127.0.0.1:5080/api/auth/login -H 'Content-Type: application/json' -d '{"userName":"admin","password":"<password-local>"}'
TOKEN="<token-devuelto-por-login>"
curl -s http://127.0.0.1:5080/api/auth/session -H "Authorization: Bearer ${TOKEN}"
curl -s http://127.0.0.1:5080/api/dashboard/summary -H "Authorization: Bearer ${TOKEN}"
curl -s http://127.0.0.1:5080/api/dashboard/alerts -H "Authorization: Bearer ${TOKEN}"
curl -s http://127.0.0.1:5080/api/commissions/consolidated -H "Authorization: Bearer ${TOKEN}"
curl -s http://127.0.0.1:5080/api/bitacora -H "Authorization: Bearer ${TOKEN}"
curl -s http://127.0.0.1:5080/api/history/closed-items -H "Authorization: Bearer ${TOKEN}"
curl -s http://127.0.0.1:5080/api/documents/integrity?take=5 -H "Authorization: Bearer ${TOKEN}"
curl -s http://127.0.0.1:5080/api/admin/users -H "Authorization: Bearer ${TOKEN}"

# Crear usuario interno desde ADMIN
curl -s http://127.0.0.1:5080/api/admin/users -H "Authorization: Bearer ${TOKEN}" -H 'Content-Type: application/json' -d '{"userName":"operator2","displayName":"Operador interno","roleCode":"OPERATOR","password":"TempPassword123!"}'

# Cambiar rol, activar/desactivar y resetear password
USER_ID="<id-devuelto-por-la-alta>"
curl -s http://127.0.0.1:5080/api/admin/users/${USER_ID}/role -X PATCH -H "Authorization: Bearer ${TOKEN}" -H 'Content-Type: application/json' -d '{"roleCode":"READONLY"}'
curl -s http://127.0.0.1:5080/api/admin/users/${USER_ID}/activation -X PATCH -H "Authorization: Bearer ${TOKEN}" -H 'Content-Type: application/json' -d '{"isActive":false}'
curl -s http://127.0.0.1:5080/api/admin/users/${USER_ID}/activation -X PATCH -H "Authorization: Bearer ${TOKEN}" -H 'Content-Type: application/json' -d '{"isActive":true}'
curl -s http://127.0.0.1:5080/api/admin/users/${USER_ID}/reset-password -H "Authorization: Bearer ${TOKEN}" -H 'Content-Type: application/json' -d '{"newPassword":"TempPassword456!"}'

# OPERATOR puede escribir operaciones funcionales normales
curl -s http://127.0.0.1:5080/api/auth/login -H 'Content-Type: application/json' -d '{"userName":"operator","password":"<password-operator-local>"}'
OPERATOR_TOKEN="<token-operator>"
curl -i http://127.0.0.1:5080/api/contacts -H "Authorization: Bearer ${OPERATOR_TOKEN}" -H 'Content-Type: application/json' -d '{"name":"Probe","contactTypeId":1}'
curl -i http://127.0.0.1:5080/api/commission-types -H "Authorization: Bearer ${OPERATOR_TOKEN}" -H 'Content-Type: application/json' -d '{"code":"PROBE","name":"Probe","sortOrder":900}'
curl -i http://127.0.0.1:5080/api/admin/users -H "Authorization: Bearer ${OPERATOR_TOKEN}"

# READONLY solo consulta
curl -s http://127.0.0.1:5080/api/auth/login -H 'Content-Type: application/json' -d '{"userName":"readonly","password":"<password-readonly-local>"}'
READONLY_TOKEN="<token-readonly>"
curl -i http://127.0.0.1:5080/api/contacts -H "Authorization: Bearer ${READONLY_TOKEN}" -H 'Content-Type: application/json' -d '{"name":"Probe","contactTypeId":1}'
curl -i http://127.0.0.1:5080/api/admin/users -H "Authorization: Bearer ${READONLY_TOKEN}"

curl -s http://127.0.0.1:4200/
curl -s http://127.0.0.1:4200/admin/users
./scripts/local/smoke-mvp.sh
```

## Troubleshooting minimo
- Si `doctor.sh` marca `WARNING` por puertos ocupados, revisar si ya existe una instancia previa del proyecto o usar overrides en `.env.local`.
- Si `docker compose` no puede levantar `fmcpa-sql`, revisar Docker y liberar el puerto `14333`.
- Si `run-backend.sh` falla por conexion, volver a correr `up-sqlserver.sh` y `apply-migrations.sh`.
- Si `run-frontend.sh` levanta pero no conecta, verificar que backend y frontend compartan el mismo `FMCPA_API_PORT`; el proxy se genera con ese valor.
- Si `run-backend.sh` levanta pero el login falla con `401`, verificar que `FMCPA_AUTH_BOOTSTRAP_PASSWORD` exista en el entorno local y que el usuario bootstrap se haya aprovisionado en `Development`.
- Si el login de `operator` o `readonly` falla con `401`, verificar que `FMCPA_AUTH_OPERATOR_PASSWORD` o `FMCPA_AUTH_READONLY_PASSWORD` existan en el entorno local y repetir `dev-up.sh` para que el backend sincronice esos usuarios.
- Si un usuario gestionado cambia de rol, se desactiva o se le resetea el password, cualquier token previo deja de servir; volver a iniciar sesion con el rol/password vigentes.
- Si se necesita limpiar solo la base local del proyecto, usar `reset-db.sh` en vez de tocar contenedores o volumenes manualmente.
- Si ya existe un `App_Data/` previo, los scripts reutilizan esa ruta y crean subcarpetas faltantes sin limpiar contenido existente.
- Si `smoke-mvp.sh` falla creando registros tecnicos, revisar que las migraciones y seeds esten aplicadas; la forma mas rapida de volver a una base limpia es `./scripts/local/reset-db.sh --force`.
- Si `smoke-mvp.sh` falla por puertos ocupados, usar primero `doctor.sh` y despues overrides en `.env.local` o variables de entorno del proceso.
- Si se quiere una corrida de smoke MVP mas limpia y repetible, usar `reset-db.sh --force` antes de correrlo.
- Si se ejecuta `ng serve` manualmente sin `run-frontend.sh`, el frontend de desarrollo no tendra el proxy local y fallaran `/api` y `/health`.
- Si `dotnet tool restore` falla, revisar conectividad de paquetes antes de volver a correr `apply-migrations.sh`.
- `dev-up.sh` y `dev-down.sh` gestionan estado temporal bajo `FMCPA_LOCAL_STATE_DIR`; si una corrida previa queda interrumpida, usar `dev-down.sh` para limpiar PIDs obsoletos.
- Si `smoke.sh` o `smoke-mvp.sh` fallan de inmediato por autenticacion, revisar primero `FMCPA_AUTH_BOOTSTRAP_PASSWORD` y despues repetir `dev-up.sh` para que el backend sincronice el usuario bootstrap local.

## Referencias
- [MVP Release Note](./mvp-release-note.md)
- [Validation Summary](./mvp-validation-summary.md)
- [Current Phase](../00-governance/current-phase.md)
- [Hardening Track Local Environment Implementation Note](../05-post-mvp/hardening-track-local-environment-implementation-note.md)
- [Hardening Track Doctor and Reset Implementation Note](../05-post-mvp/hardening-track-doctor-and-reset-implementation-note.md)
- [Hardening Track Smoke MVP Implementation Note](../05-post-mvp/hardening-track-smoke-mvp-implementation-note.md)
- [Hardening Track Frontend Local Wiring Implementation Note](../05-post-mvp/hardening-track-frontend-local-wiring-implementation-note.md)
- [Hardening Track Tooling and Ergonomics Implementation Note](../05-post-mvp/hardening-track-tooling-and-ergonomics-implementation-note.md)
- [Security Track](../05-post-mvp/security-track.md)
- [Security Track Auth Foundation Implementation Note](../05-post-mvp/security-track-auth-foundation-implementation-note.md)
- [Security Track Role Authorization Implementation Note](../05-post-mvp/security-track-role-authorization-implementation-note.md)
- [Security Track User Management Implementation Note](../05-post-mvp/security-track-user-management-implementation-note.md)

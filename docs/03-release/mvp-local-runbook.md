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
- Para CORS local directo contra la API, `Cors:AllowedOrigins` acepta por defecto `http://localhost:4200` y `http://127.0.0.1:4200` en `Development`.
- El flujo local recomendado con `run-frontend.sh` usa proxy Angular para `/api` y `/health`; si se usa ese proxy, el navegador llama al mismo origin del frontend y CORS no requiere cambios por puerto override.
- `run-backend.sh` exporta `Cors__AllowedOrigins__0/1` con el `FMCPA_WEB_PORT` efectivo si esos valores no fueron definidos manualmente, para mantener compatible el proxy local y la proteccion de origen web.
- Fuera de `Development`, se deben configurar origins reales mediante `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, etc.; wildcard y localhost se rechazan.
- Fuera de `Development`, `Auth__Jwt__SigningKey` debe existir y tener al menos 32 bytes, y `Auth__Jwt__Issuer` / `Auth__Jwt__Audience` deben ser especificos del entorno.
- Las mutaciones `/api` desde browser usan una proteccion minima de origen: Angular envia `X-FMCPA-Client: FMCPA-Web` y cualquier `Origin`/`Referer` presente debe coincidir con `Cors:AllowedOrigins`.

## Autenticacion y roles locales minimos
- La app exige autenticacion para `/api` salvo `/api/auth/login`, `health` y la raiz del servicio.
- El usuario bootstrap principal `admin` queda como `ADMIN`.
- Desde esta subetapa, `ADMIN` ya puede crear usuarios internos desde `/admin/users` o `/api/admin/users`; los usuarios bootstrap `operator` y `readonly` quedan como una conveniencia opcional, no como requisito para validar roles gestionados.
- Opcionalmente se pueden aprovisionar:
  - `operator` como `OPERATOR`
  - `readonly` como `READONLY`
- No se versionan contrasenas reales en el repo; las passwords locales deben definirse en variables de entorno o `.env.local`.
- Las passwords locales de bootstrap, alta admin y reset admin deben cumplir la politica minima: `12` caracteres, mayuscula, minuscula y numero; no se aceptan espacios al inicio/fin ni passwords obvios.
- Permisos actuales derivados por rol:
  - `READONLY`: `DASHBOARD_READ`, `HISTORY_READ`, `CONTACTS_READ`, `MARKETS_READ`, `DONATIONS_READ`, `FINANCIALS_READ`, `FEDERATION_READ`, `CATALOGS_READ`
  - `OPERATOR`: permisos de `READONLY` mas `CONTACTS_WRITE`, `MARKETS_WRITE`, `DONATIONS_WRITE`, `FINANCIALS_WRITE`, `FEDERATION_WRITE`
  - `ADMIN`: permisos de `OPERATOR` mas `CATALOGS_ADMIN`, `USERS_ADMIN`, `FORMAL_CLOSE_ADMIN`
- Los cierres formales requieren `FORMAL_CLOSE_ADMIN` junto con el permiso `*_WRITE` del modulo.
- Cambios de rol, activacion/desactivacion y reset administrativo de password invalidan tokens previos del usuario afectado; ese usuario debe iniciar sesion de nuevo.
- El usuario autenticado puede cambiar su propia password en `/account/password` o `POST /api/auth/change-password`; el cambio exitoso invalida el token actual y exige iniciar sesion de nuevo.
- Cambios de matriz de permisos tambien pueden exigir relogin, porque backend valida que los claims `fmcpa_permission` coincidan con el rol vigente.
- La verificacion manual mas corta es reutilizar un token previo contra `GET /api/auth/session` despues del cambio sensible y confirmar `401 Unauthorized`.
- `POST /api/auth/login` tiene rate limit minimo de `10` solicitudes por minuto por cliente; al excederlo responde `429 Too Many Requests` con `Retry-After`.
- `/api/admin/users` tiene rate limit minimo de `60` solicitudes por minuto por cliente/usuario autenticado.
- Cinco fallos consecutivos de login para un usuario bloquean temporalmente la cuenta durante `15` minutos; durante lockout el login responde `423 Locked` con `Retry-After`.
- `ADMIN` puede ver `AccessFailedCount` y `LockoutEndUtc` en `/admin/users` y limpiar el lockout con `POST /api/admin/users/{id}/unlock`.
- `ADMIN` puede consultar operacion minima de seguridad en `/admin/security` o `/api/admin/security/*`: resumen, eventos SECURITY y usuarios con lockout activo.
- La consulta documental transversal esta disponible en `/documents` o `/api/documents` para usuarios autenticados con permisos de lectura de Mercados, Donatarias o Federacion.
- El catalogo documental muestra contexto origen de Mercados, Donatarias y Federacion; las pantallas de negocio incluyen una seccion minima de documentos relacionados.
- El catalogo documental expone completitud minima y pendientes documentales para locatarios y aplicaciones con evidencia, sin interpretar eso como cumplimiento legal avanzado.
- Las respuestas `/api` agregan `Cache-Control: no-store`, `Pragma: no-cache`, `Expires: 0` y headers de seguridad basicos.
- Si se prueban mutaciones con `curl` agregando un header browser como `Origin`, se debe agregar tambien `X-FMCPA-Client: FMCPA-Web`; los scripts no-browser sin `Origin`, `Referer` ni `Sec-Fetch-*` siguen funcionando sin ese header.

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

## Hardening HTTP local
- Validar login normal:
```bash
curl -i http://127.0.0.1:5080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"userName":"admin","password":"<password-local>"}'
```
- Validar headers de sesion:
```bash
curl -i http://127.0.0.1:5080/api/auth/session \
  -H "Authorization: Bearer ${TOKEN}"
```
- Validar CORS local directo:
```bash
curl -i -X OPTIONS http://127.0.0.1:5080/api/auth/login \
  -H 'Origin: http://localhost:4200' \
  -H 'Access-Control-Request-Method: POST' \
  -H 'Access-Control-Request-Headers: content-type'
```
- Validar rate limit de login: repetir mas de `10` `POST /api/auth/login` en menos de un minuto desde el mismo cliente y confirmar `429` con `Retry-After`.
- Validar lockout de cuenta: repetir `5` logins fallidos para un usuario existente y confirmar `423 Locked`; luego limpiar con `/api/admin/users/{id}/unlock` o esperar el cooldown.
- Validar observabilidad ADMIN:
```bash
curl -s http://127.0.0.1:5080/api/admin/security/summary \
  -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/admin/security/events?eventType=AUTH_LOGIN_FAILED&take=20" \
  -H "Authorization: Bearer ${TOKEN}"
curl -s http://127.0.0.1:5080/api/admin/security/locked-users \
  -H "Authorization: Bearer ${TOKEN}"
```
- Validar proteccion de origen web:
```bash
curl -i http://127.0.0.1:5080/api/auth/login \
  -H 'Origin: http://127.0.0.1:4200' \
  -H 'X-FMCPA-Client: FMCPA-Web' \
  -H 'Content-Type: application/json' \
  -d '{"userName":"admin","password":"<password-local>"}'

curl -i http://127.0.0.1:5080/api/auth/login \
  -H 'Origin: http://127.0.0.1:4200' \
  -H 'Content-Type: application/json' \
  -d '{"userName":"admin","password":"<password-local>"}'
# Debe responder 400 por faltar la senal del cliente web.

curl -i http://127.0.0.1:5080/api/auth/login \
  -H 'Origin: https://unexpected.example.test' \
  -H 'X-FMCPA-Client: FMCPA-Web' \
  -H 'Content-Type: application/json' \
  -d '{"userName":"admin","password":"<password-local>"}'
# Debe responder 403 por origen no permitido.
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

## Validacion documental manual
- Los uploads documentales actuales aceptan solo PDF/JPEG/PNG hasta `10 MB`.
- Se rechazan archivos vacios, extensiones no permitidas, content-types no permitidos y contenido cuya firma basica no coincide con la extension.
- Las descargas documentales pasan por permisos de lectura de modulo:
  - cédula de locatario: `MARKETS_READ`
  - evidencia de Donatarias: `DONATIONS_READ`
  - evidencia de Federacion: `FEDERATION_READ`
- La respuesta de descarga debe incluir `X-Content-Type-Options: nosniff`, `Cache-Control: no-store` y `Content-Disposition: attachment`.
- La superficie transversal permite consultar:
  - `GET /api/documents?moduleCode=MARKETS&take=20`
  - `GET /api/documents?documentAreaCode=DONATIONS_APPLICATION_EVIDENCES&integrityState=VALID`
  - `GET /api/documents?documentClassCode=CERTIFICATE&take=20`
  - `GET /api/documents?documentClassCode=SUPPORTING_DOCUMENT&take=20`
  - `GET /api/documents?retentionPolicyCode=EVIDENCE_MEDIUM_TERM&take=20`
  - `GET /api/documents?retentionStatusCode=REVIEW_DUE&take=20`
  - `GET /api/documents?retentionStatusCode=EXPIRED_RETENTION&take=20`
  - `GET /api/documents?documentOperationalStatusCode=ON_HOLD&includeArchived=true&take=20`
  - `GET /api/documents?documentOperationalStatusCode=INTEGRITY_ISSUE&includeArchived=true&take=20`
  - `GET /api/documents?statusCode=ACTIVE&take=20`
  - `GET /api/documents?statusCode=ARCHIVED&take=20`
  - `GET /api/documents?includeArchived=true&take=20`
  - `GET /api/documents/{documentId}`
  - `GET /api/documents/{documentId}/download`
- El catalogo transversal incluye `originContext` con modulo, entidad origen, nombre/resumen y `routeHint` cuando puede resolverse de forma acotada.
- La consulta por entidad permite validar documentos relacionados:
  - `GET /api/documents/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId={tenantId}`
  - `GET /api/documents/by-entity?moduleCode=DONATARIAS&entityType=DONATION_APPLICATION&entityId={applicationId}`
  - `GET /api/documents/by-entity?moduleCode=FEDERATION&entityType=FEDERATION_DONATION_APPLICATION&entityId={applicationId}`
- La completitud documental minima se valida con:
  - `GET /api/documents/rules`
  - `GET /api/documents/requirements/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId={tenantId}`
  - `GET /api/documents/requirements/by-entity?moduleCode=DONATARIAS&entityType=DONATION_APPLICATION&entityId={applicationId}`
  - `GET /api/documents/requirements/by-entity?moduleCode=FEDERATION&entityType=FEDERATION_DONATION_APPLICATION&entityId={applicationId}`
  - `GET /api/documents/completeness/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId={tenantId}`
  - `GET /api/documents/completeness/by-entity?moduleCode=DONATARIAS&entityType=DONATION_APPLICATION&entityId={applicationId}`
  - `GET /api/documents/completeness/by-entity?moduleCode=FEDERATION&entityType=FEDERATION_DONATION_APPLICATION&entityId={applicationId}`
  - `GET /api/documents/pending?take=20`
- Las respuestas de requisitos, completitud y pendientes incluyen `ruleCode`, `requiredDocumentClassCodes`, `minimumRequiredCount`; requisitos agrega tambien `currentDocumentCount` y `remediationHint`.
- La bandeja documental unificada se valida con:
  - `GET /api/documents/work-queue?take=20`
  - `GET /api/documents/work-queue?moduleCode=MARKETS&take=20`
  - `GET /api/documents/work-queue?workItemType=DOCUMENT_INTEGRITY_ISSUE&take=20`
  - `GET /api/documents/work-queue?severityCode=HIGH&take=20`
- La bandeja consolida pendientes de completitud, issues de integridad y revision de retencion; no crea tareas persistidas, asignaciones ni acciones masivas.
- El resumen ejecutivo documental se valida con:
  - `GET /api/documents/summary`
- El resumen devuelve KPIs principales, desglose por modulo, desglose por clase documental y categorias de work queue.
- El resumen respeta permisos de lectura por modulo y excluye documentos de `ModuleCode` no mapeados en la autorizacion documental transversal.
- La remediacion contextual directa se valida desde los paneles de entidad y reutiliza uploads endurecidos:
  - `MarketTenant`: `POST /api/markets/tenants/{tenantId}/cedula` con multipart `certificateFile`.
  - `DonationApplication`: `POST /api/donations/applications/{applicationId}/evidences` con `evidenceTypeId`, `description` opcional y `file`.
  - `FederationDonationApplication`: `POST /api/federation/applications/{applicationId}/evidences` con `evidenceTypeId`, `description` opcional y `file`.
- Solo usuarios con permisos de escritura del modulo pueden remediar; `READONLY` debe recibir `403` al intentar uploads.
- Despues del upload contextual, `GET /api/documents/requirements/by-entity` y `GET /api/documents/completeness/by-entity` deben pasar a `COMPLETE` cuando el faltante era el unico requisito pendiente.
- Si se carga una nueva cédula para el mismo `MarketTenant`, la cédula anterior queda `ARCHIVED` y superseded; el nuevo documento queda `ACTIVE` y ambos se enlazan por `replacedDocumentId`/`supersededByDocumentId`.
- Para validar reemplazo: subir una cédula inicial, volver a llamar `POST /api/markets/tenants/{tenantId}/cedula`, consultar `GET /api/documents/by-entity?...&includeArchived=true` y confirmar un documento `ACTIVE` vigente y uno `ARCHIVED` reemplazado.
- Los documentos reemplazados siguen descargables con permiso de lectura e integridad `VALID`; no cuentan para completitud documental minima.
- La historia documental minima se valida con:
  - `GET /api/documents/{documentId}/timeline`
  - `GET /api/documents/timeline/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId={tenantId}`
  - `GET /api/documents/timeline/by-entity?moduleCode=DONATARIAS&entityType=DONATION_APPLICATION&entityId={applicationId}`
  - `GET /api/documents/timeline/by-entity?moduleCode=FEDERATION&entityType=FEDERATION_DONATION_APPLICATION&entityId={applicationId}`
- En un reemplazo de cédula, el timeline del documento vigente debe mostrar `DOCUMENT_REPLACED` relacionado al documento previo, y el timeline del documento archivado debe mostrar `DOCUMENT_SUPERSEDED` relacionado al documento vigente.
- Por defecto `GET /api/documents` devuelve solo documentos `ACTIVE`.
- `ADMIN` puede archivar/restaurar logicamente documentos desde la API transversal:
  - `POST /api/documents/{documentId}/archive` con body `{"reason":"Motivo breve"}`
  - `POST /api/documents/{documentId}/restore`
- `OPERATOR` y `READONLY` deben recibir `403` en archivado/restauracion.
- `ADMIN` puede editar metadata minima desde la API transversal:
  - `PATCH /api/documents/{documentId}/metadata`
  - body permitido: `documentClassCode`, `businessPurpose`, `isPrimaryDocument`, `classificationNotes`
- La edicion de metadata no cambia rutas fisicas, tamano, hash, content-type, integridad, modulo, entidad ni estado.
- Solo puede existir un documento `ACTIVE` principal por `documentAreaCode + entityType + entityId`; al marcar uno nuevo, la API desmarca otros principales activos del mismo conjunto.
- `OPERATOR` y `READONLY` deben recibir `403` en edicion de metadata.
- La retencion documental minima se consulta como metadata operativa:
  - politicas: `CERTIFICATE_REVIEW`, `SIGNED_LONG_TERM`, `EVIDENCE_MEDIUM_TERM`, `GENERIC_REVIEW`
  - estados calculados: `ACTIVE_RETENTION`, `REVIEW_DUE`, `EXPIRED_RETENTION`
  - `RetentionUntilUtc` se calcula desde `CreatedUtc` segun la politica derivada por clase documental.
- La retencion baseline queda en `retentionBaselinePolicyCode` / `retentionBaselineUntilUtc`; la retencion efectiva queda en `retentionEffectivePolicyCode` / `retentionEffectiveUntilUtc`.
- `ADMIN` puede operar override minimo de retencion:
  - `PATCH /api/documents/{documentId}/retention-override`
  - `DELETE /api/documents/{documentId}/retention-override`
- El override exige motivo breve y puede ajustar politica, fecha objetivo o ambas. Review queue, work queue y summary usan la retencion efectiva.
- `EXPIRED_RETENTION` no borra, no mueve ni bloquea descargas; solo indica que el documento debe revisarse operativamente.
- La bandeja de revision de retencion es ADMIN-only:
  - `GET /api/documents/review-queue?take=20`
  - `GET /api/documents/review-queue?moduleCode=DONATIONS&retentionReviewStatusCode=REVIEW_DEFERRED&take=20`
  - `PATCH /api/documents/{documentId}/retention-review`
- Estados de revision: `REVIEW_PENDING`, `REVIEW_COMPLETED`, `REVIEW_DEFERRED`.
- Marcar revisado o diferir revision no borra, no mueve, no archiva, no cambia la politica base y no bloquea descargas autorizadas.
- `OPERATOR` y `READONLY` deben recibir `403` al consultar u operar la bandeja de revision.
- Los documentos `ARCHIVED` siguen siendo descargables para usuarios con permiso de lectura del modulo mientras la integridad sea `VALID`.
- La clasificacion documental minima disponible es:
  - cédulas de Mercados: `CERTIFICATE`, `isPrimaryDocument=true`
  - evidencias PDF de Donatarias/Federacion: `SUPPORTING_DOCUMENT`
  - evidencias JPEG/PNG de Donatarias/Federacion: `PHOTO_EVIDENCE`
  - documentos no mapeados: `OTHER`
- El catalogo transversal no expone `StoredRelativePath` ni rutas fisicas internas; la descarga se resuelve por `documentId`.
- Con la matriz vigente, `READONLY`, `OPERATOR` y `ADMIN` tienen permisos de lectura de estos documentos; para comprobar denegacion por permisos con roles oficiales se debe usar una escritura documental con `READONLY`, que debe responder `403`.

Archivos temporales utiles para validar:
```bash
mkdir -p /tmp/fmcpa-doc-upload-validation
printf '%b' '%PDF-1.4\n1 0 obj\n<<>>\nendobj\ntrailer\n<<>>\n%%EOF\n' > /tmp/fmcpa-doc-upload-validation/valid.pdf
: > /tmp/fmcpa-doc-upload-validation/empty.pdf
printf '%s' 'not an allowed executable' > /tmp/fmcpa-doc-upload-validation/invalid.exe
dd if=/dev/zero of=/tmp/fmcpa-doc-upload-validation/oversized.pdf bs=1M count=11
```

Ejemplo de descarga con headers:
```bash
curl -i http://127.0.0.1:5080/api/markets/tenants/<tenant-id>/cedula \
  -H "Authorization: Bearer ${TOKEN}"
```

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
curl -s "http://127.0.0.1:5080/api/documents?take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents?moduleCode=MARKETS&integrityState=VALID&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents?documentClassCode=CERTIFICATE&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents?retentionPolicyCode=EVIDENCE_MEDIUM_TERM&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents?retentionStatusCode=EXPIRED_RETENTION&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents?documentOperationalStatusCode=ON_HOLD&includeArchived=true&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents?documentOperationalStatusCode=INTEGRITY_ISSUE&includeArchived=true&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId=${TENANT_ID}&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/by-entity?moduleCode=DONATARIAS&entityType=DONATION_APPLICATION&entityId=${DONATION_APPLICATION_ID}&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/by-entity?moduleCode=FEDERATION&entityType=FEDERATION_DONATION_APPLICATION&entityId=${FEDERATION_APPLICATION_ID}&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/rules" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/requirements/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId=${TENANT_ID}" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/completeness/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId=${TENANT_ID}" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/pending?take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/work-queue?take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/work-queue?workItemType=DOCUMENT_INTEGRITY_ISSUE&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/work-queue?severityCode=HIGH&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/summary" -H "Authorization: Bearer ${TOKEN}"

# El summary incluye operationalStatuses y la work queue incluye estado operativo cuando el item apunta a un documento.
curl -i -X POST "http://127.0.0.1:5080/api/markets/tenants/${TENANT_ID}/cedula" -H "Authorization: Bearer ${TOKEN}" -F "certificateFile=@/tmp/fmcpa-doc-upload-validation/valid.pdf;type=application/pdf"
curl -i -X POST "http://127.0.0.1:5080/api/markets/tenants/${TENANT_ID}/cedula" -H "Authorization: Bearer ${TOKEN}" -F "certificateFile=@/tmp/fmcpa-doc-upload-validation/replacement.pdf;type=application/pdf"
curl -s "http://127.0.0.1:5080/api/documents/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId=${TENANT_ID}&includeArchived=true&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}/timeline" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/timeline/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId=${TENANT_ID}&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/review-queue?take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/review-queue?retentionReviewStatusCode=REVIEW_DEFERRED&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents?statusCode=ARCHIVED&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s http://127.0.0.1:5080/api/documents/integrity?take=5 -H "Authorization: Bearer ${TOKEN}"
curl -s http://127.0.0.1:5080/api/admin/users -H "Authorization: Bearer ${TOKEN}"
curl -s http://127.0.0.1:5080/api/admin/security/summary -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/admin/security/events?take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s http://127.0.0.1:5080/api/admin/security/locked-users -H "Authorization: Bearer ${TOKEN}"

# Crear usuario interno desde ADMIN
curl -s http://127.0.0.1:5080/api/admin/users -H "Authorization: Bearer ${TOKEN}" -H 'Content-Type: application/json' -d '{"userName":"operator2","displayName":"Operador interno","roleCode":"OPERATOR","password":"TempPassword123!"}'

# La misma mutacion simulando browser debe incluir origen permitido y header de cliente web
curl -i http://127.0.0.1:5080/api/admin/users -H "Authorization: Bearer ${TOKEN}" -H 'Origin: http://127.0.0.1:4200' -H 'X-FMCPA-Client: FMCPA-Web' -H 'Content-Type: application/json' -d '{"userName":"browserprobe","displayName":"Browser Probe","roleCode":"READONLY","password":"BrowserProbe123"}'

# Archivar/restaurar un documento desde ADMIN
DOCUMENT_ID="<document-id-devuelto-por-api-documents>"
curl -i -X PATCH "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}/metadata" -H "Authorization: Bearer ${TOKEN}" -H 'Content-Type: application/json' -d '{"documentClassCode":"CERTIFICATE","businessPurpose":"Validacion local de metadata","isPrimaryDocument":true,"classificationNotes":"Ajuste local"}'
curl -i -X POST "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}/archive" -H "Authorization: Bearer ${TOKEN}" -H 'Content-Type: application/json' -d '{"reason":"Validacion local"}'
curl -s "http://127.0.0.1:5080/api/documents?statusCode=ARCHIVED&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -i "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}/download" -H "Authorization: Bearer ${TOKEN}"
curl -i -X POST "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}/restore" -H "Authorization: Bearer ${TOKEN}"

# Revisar o diferir retencion desde ADMIN
curl -i -X PATCH "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}/retention-review" -H "Authorization: Bearer ${TOKEN}" -H 'Content-Type: application/json' -d '{"retentionReviewStatusCode":"REVIEW_COMPLETED","nextRetentionReviewUtc":null,"retentionReviewNotes":"Revision local completada"}'
curl -i -X PATCH "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}/retention-review" -H "Authorization: Bearer ${TOKEN}" -H 'Content-Type: application/json' -d '{"retentionReviewStatusCode":"REVIEW_DEFERRED","nextRetentionReviewUtc":"2030-05-05T00:00:00+00:00","retentionReviewNotes":"Revision diferida localmente"}'

# Establecer o limpiar override administrativo de retencion desde ADMIN
curl -i -X PATCH "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}/retention-override" -H "Authorization: Bearer ${TOKEN}" -H 'Content-Type: application/json' -d '{"retentionOverridePolicyCode":"GENERIC_REVIEW","retentionOverrideUntilUtc":"2026-05-01T00:00:00+00:00","retentionOverrideReason":"Validacion local de override"}'
curl -s "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/review-queue?take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/work-queue?workItemType=RETENTION_REVIEW&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/summary" -H "Authorization: Bearer ${TOKEN}"
curl -i -X DELETE "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}/retention-override" -H "Authorization: Bearer ${TOKEN}"

# Establecer o limpiar hold administrativo minimo desde ADMIN
curl -i -X POST "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}/hold" -H "Authorization: Bearer ${TOKEN}" -H 'Content-Type: application/json' -d '{"reason":"Validacion local de hold administrativo"}'
curl -s "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/review-queue?take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/work-queue?workItemType=RETENTION_REVIEW&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/summary" -H "Authorization: Bearer ${TOKEN}"
HOLD_OPERATOR_TOKEN="<token-operator>"
HOLD_READONLY_TOKEN="<token-readonly>"
curl -i -X POST "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}/hold" -H "Authorization: Bearer ${HOLD_OPERATOR_TOKEN}" -H 'Content-Type: application/json' -d '{"reason":"Intento operator"}'
curl -i -X DELETE "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}/hold" -H "Authorization: Bearer ${HOLD_READONLY_TOKEN}"
curl -i -X DELETE "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}/hold" -H "Authorization: Bearer ${TOKEN}"

# Verificar invalidacion real de un token viejo por cambio de rol
USER_ID="<id-devuelto-por-la-alta>"
curl -s http://127.0.0.1:5080/api/auth/login -H 'Content-Type: application/json' -d '{"userName":"operator2","password":"TempPassword123!"}'
OLD_USER_TOKEN="<token-del-usuario-antes-del-cambio>"
curl -i http://127.0.0.1:5080/api/auth/session -H "Authorization: Bearer ${OLD_USER_TOKEN}"
curl -s http://127.0.0.1:5080/api/admin/users/${USER_ID}/role -X PATCH -H "Authorization: Bearer ${TOKEN}" -H 'Content-Type: application/json' -d '{"roleCode":"READONLY"}'
curl -i http://127.0.0.1:5080/api/auth/session -H "Authorization: Bearer ${OLD_USER_TOKEN}"
# El ultimo comando debe devolver 401; repetir el mismo patron con desactivacion o reset de password emitiendo un token nuevo antes de cada cambio sensible.

# Activar/desactivar y resetear password
curl -s http://127.0.0.1:5080/api/admin/users/${USER_ID}/activation -X PATCH -H "Authorization: Bearer ${TOKEN}" -H 'Content-Type: application/json' -d '{"isActive":false}'
curl -s http://127.0.0.1:5080/api/admin/users/${USER_ID}/activation -X PATCH -H "Authorization: Bearer ${TOKEN}" -H 'Content-Type: application/json' -d '{"isActive":true}'
curl -s http://127.0.0.1:5080/api/admin/users/${USER_ID}/reset-password -H "Authorization: Bearer ${TOKEN}" -H 'Content-Type: application/json' -d '{"newPassword":"TempPassword456!"}'

# Validar password policy y lockout minimo
curl -i http://127.0.0.1:5080/api/admin/users -H "Authorization: Bearer ${TOKEN}" -H 'Content-Type: application/json' -d '{"userName":"weakpass","displayName":"Weak Pass","roleCode":"READONLY","password":"password"}'
curl -i http://127.0.0.1:5080/api/auth/login -H 'Content-Type: application/json' -d '{"userName":"operator2","password":"WrongPassword123!"}'
# Repetir el fallo hasta recibir 423 Locked.
curl -s http://127.0.0.1:5080/api/admin/users/${USER_ID}/unlock -H "Authorization: Bearer ${TOKEN}" -H 'Content-Type: application/json' -d '{}'

# Cambio self-service de password del usuario autenticado
SELF_TOKEN="<token-del-usuario-actual>"
curl -i http://127.0.0.1:5080/api/auth/change-password -H "Authorization: Bearer ${SELF_TOKEN}" -H 'Content-Type: application/json' -d '{"currentPassword":"<password-actual>","newPassword":"NuevaClave123","confirmNewPassword":"NuevaClave123"}'
curl -i http://127.0.0.1:5080/api/auth/session -H "Authorization: Bearer ${SELF_TOKEN}"
# El segundo comando debe devolver 401; despues iniciar sesion con la nueva password.

# OPERATOR puede escribir operaciones funcionales normales
curl -s http://127.0.0.1:5080/api/auth/login -H 'Content-Type: application/json' -d '{"userName":"operator","password":"<password-operator-local>"}'
OPERATOR_TOKEN="<token-operator>"
curl -i http://127.0.0.1:5080/api/contacts -H "Authorization: Bearer ${OPERATOR_TOKEN}" -H 'Content-Type: application/json' -d '{"name":"Probe","contactTypeId":1}'
curl -i http://127.0.0.1:5080/api/commission-types -H "Authorization: Bearer ${OPERATOR_TOKEN}" -H 'Content-Type: application/json' -d '{"code":"PROBE","name":"Probe","sortOrder":900}'
curl -i http://127.0.0.1:5080/api/admin/users -H "Authorization: Bearer ${OPERATOR_TOKEN}"
curl -i http://127.0.0.1:5080/api/admin/security/summary -H "Authorization: Bearer ${OPERATOR_TOKEN}"

# READONLY solo consulta
curl -s http://127.0.0.1:5080/api/auth/login -H 'Content-Type: application/json' -d '{"userName":"readonly","password":"<password-readonly-local>"}'
READONLY_TOKEN="<token-readonly>"
curl -i http://127.0.0.1:5080/api/contacts -H "Authorization: Bearer ${READONLY_TOKEN}" -H 'Content-Type: application/json' -d '{"name":"Probe","contactTypeId":1}'
curl -i http://127.0.0.1:5080/api/admin/users -H "Authorization: Bearer ${READONLY_TOKEN}"
curl -i http://127.0.0.1:5080/api/admin/security/summary -H "Authorization: Bearer ${READONLY_TOKEN}"

curl -s http://127.0.0.1:4200/
curl -s http://127.0.0.1:4200/documents
curl -s http://127.0.0.1:4200/documents/review
curl -s http://127.0.0.1:4200/operations
curl -s http://127.0.0.1:5080/api/operations/summary -H "Authorization: Bearer ${ADMIN_TOKEN}"
curl -s "http://127.0.0.1:5080/api/operations/work-queue?take=20" -H "Authorization: Bearer ${ADMIN_TOKEN}"
curl -s "http://127.0.0.1:5080/api/operations/work-queue?categoryCode=SECURITY&take=20" -H "Authorization: Bearer ${READONLY_TOKEN}"
curl -s http://127.0.0.1:4200/admin/users
./scripts/local/smoke-mvp.sh
```

## Troubleshooting minimo
- Si `doctor.sh` marca `WARNING` por puertos ocupados, revisar si ya existe una instancia previa del proyecto o usar overrides en `.env.local`.
- Si `docker compose` no puede levantar `fmcpa-sql`, revisar Docker y liberar el puerto `14333`.
- Si `docker` no existe en la distro WSL, habilitar la integracion de Docker Desktop para esa distro o usar una instancia SQL Server local externa y aplicar migraciones con `dotnet ef database update`.
- Si `run-backend.sh` falla por conexion, volver a correr `up-sqlserver.sh` y `apply-migrations.sh`.
- Si `dotnet ef database update` falla con SQL Server no accesible en `localhost,1433`, confirmar puerto/cadena local antes de validar flujos documentales por HTTP real.
- Si `run-frontend.sh` levanta pero no conecta, verificar que backend y frontend compartan el mismo `FMCPA_API_PORT`; el proxy se genera con ese valor.
- Si `run-backend.sh` levanta pero el login falla con `401`, verificar que `FMCPA_AUTH_BOOTSTRAP_PASSWORD` exista en el entorno local y que el usuario bootstrap se haya aprovisionado en `Development`.
- Si el login de `operator` o `readonly` falla con `401`, verificar que `FMCPA_AUTH_OPERATOR_PASSWORD` o `FMCPA_AUTH_READONLY_PASSWORD` existan en el entorno local y repetir `dev-up.sh` para que el backend sincronice esos usuarios.
- Si un usuario recibe `423 Locked`, esperar el cooldown configurado o entrar como `ADMIN` y usar `/admin/users` para limpiar el lockout.
- Si `/admin/security` no muestra eventos esperados, generar primero actividad de auth, por ejemplo un login fallido, lockout o reset administrativo.
- Si `/operations` no muestra seguridad con `OPERATOR` o `READONLY`, es el comportamiento esperado; la seccion se incluye solo con `USERS_ADMIN`.
- Si `/api/operations/work-queue?categoryCode=SECURITY` responde sin items para un rol no admin, confirma el filtrado por permisos de superficie.
- Si el bootstrap local no crea o sincroniza un usuario, revisar que la password configurada cumpla la politica minima; el backend registra un warning y omite ese usuario si la password es debil.
- Si un usuario cambia su propia password, la sesion local se limpia y el token anterior queda invalido por `SecurityStamp`; debe iniciar sesion con la nueva password.
- Si un usuario gestionado cambia de rol, se desactiva o se le resetea el password, cualquier token previo deja de servir; volver a iniciar sesion con el rol/password vigentes.
- Si una mutacion probada con `curl` devuelve `400 Solicitud web no permitida`, revisar si se envio `Origin`, `Referer` o `Sec-Fetch-*` sin `X-FMCPA-Client: FMCPA-Web`.
- Si una mutacion probada desde browser devuelve `403 Origen web no permitido`, revisar `Cors:AllowedOrigins` y el origin real del frontend/proxy local.
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
- [Security Track Session Invalidation Implementation Note](../05-post-mvp/security-track-session-invalidation-implementation-note.md)
- [Security Track Module Authorization Implementation Note](../05-post-mvp/security-track-module-authorization-implementation-note.md)
- [Security Track Credential Hardening And Auth Audit Implementation Note](../05-post-mvp/security-track-credential-hardening-and-auth-audit-implementation-note.md)
- [Security Track Self-Service Password Change Implementation Note](../05-post-mvp/security-track-self-service-password-change-implementation-note.md)
- [Security Track Web Origin Protection Implementation Note](../05-post-mvp/security-track-web-origin-protection-implementation-note.md)
- [Security Track Admin Security Observability Implementation Note](../05-post-mvp/security-track-admin-security-observability-implementation-note.md)

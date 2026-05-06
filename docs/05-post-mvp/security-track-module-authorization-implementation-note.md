# Security Track Module Authorization Implementation Note

## Objetivo
- Continuar `Track 2` con una capa minima de autorizacion por modulo/superficie funcional.
- Mantener una solucion intermedia entre roles gruesos (`read/write/admin`) y RBAC ultra fino por endpoint/accion individual.

## Que se implemento

### Backend
- Se agrego `PlatformPermissionCodes` con permisos derivados por `ApplicationUser.RoleCode`.
- `POST /api/auth/login` emite un claim `fmcpa_permission` por cada permiso del rol.
- `/api/auth/session` devuelve los permisos junto con el usuario autenticado.
- `ApplicationUserTokenValidationService` valida que:
  - el usuario siga activo
  - el rol del token coincida con el rol persistido
  - el `SecurityStamp` siga vigente
  - los claims `fmcpa_permission` coincidan exactamente con la matriz vigente del rol
- Se agregaron policies reutilizables por superficie:
  - Dashboard
  - Historico, bitacora, comisiones consolidadas e integridad documental
  - Contactos
  - Mercados
  - Donatarias
  - Financieras
  - Federacion
  - Catalogos compartidos
  - Administracion de usuarios
  - Cierres formales
- Los grupos de endpoints dejaron de depender de `read/write/admin` grueso y pasaron a permisos por modulo/superficie.

### Frontend Angular
- La sesion local conserva `permissions`.
- `AuthService` expone helpers por superficie, por ejemplo:
  - `canWriteMarkets`
  - `canWriteDonations`
  - `canWriteFinancials`
  - `canWriteFederation`
  - `canAdministerCatalogs`
  - `canAdministerUsers`
  - `canAdministerFormalClose`
- Las rutas principales usan `permissionGuard` con `requiredPermission`.
- La navegacion se filtra por permiso.
- Las acciones evidentes de escritura y cierre usan helpers por modulo/superficie.

## Matriz rol -> permisos

| Rol | Permisos |
| --- | --- |
| `READONLY` | `DASHBOARD_READ`, `HISTORY_READ`, `CONTACTS_READ`, `MARKETS_READ`, `DONATIONS_READ`, `FINANCIALS_READ`, `FEDERATION_READ`, `CATALOGS_READ` |
| `OPERATOR` | Todos los permisos de `READONLY`, mas `CONTACTS_WRITE`, `MARKETS_WRITE`, `DONATIONS_WRITE`, `FINANCIALS_WRITE`, `FEDERATION_WRITE` |
| `ADMIN` | Todos los permisos de `OPERATOR`, mas `CATALOGS_ADMIN`, `USERS_ADMIN`, `FORMAL_CLOSE_ADMIN` |

## Como se aplico en backend
- `GET /api/dashboard/*` requiere `DASHBOARD_READ`.
- Bitacora, historico, comisiones consolidadas e integridad documental requieren `HISTORY_READ`.
- Contactos usa `CONTACTS_READ` y `CONTACTS_WRITE`.
- Mercados usa `MARKETS_READ` y `MARKETS_WRITE`.
- Donatarias usa `DONATIONS_READ` y `DONATIONS_WRITE`.
- Financieras usa `FINANCIALS_READ` y `FINANCIALS_WRITE`.
- Federacion usa `FEDERATION_READ` y `FEDERATION_WRITE`.
- Catalogos compartidos usan `CATALOGS_READ` para consulta y `CATALOGS_ADMIN` para altas.
- Usuarios internos usan `USERS_ADMIN`.
- Cierres formales requieren `FORMAL_CLOSE_ADMIN` combinado con el permiso `*_WRITE` del modulo.

## Decisiones tomadas
- No se creo migracion: los permisos se derivan del rol y no se persisten por usuario.
- No se abrio UI de permisos por usuario; `ADMIN` sigue asignando solo rol.
- Se agregaron `CONTACTS_READ` y `CONTACTS_WRITE` porque Contactos es una superficie operativa compartida, no un catalogo administrativo.
- Se agrego `FORMAL_CLOSE_ADMIN` como permiso transversal para conservar cierres formales fuera de `OPERATOR` sin crear permisos ultra finos como `MARKETS_CLOSE_ONLY`.
- Los tokens viejos sin claims `fmcpa_permission` quedan rechazados por la validacion viva y el frontend limpia la sesion ante `401`.

## Como validarlo localmente

### Arranque aislado sugerido
```bash
FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-moduleauth-14337 \
FMCPA_SQL_PORT=14337 \
FMCPA_DB_NAME=FMCPA_ModuleAuthorization_20260504 \
FMCPA_API_PORT=5094 \
FMCPA_WEB_PORT=4203 \
FMCPA_AUTH_BOOTSTRAP_PASSWORD='LocalAdmin123!Aa' \
FMCPA_AUTH_OPERATOR_PASSWORD='LocalOperator123!Aa' \
FMCPA_AUTH_READONLY_PASSWORD='LocalReadonly123!Aa' \
FMCPA_AUTH_JWT_SIGNING_KEY='ModuleAuthorizationLocalSigningKey20260504OnlyForValidation1234567890' \
./scripts/local/apply-migrations.sh
```

Luego levantar backend con los mismos overrides:
```bash
FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-moduleauth-14337 \
FMCPA_SQL_PORT=14337 \
FMCPA_DB_NAME=FMCPA_ModuleAuthorization_20260504 \
FMCPA_API_PORT=5094 \
FMCPA_WEB_PORT=4203 \
FMCPA_AUTH_BOOTSTRAP_PASSWORD='LocalAdmin123!Aa' \
FMCPA_AUTH_OPERATOR_PASSWORD='LocalOperator123!Aa' \
FMCPA_AUTH_READONLY_PASSWORD='LocalReadonly123!Aa' \
FMCPA_AUTH_JWT_SIGNING_KEY='ModuleAuthorizationLocalSigningKey20260504OnlyForValidation1234567890' \
./scripts/local/run-backend.sh
```

### Verificaciones minimas por API
```bash
BASE_URL=http://127.0.0.1:5094

curl -s "${BASE_URL}/api/auth/login" -H 'Content-Type: application/json' -d '{"userName":"admin","password":"LocalAdmin123!Aa"}'
curl -s "${BASE_URL}/api/auth/login" -H 'Content-Type: application/json' -d '{"userName":"operator","password":"LocalOperator123!Aa"}'
curl -s "${BASE_URL}/api/auth/login" -H 'Content-Type: application/json' -d '{"userName":"readonly","password":"LocalReadonly123!Aa"}'

# READONLY puede leer modulos, pero no escribir.
curl -i "${BASE_URL}/api/markets" -H "Authorization: Bearer ${READONLY_TOKEN}"
curl -i "${BASE_URL}/api/contacts" -X POST -H "Authorization: Bearer ${READONLY_TOKEN}" -H 'Content-Type: application/json' -d '{"name":"Probe","contactTypeId":1}'

# OPERATOR puede escribir superficies operativas, pero no administrar catalogos, usuarios ni cierres formales.
curl -i "${BASE_URL}/api/contacts" -X POST -H "Authorization: Bearer ${OPERATOR_TOKEN}" -H 'Content-Type: application/json' -d '{"name":"Probe","contactTypeId":1}'
curl -i "${BASE_URL}/api/commission-types" -X POST -H "Authorization: Bearer ${OPERATOR_TOKEN}" -H 'Content-Type: application/json' -d '{"code":"PROBE","name":"Probe","sortOrder":900}'
curl -i "${BASE_URL}/api/admin/users" -H "Authorization: Bearer ${OPERATOR_TOKEN}"
curl -i "${BASE_URL}/api/markets/11111111-1111-1111-1111-111111111111/close" -X POST -H "Authorization: Bearer ${OPERATOR_TOKEN}" -H 'Content-Type: application/json' -d '{"reason":"probe"}'

# ADMIN puede administrar usuarios.
curl -i "${BASE_URL}/api/admin/users" -H "Authorization: Bearer ${ADMIN_TOKEN}"
```

## Validacion ejecutada
- `dotnet restore src/backend/FMCPA.Backend.sln` -> exitoso.
- `dotnet build src/backend/FMCPA.Backend.sln` -> exitoso, `0 Warning(s)`, `0 Error(s)`.
- `npm run build` en `src/frontend` -> exitoso.
- `npx vitest run src/app/core/interceptors/auth.interceptor.spec.ts src/app/core/services/auth.service.spec.ts src/app/core/guards/auth.guard.spec.ts src/app/app.routes.spec.ts` -> `4 passed`, `11 passed`.
- `dotnet tool restore` -> `dotnet-ef` `10.0.6` restaurado.
- Migraciones aplicadas sobre `FMCPA_ModuleAuthorization_20260504`; no se genero migracion nueva.
- Backend validado en `http://127.0.0.1:5094/health`.
- Resultado runtime real:
  - login de `ADMIN`, `OPERATOR` y `READONLY`: `200`
  - claims de permisos esperados por rol: correctos
  - `READONLY` lee Mercados: `200`
  - `OPERATOR` lee Financieras: `200`
  - `READONLY` lee Historico: `200`
  - `OPERATOR` crea Contacto: `201`
  - `READONLY` crea Contacto: `403`
  - `OPERATOR` alta catalogo sensible: `403`
  - `OPERATOR` usuarios admin: `403`
  - `ADMIN` usuarios admin: `200`
  - `OPERATOR` cierre formal de Mercado: `403`
  - token previo a cambio de rol: `200` antes del cambio y `401` despues del cambio

## Que quedo fuera
- RBAC ultra fino por endpoint o accion individual.
- Permisos manuales por usuario.
- Permisos por recurso, expediente, municipio o registro.
- Refresh tokens, blacklist distribuida o sesiones avanzadas.
- Proveedor externo de identidad.
- Cambios de produccion o CI/CD.

# Security Track Admin Security Observability Implementation Note

## Que se implemento
- Endpoints ADMIN-only bajo `/api/admin/security`.
- Vista Angular minima `/admin/security`.
- Filtros simples para eventos SECURITY.
- Seccion de usuarios actualmente bloqueados.
- Accion de unlock reutilizando `/api/admin/users/{id}/unlock`.
- Pruebas backend de acceso ADMIN, `403` para no ADMIN, consulta de eventos y flujo de lockout/unlock.

## Decisiones tomadas
- Se reutiliza `AuditEvent` con `ModuleCode = SECURITY`.
- Se reutiliza `ApplicationUser.AccessFailedCount` y `ApplicationUser.LockoutEndUtc`.
- No se agrega migracion ni tablas nuevas.
- La autorizacion usa `USERS_ADMIN`, igual que la gestion minima de usuarios.
- La vista es operativa y acotada; no incorpora graficas, correlacion, IP intelligence ni analitica pesada.

## Que ve ADMIN
- Resumen reciente:
  - eventos SECURITY del rango;
  - logins exitosos y fallidos;
  - denegaciones por lockout;
  - lockouts temporales y resets;
  - resets de password;
  - cambios de rol;
  - activaciones/desactivaciones;
  - usuarios actualmente bloqueados.
- Eventos recientes con detalle, usuario de referencia, entidad y fecha UTC.
- Usuarios con lockout activo y boton para limpiar lockout.

## Filtros existentes
Endpoint `GET /api/admin/security/events`:
- `userId`
- `userName`
- `eventType`
- `fromUtc`
- `toUtc`
- `take` entre `1` y `200`

Endpoint `GET /api/admin/security/summary`:
- `hours` entre `1` y `720`

Endpoint `GET /api/admin/security/locked-users`:
- sin filtros adicionales; devuelve lockouts activos al momento de la consulta.

## Como validarlo localmente
Comandos:

```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter FullyQualifiedName~SecurityObservabilityTests --logger "console;verbosity=minimal"
npm run build
npm test -- --watch=false
```

Validacion manual:

```bash
curl -s http://127.0.0.1:5080/api/admin/security/summary \
  -H "Authorization: Bearer ${ADMIN_TOKEN}"

curl -s "http://127.0.0.1:5080/api/admin/security/events?eventType=AUTH_LOGIN_FAILED&take=20" \
  -H "Authorization: Bearer ${ADMIN_TOKEN}"

curl -s http://127.0.0.1:5080/api/admin/security/locked-users \
  -H "Authorization: Bearer ${ADMIN_TOKEN}"

curl -i http://127.0.0.1:5080/api/admin/security/summary \
  -H "Authorization: Bearer ${OPERATOR_TOKEN}"
```

Resultado esperado:
- `ADMIN` recibe `200`.
- `OPERATOR` y `READONLY` reciben `403`.
- Si existe un lockout activo, aparece en `locked-users` y puede limpiarse desde `/admin/security`.

## Que quedo fuera
- SIEM.
- Analitica avanzada.
- Antifraude complejo.
- MFA.
- Correlacion por dispositivo/IP.
- Exportaciones, reportes pesados o graficas.
- Nuevas tablas o migraciones.
- Cambios de produccion o CI/CD.

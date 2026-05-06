# Security Track Session Invalidation Implementation Note

## Objetivo
- Continuar `Track 2` endureciendo la validez real de sesion/token para que tokens JWT previamente emitidos dejen de aceptarse cuando cambia el estado sensible del usuario.
- Mantener el alcance minimo: sin refresh tokens, blacklist distribuida, revocacion avanzada, sesiones multi-dispositivo, proveedor externo de identidad ni RBAC fino adicional.

## Que se implemento

### Backend
- Se reutiliza la base ya existente de `SecurityStamp` en `ApplicationUser`.
- El JWT emitido por `POST /api/auth/login` incluye `fmcpa_security_stamp`.
- El pipeline `JWT bearer` ejecuta `ApplicationUserTokenValidationService` en `OnTokenValidated`.
- Cada request autenticado valida contra base:
  - `sub` valido como `Guid`
  - claim `fmcpa_security_stamp` presente
  - usuario existente
  - `IsActive = true`
  - `SecurityStamp` del token igual al persistido
- Si cualquiera de esas condiciones falla, el middleware rechaza el token y la API responde `401`.

### Eventos que invalidan sesion
- Cambio de rol mediante `ApplicationUser.ChangeRole`.
- Desactivacion o reactivacion mediante `ApplicationUser.SetIsActive`.
- Reset administrativo de password mediante `ApplicationUser.ResetPassword`.
- Sincronizacion bootstrap local que cambie password, rol o reactive un usuario en `Development`.

### Frontend Angular
- El interceptor ya trata `401` de requests protegidos como sesion invalida.
- `AuthService.handleUnauthorized()` limpia la sesion en `sessionStorage` y redirige a `/login`.
- Se agregaron pruebas unitarias para fijar:
  - envio del bearer en requests protegidos
  - llamada a `handleUnauthorized()` ante `401`
  - limpieza de sesion local y navegacion a login desde `AuthService`

### Trazabilidad
- Los cambios sensibles quedan registrados en bitacora `SECURITY` con:
  - `USER_ROLE_CHANGED`
  - `USER_DEACTIVATED`
  - `USER_ACTIVATED`
  - `USER_PASSWORD_RESET`
- La validacion runtime confirmo eventos para desactivacion, cambio de rol y reset de password.

## Decisiones tomadas
- No se creo una migracion nueva: `SecurityStamp` ya existe por `Track2SecurityUserManagementAdmin`.
- No se agrego blacklist ni tabla de sesiones: el `SecurityStamp` persistido es la version minima de sesion/credenciales.
- No se agrego cache a la validacion viva: para este MVP/post-MVP se acepta una lectura de usuario por request autenticado como costo razonable.
- El frontend no distingue aun causas finas de `401`; cualquier `401` protegido limpia sesion local como comportamiento seguro por defecto.

## Como funciona la invalidacion
1. El usuario inicia sesion y recibe un JWT con su rol actual y `fmcpa_security_stamp`.
2. En cada request protegido, backend valida firma, issuer, audience, expiracion y despues consulta el usuario actual.
3. Si el usuario fue desactivado, el token falla por `IsActive = false`.
4. Si el rol o password cambiaron, el token falla porque el `SecurityStamp` persistido ya no coincide con el claim del JWT viejo.
5. El usuario debe volver a iniciar sesion para obtener un token con claims y stamp vigentes.

## Como validarlo localmente

### Arranque aislado sugerido
```bash
FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-sessioninvalid-14336 \
FMCPA_SQL_PORT=14336 \
FMCPA_DB_NAME=FMCPA_SessionInvalidation_20260504 \
FMCPA_API_PORT=5093 \
FMCPA_WEB_PORT=4202 \
FMCPA_AUTH_BOOTSTRAP_PASSWORD='LocalAdmin123!Aa' \
FMCPA_AUTH_JWT_SIGNING_KEY='SessionInvalidationLocalSigningKey20260504OnlyForValidation1234567890' \
./scripts/local/dev-up.sh --no-frontend
```

### Flujo API minimo
```bash
BASE_URL=http://127.0.0.1:5093

# 1. Login ADMIN y guardar TOKEN.
curl -s "${BASE_URL}/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d '{"userName":"admin","password":"LocalAdmin123!Aa"}'

# 2. Crear usuario objetivo como OPERATOR y guardar USER_ID.
curl -s "${BASE_URL}/api/admin/users" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H 'Content-Type: application/json' \
  -d '{"userName":"sessionprobe","displayName":"Session probe","roleCode":"OPERATOR","password":"InitialPassword123!Aa"}'

# 3. Login del usuario objetivo y guardar OLD_USER_TOKEN.
curl -s "${BASE_URL}/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d '{"userName":"sessionprobe","password":"InitialPassword123!Aa"}'

# 4. Confirmar que el token viejo funciona antes del cambio.
curl -i "${BASE_URL}/api/auth/session" \
  -H "Authorization: Bearer ${OLD_USER_TOKEN}"

# 5. Ejecutar un cambio sensible como ADMIN.
curl -s "${BASE_URL}/api/admin/users/${USER_ID}/role" \
  -X PATCH \
  -H "Authorization: Bearer ${TOKEN}" \
  -H 'Content-Type: application/json' \
  -d '{"roleCode":"READONLY"}'

# 6. Reusar OLD_USER_TOKEN y confirmar 401.
curl -i "${BASE_URL}/api/auth/session" \
  -H "Authorization: Bearer ${OLD_USER_TOKEN}"
```

Repetir el mismo patron emitiendo un token nuevo antes de cada cambio sensible:
- `PATCH /api/admin/users/{USER_ID}/activation` con `{"isActive":false}` debe invalidar el token previo y rechazar login mientras este desactivado.
- `POST /api/admin/users/{USER_ID}/reset-password` debe invalidar el token previo, rechazar la password anterior y aceptar la password nueva.

### Apagar la corrida aislada
```bash
FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-sessioninvalid-14336 \
FMCPA_SQL_PORT=14336 \
FMCPA_DB_NAME=FMCPA_SessionInvalidation_20260504 \
FMCPA_API_PORT=5093 \
FMCPA_WEB_PORT=4202 \
./scripts/local/dev-down.sh
```

## Validacion ejecutada
- `dotnet restore src/backend/FMCPA.Backend.sln` -> exitoso.
- `dotnet build src/backend/FMCPA.Backend.sln` -> exitoso, `0 Warning(s)`, `0 Error(s)`.
- `npm run build` en `src/frontend` -> exitoso.
- `dotnet tool restore` -> `dotnet-ef` `10.0.6` restaurado.
- `npx vitest run src/app/core/interceptors/auth.interceptor.spec.ts src/app/core/services/auth.service.spec.ts src/app/core/guards/auth.guard.spec.ts src/app/app.routes.spec.ts` -> `4 passed`, `8 passed`.
- Migraciones aplicadas sobre `FMCPA_SessionInvalidation_20260504` hasta `Track2SecurityUserManagementAdmin`; no se genero migracion nueva.
- Backend local validado en `http://127.0.0.1:5093/health`.
- Resultado runtime real:
  - token del usuario funciono antes de desactivacion: `200`
  - token viejo tras desactivacion: `401`
  - login mientras usuario estaba desactivado: `401`
  - token del usuario funciono antes de cambio de rol: `200`
  - token viejo tras cambio de rol: `401`
  - token del usuario funciono antes de reset de password: `200`
  - token viejo tras reset de password: `401`
  - password anterior tras reset: `401`
  - password nueva tras reset: `200`
  - bitacora `SECURITY` incluyo `USER_DEACTIVATED`, `USER_ROLE_CHANGED` y `USER_PASSWORD_RESET`

## Que quedo fuera
- Refresh tokens.
- Blacklist o revocacion distribuida.
- Logout remoto multi-dispositivo.
- Gestion avanzada de sesiones.
- Proveedor externo de identidad.
- RBAC fino por modulo, accion o recurso.
- Cambios de produccion o CI/CD.

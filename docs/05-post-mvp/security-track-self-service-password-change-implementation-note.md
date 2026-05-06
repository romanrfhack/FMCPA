# Security Track Self-Service Password Change Implementation Note

## Que se implemento
- Endpoint autenticado `POST /api/auth/change-password`.
- Contrato con `currentPassword`, `newPassword` y `confirmNewPassword`.
- Validacion de password actual contra el hash vigente del usuario autenticado.
- Aplicacion de `PasswordPolicyService` a la nueva password.
- Rechazo de confirmacion distinta y de nueva password igual al valor actual capturado.
- Rotacion de `SecurityStamp` mediante `ApplicationUser.ResetPassword`.
- Auditoria minima en `AuditEvent` bajo modulo `SECURITY`.
- Vista Angular minima `/account/password`.
- Limpieza de sesion local y redireccion a login tras cambio exitoso.

## Decisiones tomadas
- No se emite token nuevo despues del cambio.
- El cambio exitoso invalida el token actual y cualquier token previo por rotacion de `SecurityStamp`.
- El usuario debe iniciar sesion nuevamente con la nueva password.
- La recuperacion operativa ante perdida de password sigue siendo el reset administrativo ya existente.
- No se agrego migracion porque `ApplicationUser` ya tenia `PasswordHash`, `SecurityStamp`, `AccessFailedCount` y `LockoutEndUtc`.

## Flujo de cambio de password
1. El usuario autenticado abre `/account/password`.
2. Captura password actual, nueva password y confirmacion.
3. Frontend valida campos basicos y coincidencia de confirmacion.
4. Backend valida usuario autenticado vigente.
5. Backend valida password actual real.
6. Backend aplica la politica minima de password vigente.
7. Backend actualiza password, rota `SecurityStamp` y registra auditoria.
8. Frontend muestra confirmacion breve, limpia sesion local y redirige a login.

## Como se invalida sesion
- `ApplicationUser.ResetPassword` actualiza `PasswordHash` y rota `SecurityStamp`.
- El JWT anterior conserva el `fmcpa_security_stamp` previo.
- En el siguiente request autenticado, `ApplicationUserTokenValidationService` compara el stamp del token contra el persistido y rechaza el token viejo con `401`.

## Eventos auditados
- `AUTH_PASSWORD_CHANGED_SELF_SERVICE`: cambio exitoso por el propio usuario.
- `AUTH_PASSWORD_CHANGE_FAILED`: intento fallido por password actual incorrecta.

No se registran passwords ni secretos.

## Como validarlo localmente
1. Restaurar y compilar backend:
   ```bash
   dotnet restore src/backend/FMCPA.Backend.sln
   dotnet build src/backend/FMCPA.Backend.sln
   ```
2. Compilar frontend:
   ```bash
   npm run build
   ```
3. Aplicar migraciones existentes en una base local:
   ```bash
   ./scripts/local/dev-up.sh --no-backend --no-frontend
   ```
4. Levantar backend y hacer login con un usuario de prueba.
5. Probar `POST /api/auth/change-password` con password actual incorrecta y confirmar `400`.
6. Probar nueva password invalida y confirmar `400`.
7. Probar cambio exitoso y confirmar `200`.
8. Reusar el token previo contra `/api/auth/session` y confirmar `401`.
9. Iniciar sesion con la nueva password y confirmar `200`.
10. Consultar `/api/bitacora?moduleCode=SECURITY` y confirmar el evento de cambio self-service.
11. Levantar frontend, abrir `/account/password` y confirmar que el cambio exitoso limpia sesion y redirige a login.

## Que quedo fuera
- Forgot password o recuperacion por correo.
- MFA.
- Proveedor externo de identidad.
- Refresh tokens.
- Gestion avanzada de sesiones o logout remoto multi-dispositivo.
- Perfil completo de usuario.
- Cambios de produccion o CI/CD.

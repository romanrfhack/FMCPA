# Security Track Credential Hardening And Auth Audit Implementation Note

## Que se implemento
- Politica minima de password centralizada en `PasswordPolicyService`.
- Configuracion de credenciales en `Security:Credentials`.
- Persistencia de `AccessFailedCount` y `LockoutEndUtc` en `ApplicationUser`.
- Lockout temporal por usuario durante login.
- Eventos de autenticacion en `AuditEvent` bajo modulo `SECURITY`.
- Endpoint admin acotado para limpiar lockout: `POST /api/admin/users/{userId}/unlock`.
- Vista Angular `/admin/users` con estado basico de lockout, validacion minima de password y accion para limpiar lockout.

## Decisiones tomadas
- La politica de password se mantiene simple y local: minimo `12` caracteres, mayuscula, minuscula y numero.
- Se rechazan passwords con espacios al inicio/final y passwords obvios locales, sin usar diccionarios externos ni breach checks.
- El lockout es por usuario y persistido en `ApplicationUsers`; no sustituye al rate limit HTTP por cliente.
- El contador es consecutivo: login exitoso, reset admin, desbloqueo admin o expiracion del cooldown limpian contador y lockout.
- La auditoria usa `AuditEvent` existente y no guarda passwords ni secretos.

## Reglas de password
- `MinimumLength`: `12`
- `RequireUppercase`: `true`
- `RequireLowercase`: `true`
- `RequireDigit`: `true`
- `RequireNonAlphanumeric`: `false`
- Rechazo adicional:
  - password vacia o whitespace
  - espacios al inicio o final
  - passwords locales obviamente debiles como `password`, `password123`, `admin123`, `changeme`, `123456789012`

La politica aplica a:
- bootstrap local en `Development`
- creacion de usuario por `ADMIN`
- reset administrativo de password

## Politica de lockout
- `MaxFailedAccessAttempts`: `5`
- `CooldownSeconds`: `900`
- Al alcanzar el umbral, el login responde `423 Locked` con `Retry-After`, `lockedUntilUtc` y `retryAfterSeconds`.
- Durante lockout no se verifica password; se rechaza hasta que expire o un `ADMIN` limpie el lockout.
- Al expirar el cooldown, el siguiente intento libera automaticamente el estado antes de continuar.

## Eventos auditados
- `AUTH_LOGIN_SUCCEEDED`
- `AUTH_LOGIN_FAILED`
- `AUTH_LOGIN_LOCKOUT_DENIED`
- `USER_TEMPORARILY_LOCKED`
- `USER_LOCKOUT_EXPIRED`
- `USER_LOCKOUT_RESET`
- `USER_PASSWORD_RESET`

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
3. Generar/aplicar migracion:
   ```bash
   dotnet ef migrations add Track2CredentialHardeningAndAuthAudit --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --context PlatformDbContext --output-dir Persistence/Migrations --no-build
   ./scripts/local/dev-up.sh --no-backend --no-frontend
   ```
4. Levantar backend en `Development`.
5. Iniciar sesion como `ADMIN`.
6. Probar alta o reset con password invalida y confirmar `400`.
7. Probar alta o reset con password valida y confirmar `201`/`200`.
8. Forzar varios logins fallidos sobre un usuario existente y confirmar `423 Locked`.
9. Probar login durante lockout y confirmar que sigue `423`.
10. Limpiar lockout con `POST /api/admin/users/{userId}/unlock` o esperar cooldown.
11. Iniciar sesion con password correcta y confirmar `200`.
12. Consultar bitacora y confirmar eventos `SECURITY` de login, fallo, lockout y desbloqueo.

## Que quedo fuera
- MFA.
- CAPTCHA.
- Antifraude avanzado.
- Proveedor externo de identidad.
- Refresh tokens.
- Bloqueo distribuido o correlacion anti-abuso entre instancias.
- Breach checks o diccionarios externos de password.
- Auditoria forense completa.
- Self-service de cambio/recuperacion de password.
- Cambios de produccion o CI/CD.

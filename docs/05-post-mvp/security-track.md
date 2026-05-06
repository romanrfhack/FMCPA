# Security Track

## Objetivo
- Endurecer el acceso transversal a la plataforma post-MVP con una base minima y extensible de autenticacion y autorizacion.

## Alcance implementado actualmente
- Login backend con `JWT bearer`
- Validacion de token y endpoint de sesion actual
- Usuario bootstrap local controlado por configuracion
- Roles base `ADMIN`, `OPERATOR`, `READONLY`
- Claim de rol en JWT y `RoleCode` persistido en `ApplicationUser`
- Claims `fmcpa_permission` derivados por rol y devueltos en `/api/auth/session`
- `SecurityStamp` persistido para invalidar tokens previos en cambios sensibles de usuario
- Validacion viva de `SecurityStamp` e `IsActive` contra base en cada request autenticado
- Politicas backend por modulo/superficie funcional
- Proteccion minima de endpoints operativos, cierres formales y catalogos administrativos
- Validacion minima de uploads documentales para cédulas de Mercados, evidencias de Donatarias y evidencias de Federacion
- Descargas documentales con autorizacion por modulo, verificacion de integridad y headers seguros
- Rate limiting minimo en login y administracion de usuarios
- Headers HTTP de seguridad y cache conservadora para respuestas `/api`
- CORS controlado con fail-fast fuera de `Development`
- Validacion JWT fail-fast fuera de `Development`
- Politica minima de password para bootstrap local, alta admin y reset administrativo
- Lockout temporal por usuario con contador de intentos fallidos persistido
- Auditoria minima de autenticacion en `AuditEvent`
- Cambio self-service de password para usuario autenticado
- Cobertura de regresion backend para autenticacion/autorizacion por superficie
- Inventario explicito de superficie protegida y endpoints publicos permitidos
- Guardrails automaticos de superficie que introspectan endpoints reales y comparan allowlist/policies contra un manifiesto versionado
- Proteccion minima de origen web para mutaciones `/api` desde contexto browser
- Observabilidad operativa minima de seguridad para `ADMIN`
- Endpoints `ADMIN` para gestion minima de usuarios internos
- Login Angular, guard, interceptor, logout, pantalla `/admin/users`, pantalla `/account/password` y reflejo minimo del rol en shell y acciones visibles
- Smoke local autenticado y smoke MVP autenticado
- Validacion runtime cerrada en stack aislado local para gestion minima de usuarios y `403` por rol insuficiente
- Validacion runtime cerrada de `401` para tokens viejos tras desactivacion, cambio de rol y reset administrativo de password
- Validacion runtime cerrada de permisos por modulo con `200/201`, `403` y `401` por cambio de rol
- Validacion runtime cerrada de cambio self-service de password con `401` del token previo y login posterior con password nueva
- Validacion automatizada local de regresion con `FMCPA.Api.AuthorizationRegressionTests`

## Matriz minima rol -> permisos
- `READONLY`
  - `DASHBOARD_READ`
  - `HISTORY_READ`
  - `CONTACTS_READ`
  - `MARKETS_READ`
  - `DONATIONS_READ`
  - `FINANCIALS_READ`
  - `FEDERATION_READ`
  - `CATALOGS_READ`
- `OPERATOR`
  - Todos los permisos de `READONLY`
  - `CONTACTS_WRITE`
  - `MARKETS_WRITE`
  - `DONATIONS_WRITE`
  - `FINANCIALS_WRITE`
  - `FEDERATION_WRITE`
- `ADMIN`
  - Todos los permisos de `OPERATOR`
  - `CATALOGS_ADMIN`
  - `USERS_ADMIN`
  - `FORMAL_CLOSE_ADMIN`

## Lectura operativa por rol
- `READONLY`
  - Puede iniciar sesion y consultar dashboard, bitacora, historico, documentos, contactos y modulos operativos.
  - No puede crear registros ni ejecutar cierres formales.
- `OPERATOR`
  - Puede consultar y ejecutar escrituras funcionales normales del MVP.
  - No puede ejecutar cierres formales ni altas administrativas de catalogos compartidos.
- `ADMIN`
  - Conserva lectura, escrituras funcionales y endpoints administrativos actuales dentro del alcance del sistema local.
  - Puede listar, consultar, crear, cambiar rol, activar/desactivar y resetear password de usuarios internos.

## Mapeo minimo actual por tipo de endpoint
- Dashboard: `DASHBOARD_READ`
- Bitacora, historico, comisiones consolidadas e integridad documental: `HISTORY_READ`
- Contactos: `CONTACTS_READ` / `CONTACTS_WRITE`
- Mercados: `MARKETS_READ` / `MARKETS_WRITE`
- Donatarias: `DONATIONS_READ` / `DONATIONS_WRITE`
- Financieras: `FINANCIALS_READ` / `FINANCIALS_WRITE`
- Federacion: `FEDERATION_READ` / `FEDERATION_WRITE`
- Catálogos compartidos: `CATALOGS_READ` para consulta y `CATALOGS_ADMIN` para altas sensibles
- Cierres formales: permiso `*_WRITE` del modulo mas `FORMAL_CLOSE_ADMIN`
- Administracion de usuarios internos: `USERS_ADMIN`
- Tokens emitidos antes de un cambio sensible: `401` hasta que el usuario vuelva a iniciar sesion y obtenga claims actualizados
- Descargas documentales:
  - cédula de locatario: `MARKETS_READ`
  - evidencia de Donatarias: `DONATIONS_READ`
  - evidencia de Federacion: `FEDERATION_READ`

## Politica minima de adjuntos documentales
- Areas cubiertas:
  - cédulas digitalizadas de locatarios de Mercados
  - evidencias de aplicaciones de Donatarias
  - evidencias de aplicaciones de Federacion
- Regla comun:
  - tamano maximo `10 MB`
  - extensiones permitidas `.pdf`, `.jpg`, `.jpeg`, `.png`
  - content-types permitidos `application/pdf`, `image/jpeg`, `image/png`
  - rechazo de archivos vacios
  - saneamiento del nombre original como metadata
  - nombre fisico generado por el sistema
  - validacion basica de firma PDF/JPEG/PNG, sin sniffing complejo

## Hardening minimo de borde HTTP
- Rate limiting:
  - `POST /api/auth/login`: `10` solicitudes por minuto por cliente
  - `/api/admin/users`: `60` solicitudes por minuto por cliente/usuario autenticado
  - respuesta al exceder limite: `429 Too Many Requests` con `Retry-After` y body `application/problem+json`
- Headers:
  - `X-Content-Type-Options: nosniff`
  - `Referrer-Policy: no-referrer`
  - `X-Frame-Options: DENY`
  - `Permissions-Policy: camera=(), microphone=(), geolocation=()`
  - respuestas `/api`: `Cache-Control: no-store`, `Pragma: no-cache`, `Expires: 0`
- CORS:
  - `Development` conserva `http://localhost:4200` y `http://127.0.0.1:4200`
  - origins se configuran en `Cors:AllowedOrigins`
  - wildcard `*` se rechaza
  - fuera de `Development` se rechazan origins localhost
- JWT fuera de `Development`:
  - `Auth:Jwt:SigningKey` es obligatoria
  - signing key debe tener al menos 32 bytes para HS256
  - issuer/audience deben ser especificos del entorno, no los defaults locales
  - token lifetime debe ser positivo

## Proteccion minima de origen web
- Aplica a metodos inseguros bajo `/api`: `POST`, `PUT`, `PATCH`, `DELETE`.
- Incluye login, mutaciones funcionales, uploads, cierres formales, cambio self-service de password y administracion de usuarios cuando se invocan desde contexto browser.
- Si la solicitud trae `Origin` o `Referer`, el origen debe coincidir con `Cors:AllowedOrigins`.
- Si la solicitud trae senales browser (`Origin`, `Referer` o `Sec-Fetch-*`), debe incluir `X-FMCPA-Client: FMCPA-Web`.
- Angular agrega ese header de forma centralizada desde el interceptor para metodos inseguros `/api`.
- Scripts locales no-browser sin `Origin`, `Referer` ni `Sec-Fetch-*` siguen soportados para validacion local con bearer token.
- Rechazos:
  - origen no permitido: `403`
  - header web faltante o incorrecto en contexto browser: `400`
- Esta capa complementa JWT, permisos y CORS; no sustituye auth ni equivale a antiforgery completo.

## Observabilidad operativa minima de seguridad
- Endpoints ADMIN-only:
  - `GET /api/admin/security/summary`
  - `GET /api/admin/security/events`
  - `GET /api/admin/security/locked-users`
- La superficie reutiliza `AuditEvent` con `ModuleCode = SECURITY` y el estado persistido de `ApplicationUser`.
- Filtros disponibles en eventos:
  - `userId`
  - `userName`
  - `eventType`
  - `fromUtc`
  - `toUtc`
  - `take` hasta `200`
- Resumen basico:
  - eventos SECURITY recientes
  - logins exitosos/fallidos
  - denegaciones por lockout
  - usuarios bloqueados activos
  - usuarios con fallos acumulados
  - cambios sensibles de usuario
- Frontend:
  - ruta `/admin/security`
  - resumen, eventos recientes, usuarios bloqueados y unlock reutilizado
  - protegido por `USERS_ADMIN`

## Politica minima de credenciales y auditoria auth
- Password policy:
  - minimo `12` caracteres
  - al menos una mayuscula
  - al menos una minuscula
  - al menos un numero
  - sin espacios al inicio o final
  - rechazo de passwords locales obviamente debiles
- Aplicacion de la politica:
  - bootstrap local `Development`
  - alta de usuario por `ADMIN`
  - reset administrativo de password
- Lockout:
  - contador persistido `AccessFailedCount`
  - cooldown persistido `LockoutEndUtc`
  - `5` fallos consecutivos bloquean temporalmente la cuenta durante `15` minutos
  - login durante lockout responde `423 Locked` con `Retry-After`
  - login exitoso, reset administrativo, desbloqueo admin o expiracion del cooldown limpian el contador
- Auditoria minima:
  - `AUTH_LOGIN_SUCCEEDED`
  - `AUTH_LOGIN_FAILED`
  - `AUTH_LOGIN_LOCKOUT_DENIED`
  - `USER_TEMPORARILY_LOCKED`
  - `USER_LOCKOUT_EXPIRED`
  - `USER_LOCKOUT_RESET`
  - `USER_PASSWORD_RESET`
  - `AUTH_PASSWORD_CHANGE_FAILED`
  - `AUTH_PASSWORD_CHANGED_SELF_SERVICE`

## Cambio self-service de password
- Endpoint: `POST /api/auth/change-password`
- Requiere usuario autenticado.
- Solicita:
  - `currentPassword`
  - `newPassword`
  - `confirmNewPassword`
- Validaciones:
  - password actual real contra hash vigente
  - politica minima de password sobre `newPassword`
  - confirmacion igual a `newPassword`
  - rechazo de nueva password igual al valor actual capturado
- Comportamiento exitoso:
  - actualiza el password del usuario autenticado
  - rota `SecurityStamp` via `ApplicationUser.ResetPassword`
  - limpia contador/lockout de acceso del usuario
  - registra evento `AUTH_PASSWORD_CHANGED_SELF_SERVICE`
  - no emite token nuevo
  - frontend limpia sesion local y redirige a login

## Cobertura de regresion de autorizacion
- Proyecto: `src/backend/tests/FMCPA.Api.AuthorizationRegressionTests`
- Estrategia:
  - `WebApplicationFactory<Program>`
  - `PlatformDbContext` InMemory
  - usuarios bootstrap `ADMIN`, `OPERATOR`, `READONLY` solo para test
  - tokens reales emitidos por `/api/auth/login`
- Casos cubiertos:
  - endpoints publicos minimos: `/`, `/health`, `/api/auth/login`
  - rechazo anonimo `401` en superficies protegidas representativas
  - lecturas permitidas para `READONLY`, `OPERATOR` y `ADMIN`
  - escrituras funcionales permitidas para `OPERATOR` y `ADMIN`
  - escrituras denegadas para `READONLY`
  - cierres formales denegados para `READONLY` y `OPERATOR`, permitidos para `ADMIN`
  - catalogos administrativos y usuarios restringidos a `ADMIN`
  - cambio self-service de password e invalidacion del token previo
- Inventario:
  - `docs/05-post-mvp/security-authorization-surface-inventory.md`
- Guardrails de superficie:
  - manifiesto operativo: `docs/05-post-mvp/security-authorization-surface-guardrails.json`
  - prueba `Public_endpoints_match_the_explicit_allowlist`
  - prueba `Api_endpoints_are_either_protected_or_explicitly_public`
  - prueba `Protected_manifest_rules_have_matching_real_endpoints`
  - prueba `Protected_api_endpoints_declare_the_expected_authorization_metadata`
  - deteccion automatica via `EndpointDataSource` de ruta, metodo HTTP, metadata `Authorize` y policy
  - el inventario Markdown es la referencia humana; el JSON es la referencia ejecutable para la suite
- Proteccion de origen web:
  - prueba backend de login browser valido con origen/header esperado
  - prueba backend de request sensible browser sin header esperado
  - prueba backend de request sensible browser con origen no permitido
  - prueba backend de mutacion admin browser valida
  - prueba backend de compatibilidad con login no-browser local sin header web
  - prueba frontend de header centralizado en el interceptor Angular
- Observabilidad ADMIN de seguridad:
  - prueba backend de resumen, eventos, usuarios bloqueados y unlock reutilizado
  - prueba backend de `403` para `OPERATOR` y `READONLY`
  - prueba frontend de ruta `/admin/security` protegida con `USERS_ADMIN`

## Alcance diferido explicitamente
- RBAC ultra fino por endpoint o accion individual
- Matriz fina de permisos por endpoint
- Edicion manual de permisos por usuario
- Gestion completa de usuarios
- Perfil completo de usuario, recuperacion por correo o forgot password
- Refresh tokens, revocacion central o sesiones avanzadas
- Integraciones externas de identidad
- Antivirus, DLP o analisis profundo de contenido
- Storage externo, politica documental completa, respaldo/retencion y plataforma documental transversal
- WAF, CAPTCHA, proveedor externo de identidad y antifraude avanzado
- MFA y politicas avanzadas de identidad
- E2E pesado de seguridad con navegador
- Cambios de produccion o CI/CD

## Punto de partida real
- El MVP quedo cerrado con reservas y sin autenticacion/autorizacion completas.
- `Track 1` dejo lista una base operativa mas consistente, pero sigue pendiente de aprobacion formal.
- La foundation de autenticacion ya estaba implementada y ahora se extiende solo lo necesario para que no todos los autenticados tengan el mismo alcance.
- La capa actual agrega administracion interna minima de usuarios sin depender de bootstrap local para todos los roles de prueba.

## Entregables actuales
- `ApplicationUser` con `RoleCode`
- Migraciones `Track2SecurityAuthFoundation` y `Track2SecurityRoleAuthorization`
- `/api/auth/login`
- `/api/auth/session`
- Roles base `ADMIN`, `OPERATOR`, `READONLY`
- Permisos derivados por rol en claim `fmcpa_permission`
- Politicas por modulo/superficie funcional
- Bootstrap local opcional de `operator` y `readonly`
- Endpoints `ADMIN` `/api/admin/users/*`
- `SecurityStamp` e invalidacion de tokens previos para rol, activacion y reset de password
- Rechazo `401` de tokens previos tras desactivacion, cambio de rol y reset administrativo de password
- Trazabilidad minima de gestion de usuarios en `Bitacora` bajo modulo `SECURITY`
- Hardening minimo de uploads/downloads documentales sin migracion nueva
- Hardening minimo del borde HTTP sin migracion nueva
- Migracion `Track2CredentialHardeningAndAuthAudit` para lockout por usuario
- Hardening minimo de credenciales y auditoria auth sin MFA/CAPTCHA/IdP
- Endpoint `/api/auth/change-password` sin migracion nueva
- Proyecto `FMCPA.Api.AuthorizationRegressionTests` sin migracion nueva
- Inventario `docs/05-post-mvp/security-authorization-surface-inventory.md`
- Manifiesto `docs/05-post-mvp/security-authorization-surface-guardrails.json`
- Guardrail automatico de superficie protegida sin cambios runtime ni migracion
- Middleware de proteccion de origen web sin migracion nueva
- Interceptor Angular enviando `X-FMCPA-Client: FMCPA-Web` para metodos inseguros `/api`
- Endpoints `/api/admin/security/*` sin migracion nueva
- Vista `/admin/security`
- Frontend con ruta admin minima para catalogos, pantalla `/admin/users`, pantalla `/account/password`, acciones visibles restringidas por permisos y limpieza de sesion local ante `401` protegido
- Nota de implementacion:
  - `docs/05-post-mvp/security-track-auth-foundation-implementation-note.md`
  - `docs/05-post-mvp/security-track-role-authorization-implementation-note.md`
  - `docs/05-post-mvp/security-track-user-management-implementation-note.md`
  - `docs/05-post-mvp/security-track-session-invalidation-implementation-note.md`
  - `docs/05-post-mvp/security-track-module-authorization-implementation-note.md`
  - `docs/05-post-mvp/security-track-document-upload-hardening-implementation-note.md`
  - `docs/05-post-mvp/security-track-http-boundary-hardening-implementation-note.md`
  - `docs/05-post-mvp/security-track-credential-hardening-and-auth-audit-implementation-note.md`
  - `docs/05-post-mvp/security-track-self-service-password-change-implementation-note.md`
  - `docs/05-post-mvp/security-track-authorization-regression-coverage-implementation-note.md`
  - `docs/05-post-mvp/security-track-authorization-surface-guardrails-implementation-note.md`
  - `docs/05-post-mvp/security-track-web-origin-protection-implementation-note.md`
  - `docs/05-post-mvp/security-track-admin-security-observability-implementation-note.md`

## Criterios de salida vigentes
- Backend compila.
- Frontend compila.
- Existe login funcional con rol y permisos visibles.
- Un usuario autenticado ya no implica acceso total automatico.
- `ADMIN` puede operar.
- `ADMIN` puede administrar usuarios internos.
- Un rol restringido recibe `403` en endpoints sin permiso de modulo/superficie.
- Un token emitido antes de desactivacion, cambio de rol o reset administrativo de password recibe `401`.
- El frontend refleja minimamente restricciones por permiso.
- El frontend limpia `sessionStorage` y redirige a login ante `401` de un request protegido.
- Uploads documentales invalidos por vacio, extension, content-type o tamano responden `400`.
- Descargas documentales autorizadas responden con headers conservadores y sin rutas fisicas internas.
- Login queda protegido por rate limit minimo y responde `429` al exceder el limite.
- Headers HTTP de seguridad se observan en respuestas relevantes.
- CORS queda controlado y fuera de `Development` falla si conserva origins locales o wildcard.
- JWT fuera de `Development` falla rapido con signing key ausente o insegura.
- Passwords invalidos se rechazan con reglas claras en alta/reset admin y bootstrap local.
- Multiples fallos consecutivos de login disparan lockout temporal por usuario.
- Login durante lockout responde `423` y login/desbloqueo posterior permite recuperar acceso.
- Eventos de autenticacion relevantes quedan en bitacora `SECURITY`.
- Usuario autenticado puede cambiar su propia password validando la password actual.
- Password actual incorrecta o nueva password invalida fallan con mensajes claros.
- Cambio self-service exitoso invalida el token previo y exige relogin.
- La suite de regresion de autorizacion cubre publicos minimos, anonimo `401`, matriz por rol, cierres formales, usuarios admin-only y self-service password.
- Existe inventario explicito de superficie protegida y acceso esperado por rol.
- El guardrail automatico falla ante endpoints publicos fuera del allowlist, endpoints `/api` sin regla canónica o metadata de policy incoherente.
- Mutaciones browser sensibles con origen permitido y header esperado pasan a la capa normal de auth/autorizacion.
- Mutaciones browser sensibles sin header esperado reciben `400`.
- Mutaciones browser sensibles con origen no permitido reciben `403`.
- `ADMIN` puede consultar eventos SECURITY, resumen y usuarios bloqueados desde API/UI.
- `OPERATOR` y `READONLY` reciben `403` en `/api/admin/security/*`.
- No se introduce todavia RBAC ultra fino por endpoint/accion individual, permisos manuales por usuario ni self-service completo.
- No se introduce todavia antivirus, DLP, storage externo ni politica documental completa.
- No se introduce todavia WAF, MFA, CAPTCHA, proveedor externo de identidad ni antifraude avanzado.
- La convencion local y el runbook quedan actualizados.

## Dependencias
- Reutiliza la convencion local, tooling y smoke ya dejados por `Track 1`.

## Estado
- Foundation de autenticacion, extension minima de autorizacion por roles, gestion minima de usuarios internos, invalidacion real de tokens, autorizacion minima por modulo, hardening documental de uploads/downloads, hardening del borde HTTP, hardening de credenciales/auditoria auth, cambio self-service de password, cobertura de regresion de autorizacion, guardrails automaticos de superficie, proteccion minima de origen web y observabilidad operativa ADMIN de seguridad entregados y validados localmente, pendientes de aprobacion formal.

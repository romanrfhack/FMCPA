# Security Track User Management Implementation Note

## Objetivo
- Continuar `Track 2` agregando una gestion minima de usuarios y roles internos para `ADMIN`, sin abrir todavia RBAC fino, self-service de cuenta ni recuperacion avanzada de password.

## Que se implemento

### Backend
- `ApplicationUser` ahora persiste `SecurityStamp` y rota ese valor cuando cambia `roleCode`, `isActive` o se resetea el password.
- Se agrega la migracion `Track2SecurityUserManagementAdmin`.
- El bearer JWT emitido en login ahora incluye `fmcpa_security_stamp`.
- Cada request autenticado valida el `SecurityStamp` y el estado activo del usuario contra base de datos antes de autorizar.
- Se agregan endpoints `ADMIN` para:
  - listar usuarios
  - consultar detalle basico
  - crear usuario
  - cambiar rol base
  - activar/desactivar logicamente
  - resetear password por administrador
- No se expone hash ni datos sensibles.

### Reglas minimas aplicadas
- Solo `ADMIN` puede usar `/api/admin/users/*`.
- `userName` no puede duplicarse.
- No existe borrado fisico en esta etapa.
- No se permite dejar al sistema sin ningun `ADMIN` activo.
- La password inicial o reseteada debe tener al menos 12 caracteres.
- Cambio de rol, activacion/desactivacion y reset de password invalidan tokens previos del usuario afectado; ese usuario debe iniciar sesion de nuevo.

### Trazabilidad minima
- Se registran eventos `SECURITY` en bitacora para:
  - alta de usuario
  - cambio de rol
  - activacion
  - desactivacion
  - reset administrativo de password

### Frontend Angular
- Se agrega la ruta protegida `admin/users` solo para `ADMIN`.
- Se agrega una pantalla minima para:
  - listar usuarios
  - dar de alta
  - cambiar rol
  - activar/desactivar
  - resetear password
- La navegacion principal muestra `Usuarios` solo para `ADMIN`.
- Se agrega una prueba puntual de rutas para verificar que `/admin/users` usa `adminOnlyGuard`.

## Flujo de administracion
1. Iniciar sesion como `ADMIN`.
2. Abrir `/admin/users`.
3. Dar de alta un usuario con `userName`, `displayName`, `roleCode` y password inicial.
4. Cambiar rol o estado segun necesidad.
5. Si se resetea password o cambia rol/estado, pedir al usuario afectado que vuelva a iniciar sesion.

## Que puede hacer `ADMIN`
- Consultar todos los usuarios internos visibles para la app.
- Crear cuentas operativas nuevas sin depender de variables de entorno para cada rol.
- Cambiar entre `ADMIN`, `OPERATOR` y `READONLY`.
- Desactivar logicamente usuarios sin borrarlos.
- Reactivar usuarios.
- Restablecer passwords de forma administrativa.

## Como validarlo localmente
- Configurar al menos `FMCPA_AUTH_BOOTSTRAP_PASSWORD` para el `admin` local.
- Levantar el stack local con `./scripts/local/dev-up.sh`.
- Iniciar sesion como `ADMIN`.
- Crear un usuario de prueba por API o desde `/admin/users`.
- Cambiarle el rol.
- Desactivarlo y comprobar que ya no puede hacer login.
- Reactivarlo y resetear su password.
- Volver a iniciar sesion con la password nueva.
- Verificar `403` de `OPERATOR` y `READONLY` en `/api/admin/users`.
- Consultar `GET /api/bitacora?moduleCode=SECURITY`.

## Validacion runtime cerrada
- Fecha de cierre runtime: `2026-04-23`.
- Corrida realizada sobre un stack aislado con:
  - SQL Server efimero en Docker del host: `fmcpa-sql-usermgmt-14335`
  - Puerto SQL: `14335`
  - Base: `FMCPA_UserMgmtValidation_20260423`
  - Backend: `http://127.0.0.1:5092`
- Resultado real confirmado:
  - `ADMIN` pudo iniciar sesion, listar usuarios, consultar detalle, crear usuario, cambiar rol, desactivar, reactivar y resetear password.
  - El usuario de prueba desactivado recibio `401` al intentar login.
  - El mismo usuario pudo iniciar sesion con la password reseteada y, ya como `OPERATOR`, recibio `403` en `/api/admin/users`.
  - `READONLY` tambien recibio `403` en `/api/admin/users`.
  - `npm run build` y `vitest` de guards/rutas Angular pasaron sin cambios de codigo funcional.
- Observacion operativa:
  - En la corrida aislada se observo un timeout transitorio en el primer arranque del backend inmediatamente despues de migrar la base; al reintentar una vez sobre la misma base, el bootstrap termino correctamente y la validacion completa paso sin requerir fixes de codigo.

## Decisiones tomadas
- Se reutiliza `ApplicationUser` en vez de introducir una plataforma completa de identidad.
- Se usa `SecurityStamp` para invalidacion inmediata de tokens sensibles sin abandonar JWT bearer.
- La administracion minima se mantiene acotada a `ADMIN`.
- La desactivacion es logica; no hay delete fisico.

## Que queda fuera
- RBAC fino por modulo o accion.
- Self-service de perfil o cambio autonomo de password.
- Recuperacion de password por correo o invitaciones.
- Revocacion avanzada, refresh token o sesiones complejas.
- Produccion, CI/CD y refactors amplios del MVP.

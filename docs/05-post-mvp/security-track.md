# Security Track

## Objetivo
- Endurecer el acceso transversal a la plataforma post-MVP con una base minima y extensible de autenticacion y autorizacion.

## Alcance implementado actualmente
- Login backend con `JWT bearer`
- Validacion de token y endpoint de sesion actual
- Usuario bootstrap local controlado por configuracion
- Roles base `ADMIN`, `OPERATOR`, `READONLY`
- Claim de rol en JWT y `RoleCode` persistido en `ApplicationUser`
- `SecurityStamp` persistido para invalidar tokens previos en cambios sensibles de usuario
- Politicas backend `read`, `write` y `admin`
- Proteccion minima de endpoints operativos, cierres formales y catalogos administrativos
- Endpoints `ADMIN` para gestion minima de usuarios internos
- Login Angular, guard, interceptor, logout, pantalla `/admin/users` y reflejo minimo del rol en shell y acciones visibles
- Smoke local autenticado y smoke MVP autenticado

## Mapeo minimo actual por rol
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
- Lecturas globales y consultas operativas: `READONLY`, `OPERATOR`, `ADMIN`
- Escrituras funcionales normales: `OPERATOR`, `ADMIN`
- Cierres formales y altas de catalogos sensibles: `ADMIN`
- Administracion de usuarios internos: `ADMIN`

## Alcance diferido explicitamente
- RBAC completo por modulo o accion
- Matriz fina de permisos por endpoint
- Gestion completa de usuarios
- Self-service, recuperacion o cambio autonomo de password
- Refresh tokens, revocacion central o sesiones avanzadas
- Integraciones externas de identidad
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
- Politicas `read`, `write`, `admin`
- Bootstrap local opcional de `operator` y `readonly`
- Endpoints `ADMIN` `/api/admin/users/*`
- `SecurityStamp` e invalidacion de tokens previos para rol, activacion y reset de password
- Trazabilidad minima de gestion de usuarios en `Bitacora` bajo modulo `SECURITY`
- Frontend con ruta admin minima para catalogos, pantalla `/admin/users` y acciones visibles restringidas por rol
- Nota de implementacion:
  - `docs/05-post-mvp/security-track-auth-foundation-implementation-note.md`
  - `docs/05-post-mvp/security-track-role-authorization-implementation-note.md`
  - `docs/05-post-mvp/security-track-user-management-implementation-note.md`

## Criterios de salida vigentes
- Backend compila.
- Frontend compila.
- Existe login funcional con rol visible.
- Un usuario autenticado ya no implica acceso total automatico.
- `ADMIN` puede operar.
- `ADMIN` puede administrar usuarios internos.
- Un rol restringido recibe `403` en endpoints no permitidos.
- El frontend refleja minimamente restricciones por rol.
- No se introduce todavia RBAC fino por modulo o accion ni self-service completo.
- La convencion local y el runbook quedan actualizados.

## Dependencias
- Reutiliza la convencion local, tooling y smoke ya dejados por `Track 1`.

## Estado
- Foundation de autenticacion, extension minima de autorizacion por roles y gestion minima de usuarios internos entregadas y validadas localmente, pendientes de aprobacion formal.

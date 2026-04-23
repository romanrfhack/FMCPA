# Current Phase

## Fase actual
**Track 2 post-MVP: Seguridad transversal, autenticacion, autorizacion base y gestion minima de usuarios internos**

## Estado actual
- Fecha de inicio documentada: 2026-04-21
- Estado de la fase: base minima de autenticacion, autorizacion por roles base y gestion minima de usuarios internos implementadas y validadas localmente, pendiente de aprobacion formal
- Estado del MVP: **Cerrado con reservas**
- Estado del track anterior: `Track 1` queda entregado y sigue pendiente de aprobacion formal
- Enfoque: exigir autenticacion para operar la app, introducir una diferenciacion pragmatica entre lectura, operacion y administracion, y dejar una gestion interna minima de usuarios sin abrir todavia RBAC fino ni self-service

## Nota operativa
- El MVP permanece cerrado con reservas; `Track 2` no reabre alcance funcional ni agrega modulos de negocio nuevos.
- `Track 2` ya cuenta con login JWT, sesion actual, bootstrap local controlado, roles base `ADMIN`, `OPERATOR` y `READONLY`, y una administracion minima de usuarios internos solo para `ADMIN`.
- `ApplicationUser` incorpora `RoleCode` persistido y el token emitido por `/api/auth/login` incluye el rol como claim estable.
- `ApplicationUser` incorpora tambien `SecurityStamp`; cambios de rol, activacion/desactivacion y reset administrativo de password invalidan tokens previos del usuario afectado.
- La API aplica tres politicas fijas:
  - `read`: `READONLY`, `OPERATOR`, `ADMIN`
  - `write`: `OPERATOR`, `ADMIN`
  - `admin`: `ADMIN`
- Los cierres formales y altas administrativas de catalogos quedan restringidos a `ADMIN`; las escrituras funcionales normales quedan para `OPERATOR` y `ADMIN`.
- El bootstrap local sigue limitado a `Development`, sin versionar secretos reales; ahora admite tambien usuarios opcionales `operator` y `readonly` por configuracion local.
- El frontend conserva el rol en `sessionStorage`, protege rutas administrativas de catalogos y deshabilita acciones visibles de escritura o cierre cuando el rol no las puede ejecutar.
- El frontend incorpora ademas una vista minima `/admin/users` protegida para `ADMIN`, con listado, alta, cambio de rol, activacion logica y reset de password.
- No se implementa todavia autorizacion fina por modulo/accion, self-service completo, recuperacion avanzada de password, refresh token ni proveedor externo de identidad.

## Objetivos de la fase
- Mantener una base minima y segura de autenticacion para backend y frontend.
- Evitar que cualquier usuario autenticado tenga acceso total automatico.
- Diferenciar de forma simple lectura, escritura funcional y administracion sensible.
- Permitir que `ADMIN` gestione usuarios internos sin depender de bootstrap por variables para todos los roles.
- Dejar una convencion local clara para probar al menos `ADMIN`, `OPERATOR` y `READONLY`.
- Validar `401` sin token, `403` por rol insuficiente y acceso permitido con rol valido.
- Documentar decisiones, riesgos, convencion local y limites del paso.

## Entregables esperados de esta fase
- `ApplicationUser` con `RoleCode` y migracion `Track2SecurityRoleAuthorization`
- Roles base `ADMIN`, `OPERATOR`, `READONLY`
- JWT con claim de rol y `/api/auth/session` con rol visible
- Politicas backend `read`, `write` y `admin`
- Proteccion minima de endpoints por rol
- Endpoints `ADMIN` para listar, consultar, crear, cambiar rol, activar/desactivar y resetear password de usuarios
- Invalidacion de tokens por `SecurityStamp` en cambios sensibles de usuario
- Bootstrap local opcional de usuarios `operator` y `readonly`, ya no obligatorio para validar roles gestionados
- Frontend con rol en sesion, guard admin minimo, pantalla de usuarios y acciones visibles restringidas
- `smoke.sh` actualizado con validacion opcional de `READONLY`
- Nota de implementacion del track bajo `docs/05-post-mvp`

## Criterios de salida de la fase
- Backend compila.
- Frontend compila.
- Existe login funcional con rol visible.
- Un usuario autenticado ya no implica acceso total automatico.
- `ADMIN` puede operar y acceder a endpoints administrativos actuales.
- `ADMIN` puede listar y administrar usuarios internos.
- Un rol restringido recibe `403` en operaciones no permitidas.
- El frontend refleja minimamente las restricciones por rol.
- No existe todavia RBAC fino por modulo o accion ni self-service completo.
- La documentacion de etapa, riesgos, decisiones y runbook local queda actualizada.

## Siguiente decision esperada
- Revisar y aceptar o rechazar la extension de `Track 2` con gestion minima de usuarios internos administrada por `ADMIN`.
- Definir si el siguiente paso profundiza permisos finos por modulo/accion o si se estabiliza primero esta capa base de seguridad y administracion interna.

## Referencias
- [Security Track](../05-post-mvp/security-track.md)
- [Security Track Auth Foundation Implementation Note](../05-post-mvp/security-track-auth-foundation-implementation-note.md)
- [Security Track Role Authorization Implementation Note](../05-post-mvp/security-track-role-authorization-implementation-note.md)
- [Security Track User Management Implementation Note](../05-post-mvp/security-track-user-management-implementation-note.md)
- [MVP Local Runbook](../03-release/mvp-local-runbook.md)
- [Backlog](./backlog.md)
- [Historial de aceptacion](./acceptance-history.md)

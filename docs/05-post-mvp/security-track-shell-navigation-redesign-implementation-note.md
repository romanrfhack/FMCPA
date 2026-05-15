# Security Track Shell Navigation Redesign Implementation Note

## Alcance
- Se rediseña solo la navegacion principal del shell autenticado en Angular.
- No se modifica backend, `AuthService`, guards, rutas protegidas, permisos, contratos API, migraciones, CI/CD ni produccion.
- La navegacion conserva los mismos enlaces y el mismo filtrado por permisos de sesion.

## Cambios Implementados
- La navegacion deja de mostrarse como lista tecnica plana y se organiza visualmente por grupos.
- Los grupos se renderizan solo si contienen al menos un enlace visible despues de aplicar permisos.
- Los labels visibles se ajustan a lenguaje operativo e institucional:
  - `Operaciones` pasa a `Centro operativo`.
  - `Historico`, `Bitacora`, `Revision documental`, `Tipos de comision`, `Estatus por modulo` y `Federacion` se muestran con acentos.
- El estado activo se refuerza con fondo verde petroleo, texto claro y una marca interna teal, no solo color.
- Hover y foco visible usan el mismo tratamiento discreto del shell institucional.
- Los links son mas compactos y el contenedor mantiene fondo blanco/translucido, crema, verde petroleo, teal y sombra suave.

## Agrupacion Final

### Inicio
- `Dashboard` -> `/dashboard`
- `Centro operativo` -> `/operations`

### Operacion
- `Mercados` -> `/markets`
- `Donatarias` -> `/donatarias`
- `Financieras` -> `/financials`
- `Federación` -> `/federation`

### Control
- `Documentos` -> `/documents`
- `Bandeja documental` -> `/documents/work-queue`
- `Revisión documental` -> `/documents/review`
- `Comisiones` -> `/commissions`
- `Histórico` -> `/history`
- `Bitácora` -> `/bitacora`

### Administracion
- `Contactos` -> `/contacts`
- `Usuarios` -> `/admin/users`
- `Seguridad` -> `/admin/security`
- `Tipos de comisión` -> `/catalogs/commission-types`
- `Tipos de evidencia` -> `/catalogs/evidence-types`
- `Estatus por módulo` -> `/catalogs/module-statuses`

## Conservacion Del Filtrado Por Permisos
- Cada item conserva su `requiredPermission` o `requiredAnyPermissions` existente.
- El metodo `hasNavigationAccess()` sigue consultando `AuthService.hasPermission()`.
- La nueva computed `visibleNavigationGroups()` filtra primero los items por permiso y despues oculta grupos vacios.
- No se modifican `app.routes.ts`, `permissionGuard`, `authChildGuard`, claims, roles ni contratos de sesion.

## Accesibilidad Y Responsive
- La navegacion conserva `aria-label="Navegación principal"`.
- Cada grupo se renderiza como `section` con `aria-labelledby` hacia su encabezado visible.
- Los links mantienen foco visible por teclado.
- En desktop la navegacion queda como sidebar compacto.
- En tablet la navegacion se distribuye por grupos en columnas flexibles.
- En movil la navegacion vuelve a una columna para evitar overflow horizontal y mantener targets usables.

## Validacion Local
- `npm run build`: exitoso. Se conserva la advertencia preexistente de presupuesto CSS en `donatarias-page.component.ts`.
- `npm test -- --watch=false`: exitoso, `25/25`.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: exitoso, `0` warnings, `0` errores.
- `git diff -- src/backend`: sin cambios.
- `git diff --check`: sin observaciones.
- Playwright/Chromium headless con login mockeado:
  - login exitoso hacia `/dashboard`.
  - usuario ADMIN con todos los grupos visibles.
  - estado activo verificado en `Dashboard`, `Centro operativo`, `Donatarias`, `Documentos` y `Usuarios`.
  - navegacion validada a `/dashboard`, `/operations`, `/donatarias`, `/documents` y `/admin/users`.
  - 390px, 768px y 1366px sin overflow horizontal.

## Fuera De Alcance
- Cambios backend, rutas, guards, permisos o contratos API.
- Cambios en `AuthService`.
- Pantallas nuevas, modulos nuevos o rediseño interno de paginas.
- Librerias externas, iconografia nueva o assets nuevos.
- CI/CD, produccion, migraciones o cambios de base de datos.

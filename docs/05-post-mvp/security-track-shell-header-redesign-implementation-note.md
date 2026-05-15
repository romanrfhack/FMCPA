# Security Track Shell Header Redesign Implementation Note

## Alcance
- Se rediseña solo el shell autenticado del frontend Angular.
- No se modifica backend, `AuthService`, guards, rutas protegidas, permisos, contratos API, migraciones, CI/CD ni produccion.
- La navegacion existente del shell se conserva con el mismo filtrado por permisos.

## Cambios Implementados
- Se reemplaza el encabezado tecnico por una marca institucional:
  - logo FMCPA
  - `FMCPA Platform`
  - subtitulo corto `Gestión operativa`
- Se integra el logo local reutilizado por login:
  - archivo: `src/frontend/public/assets/brand/logo-fmcpa.webp`
  - consumo: `/assets/brand/logo-fmcpa.webp`
  - `alt="FMCPA"`
- Se elimina la tarjeta grande de sesion y se agrega un menu compacto de usuario.
- El header muestra permanentemente solo:
  - avatar con iniciales derivadas del nombre visible
  - nombre visible del usuario
  - indicador de desplegable
- El nombre visible usa `displayName` cuando existe y cae a `userName` solo como respaldo.

## Texto Tecnico Retirado
- `Track 2 Seguridad · Autenticación y autorización mínima por módulo`
- `Angular 21`
- `.NET 10`
- `JWT Local`
- El bloque largo sobre MVP operativo, JWT local, sesion autenticada, rutas protegidas y separacion minima por permisos.
- La exposicion permanente de rol en formato `userName · rol`, por ejemplo `admin · Admin`.
- Los botones permanentes `Cambiar contraseña` y `Cerrar sesión`.

## Menu De Usuario
- El boton de usuario abre/cierra el dropdown por click, no por hover.
- El boton expone `aria-haspopup="menu"` y `aria-expanded`.
- El dropdown contiene:
  - `Cambiar contraseña`, enlazando a `/account/password`
  - `Cerrar sesión`, como boton real que ejecuta `AuthService.logout()`
- Al seleccionar una accion el menu se cierra.
- El menu tambien se cierra con click fuera y con Escape.
- No se agrega opcion `Mi cuenta` porque no existe una pantalla de perfil real.

## Conservacion Funcional
- Logout sigue usando el metodo existente `AuthService.logout()`.
- Cambio de contraseña sigue usando el `routerLink` existente a `/account/password`.
- La navegacion principal sigue usando `visibleNavigation()` y `hasPermission()` sin cambios funcionales.
- No se modifica la sesion, el token, los permisos ni la autenticacion backend.

## Estilo Responsive
- Header con fondo crema/blanco translucido, sombra suave, verde petroleo y teal.
- Logo con `object-fit: contain` para evitar deformacion.
- En movil el menu de usuario ocupa el ancho disponible y el dropdown queda dentro de pantalla.
- Se evita overflow horizontal en 390px, 768px y 1366px.

## Validacion Local
- `npm run build`: exitoso. Se conserva advertencia preexistente de presupuesto CSS en `donatarias-page.component.ts`.
- `npm test -- --watch=false`: exitoso, `25/25`.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: exitoso, `0` warnings, `0` errores.
- `git diff -- src/backend`: sin cambios.
- Playwright/Chromium headless con sesion mockeada en `sessionStorage`:
  - 390px, 768px y 1366px sin overflow horizontal.
  - Logo visible.
  - No aparece `Track 2 Seguridad`.
  - No aparecen `Angular 21`, `.NET 10` ni `JWT Local`.
  - No aparece el texto largo de MVP/JWT.
  - Nombre visible del usuario presente.
  - Rol y usuario tecnico no visibles en el header.
  - Dropdown abre por click.
  - `Cambiar contraseña` navega a `/account/password`.
  - `Cerrar sesión` navega a `/login`.

## Fuera De Alcance
- Rediseño de modulos internos.
- Pantalla real de perfil o `Mi cuenta`.
- Cambios a backend, auth backend, contratos API, permisos, guards, rutas protegidas, migraciones, CI/CD o produccion.
- Librerias externas o nuevos assets.

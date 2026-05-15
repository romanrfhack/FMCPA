# Security Track Login Redesign Implementation Note

## Alcance
- Se rediseña solo la pantalla publica `/login` del frontend Angular.
- No se modifica backend, endpoints, contratos API, guards, rutas, permisos, migraciones, token handling ni CI/CD.
- El flujo de submit sigue usando `AuthService.login()` y conserva el manejo actual de errores mediante `getApiErrorMessage`.

## Cambios Implementados
- Se integra el logotipo institucional en `/login` desde `src/frontend/public/assets/brand/logo-fmcpa.webp`.
- Se reemplaza el copy tecnico por contenido de usuario final:
  - `FMCPA Platform`
  - `Acceso al sistema`
  - `Ingresa tus credenciales para continuar`
  - campos `Usuario` y `Contraseña`
  - boton `Iniciar sesión`
- Se retira el valor inicial visible `admin` del campo usuario para no sugerir credenciales.
- Se mejora accesibilidad minima con labels asociados por `for`/`id`, `alt="FMCPA"`, foco visible y `role="alert"` para errores.
- Se ajusta el layout a una tarjeta institucional responsive con fondo crema, tarjeta blanca, acento rojo discreto y boton verde petroleo.

## Texto Tecnico Retirado
- `Track 2 Seguridad`
- `Acceso local mínimo`
- Referencias visibles a `JWT`
- Bloque `Convención local`
- Usuarios sugeridos `admin`, `operator`, `readonly`
- Variables de entorno de passwords/bootstrap locales

## Ruta Del Logo
- `src/frontend/public/assets/brand/logo-fmcpa.webp`
- Angular ya publica assets desde `src/frontend/public`, por lo que la UI lo consume como `/assets/brand/logo-fmcpa.webp`.
- No se convierte el archivo a otro formato y no se descargan assets externos.

## Validacion Ejecutada
- `npm run build`: exitoso. Se conserva una advertencia preexistente de presupuesto CSS en `donatarias-page.component.ts`.
- `npm test -- --watch=false`: exitoso, `24/24`.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: exitoso, `0` warnings, `0` errores.
- `git diff -- src/backend`: sin cambios.
- `git diff --check`: sin observaciones.
- Validacion headless con Playwright/Chromium sobre `/login`:
  - 390px, 768px y 1366px sin overflow horizontal.
  - Logo visible y asset cargado.
  - Campos `Usuario`/`Contraseña` y boton visibles.
  - No aparecen `Track 2 Seguridad`, `JWT`, `Convención local`, `admin`, `operator` ni `readonly`.
  - Login fallido muestra mensaje de error.
  - Login exitoso validado con API mockeada y navegacion a `/dashboard`.

## Fuera De Alcance
- Login real contra backend vivo con credenciales locales, porque la validacion visual se ejecuto con API mockeada.
- Rediseño del shell autenticado.
- Cambios a autenticacion backend, guards, rutas, permisos, contratos API, migraciones, CI/CD o produccion.

# Nota de validacion visual global del rediseño FMCPA

## Alcance

Validacion integral posterior al rediseño de login, shell/header, navegacion principal, Donatarias, Documents, Admin Users, Financials, Federation, Markets y limpieza CSS/budget.

No se implementaron funcionalidades nuevas. No se modifico backend, contratos API, permisos, rutas protegidas, schema ni migraciones.

## Pantallas validadas

- `/login`
- Shell autenticado y navegacion principal
- `/dashboard`
- `/operations`
- `/markets`
- `/donatarias`
- `/financials`
- `/federation`
- `/documents`
- `/documents/work-queue`
- `/documents/review`
- `/admin/users`
- `/admin/security`
- `/contacts`
- `/commissions`
- `/history`
- `/bitacora`
- `/account/password`

## Viewports validados

- `390px`
- `768px`
- `1366px`

## Resultados de validacion visual

- Sin overflow horizontal global en las rutas validadas.
- Header autenticado consistente, logo visible y menu de usuario funcional.
- Navegacion principal agrupada visible y usable con usuario `ADMIN`.
- Login real con `ADMIN` exitoso; logout regresa a `/login`.
- Cambio de contraseña sigue disponible desde el menu de usuario y en `/account/password`.
- Tabs principales usables en `/markets`, `/donatarias`, `/financials`, `/federation` y `/documents`.
- Filtros principales se mantienen dentro del layout en las rutas con filtros.
- Modales principales abren y cierran en `/markets`, `/donatarias`, `/financials`, `/federation` y `/admin/users`.
- No se detectaron errores de consola en la corrida Playwright.
- Paleta y patrones visuales se sostienen con las utilidades `fmcpa-*` y los rediseños recientes.

La validacion Playwright uso stack local aislado:

- API: `http://127.0.0.1:5096`
- Web: `http://127.0.0.1:4210`
- Base: `FMCPA_UiGlobalValidation_20260516`
- Usuario: `admin` con rol `ADMIN`
- Dato temporal: `Mercado Validación UI Global`, usado solo para activar tabs de Markets en la base aislada.

## Build y tests

- `npm run build`: exitoso, sin warnings de CSS budget.
- `npm test -- --watch=false`: exitoso, `29/29`.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: exitoso, `0 warnings`, `0 errors`.
- Playwright/Chromium headless: exitoso en `390px`, `768px`, `1366px`.
- `git diff -- src/backend`: sin cambios.
- `git diff --check`: sin observaciones.

## Bugs visuales detectados y corregidos

- `/bitacora` en `390px` generaba overflow horizontal global por ancho efectivo de cards antiguas. Se corrigio con `min-width: 0`, `max-width: 100%` y ajuste responsive puntual de padding/actions.
- Varias pantallas transversales conservaban kickers/copy tecnico visible (`STAGE`, `TRACK`, `MVP`, `SECURITY`, `DOCUMENTS`, `HIGH/MEDIUM/LOW`, `summary`, referencia a token). Se corrigio como bug visual/copy minimo en frontend para:
  - `/dashboard`
  - `/operations`
  - `/documents/review`
  - `/admin/security`
  - `/contacts`
  - `/commissions`
  - `/history`
  - `/bitacora`
  - `/account/password`

## Pendientes visuales restantes

- `/contacts` aun conserva alta inline; queda como siguiente mejora de densidad/interaccion.
- Catalogos simples (`/catalogs/commission-types`, `/catalogs/evidence-types`, `/catalogs/module-statuses`) quedan pendientes de homologacion visual completa.
- `/dashboard`, `/operations`, `/commissions`, `/history` y `/bitacora` ya no muestran copy tecnico principal, pero aun pueden recibir pulido de densidad/jerarquia visual en una fase posterior.
- Algunos codigos tecnicos siguen existiendo como valores internos o dentro de helpers; no se detectaron como texto visible principal en la validacion.

## Estado final

Aceptada para cierre de esta etapa visual, con pendientes visuales menores ya registrados en el backlog post-MVP.

## Actualizacion documental posterior

- Guia rapida y HTML final actualizados para reflejar esta etapa visual aceptada.
- Evidencia: `docs/05-post-mvp/ui-global-redesign-user-guide-update-note.md`.

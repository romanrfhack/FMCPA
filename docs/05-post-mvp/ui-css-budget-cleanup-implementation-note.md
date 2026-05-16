# UI CSS budget cleanup implementation note

Fecha: 2026-05-16

## Alcance

Limpieza CSS frontend acotada para reducir warnings de budget visual en componentes rediseñados:

- `donatarias-page.component.ts`
- `documents-page.component.ts`
- `documents-work-queue-page.component.ts`
- `financials-page.component.ts`
- `federation-page.component.ts`
- `markets-page.component.ts`
- `styles.css`

No se modifico backend, endpoints, contratos API, permisos, rutas, servicios, DTOs, migraciones, CI/CD ni logica funcional.

## Patrones movidos a global

Se agregaron utilidades/alias globales con prefijo `fmcpa-` en `src/frontend/src/styles.css`:

- `.fmcpa-modal-backdrop` / `.fmcpa-modal`
- `.fmcpa-tabs` / `.fmcpa-tab`
- `.fmcpa-card`
- `.fmcpa-kpi-grid` / `.fmcpa-kpi-card`
- `.fmcpa-form-grid`
- `.fmcpa-action-bar`
- `.fmcpa-badge`
- `.fmcpa-table-wrap`
- `.fmcpa-empty-state`
- `.fmcpa-filter-bar`
- `.fmcpa-document-panel` / `.fmcpa-document-badge`

Para minimizar cambios de templates, las utilidades se publicaron tambien como alias globales scoped por selector de componente (`app-donatarias-page`, `app-financials-page`, `app-federation-page`, `app-markets-page`, `app-documents-page`, `app-documents-work-queue-page`) sobre clases locales existentes como `.modal`, `.tab-nav`, `.summary-grid`, `.form-grid`, `.status-pill`, `.filters-panel`, `.panel` y `.badge`.

## Componentes reducidos

- Financials: modales, tabs, cards, formularios, botones, alerts, badges y KPI base se removieron del CSS local.
- Federation: modales, tabs, cards, formularios, botones, alerts, badges y KPI base se removieron del CSS local.
- Markets: modales, tabs, cards, formularios, botones, alerts, badges y KPI base se removieron del CSS local.
- Donatarias: tabs, modales, cards, tablas de aplicaciones, signal cards, badges, filas, alerts y base de formularios se consolidaron en global.
- Documents: paneles, filtros, botones y badges documentales se consolidaron en global; se corrigio overflow movil del drawer cerrado.
- Documents work queue: paneles, filtros, botones y badges se consolidaron en global.

## Warnings antes/despues

Baseline observado con `npm run build`:

| Componente | Antes |
| --- | ---: |
| Donatarias | 8.00 kB, excedia por 4.00 kB |
| Documents | 4.72 kB, excedia por 719 bytes |
| Financials | 5.24 kB, excedia por 1.24 kB |
| Federation | 5.04 kB, excedia por 1.04 kB |
| Markets | 5.29 kB, excedia por 1.28 kB |

Resultado despues:

- `npm run build` sin warnings de CSS budget.
- Ningun componente objetivo queda excediendo el budget de 4.00 kB.

## Validacion responsive

Validado con `ng serve` local y runner Playwright/Chromium headless con sesion frontend mockeada solo para atravesar guards:

- Rutas: `/donatarias`, `/documents`, `/documents/work-queue`, `/financials`, `/federation`, `/markets`.
- Viewports: `390px`, `768px`, `1366px`.
- Resultado: 18 combinaciones ruta/viewport sin overflow horizontal global.
- La validacion hizo interaccion basica con tabs y con el primer disparador de modal visible cuando existia.

Tambien se ejecuto:

- `npm run build`
- `npm test -- --watch=false`
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`
- `git diff -- src/backend`
- `git diff --check`

## Riesgos

- Las reglas globales scoped reducen duplicacion sin cambiar comportamiento, pero aumentan la importancia de mantener el prefijo/alcance `fmcpa-` y selectores `app-*` para evitar colisiones futuras.
- Algunos patrones de Donatarias siguen teniendo reglas globales especificas porque la pantalla conserva tablas y reporte imprimible con necesidades propias.
- La validacion visual fue headless con API no levantada; confirma layout, guards frontend, tabs/modales visibles y overflow, no datos reales de negocio.

## Fuera de alcance

- Backend, endpoints, DTOs, servicios, permisos, rutas, migraciones, CI/CD y produccion.
- Redisenar pantallas desde cero.
- Agregar librerias externas.
- Cambiar contratos, payloads o reglas de negocio.
- Normalizar todos los componentes historicos fuera de las pantallas objetivo.

# Nota de actualización de guía por rediseño visual global FMCPA

Fecha: 2026-05-16

## Alcance

Actualización documental de la guía rápida de usuario y sus fuentes para reflejar el rediseño visual global ya aceptado.

No se modificó código funcional. No se modificó `src/backend` ni `src/frontend`.

## Documentos actualizados

- `docs/user-guide/drafts/system-visual-reference.md`
- `docs/user-guide/drafts/feature-status-matrix.md`
- `docs/user-guide/drafts/user-guide-outline.md`
- `docs/user-guide/drafts/user-guide-source.md`
- `docs/user-guide/drafts/user-guide-final-source.md`
- `docs/user-guide/drafts/user-guide-change-log.md`
- `docs/user-guide/user-guide.html`
- `docs/user-guide/user-guide-final.html`
- `docs/00-governance/session-log.md`
- `docs/00-governance/acceptance-history.md`
- `docs/04-codex/prompts-log.md`

## Cambios principales documentados

- Login institucional con logo FMCPA.
- Header autenticado con logo FMCPA, nombre de plataforma y menú de usuario compacto.
- Cambio de contraseña y cierre de sesión desde el menú de usuario.
- Navegación agrupada por `Inicio`, `Operación`, `Control` y `Administración`, filtrada por permisos.
- Donatarias con tabs, KPIs, reporte de transparencia, vista imprimible operativa y modales contextuales.
- Mercados, Financieras y Federación con tabs, resúmenes/fichas operativas, listados compactos y modales contextuales.
- Documents como catálogo compacto con labels operativos y work queue compacta.
- Admin Users con alta en diálogo y acciones agrupadas en `Gestionar`.
- Limpieza CSS/budget con utilidades `fmcpa-*` y build sin warnings de CSS budget.

## Alcance y reservas visibles

- La vista imprimible de Donatarias es operativa y no sustituye reporte legal, fiscal o contable oficial.
- CSV y exportaciones ligeras se describen como apoyos operativos, no como reportes oficiales.
- PDF oficial, folio, firma, versionamiento, validación legal/fiscal/contable, seguridad avanzada, reportes formales, Contacts UX y Catálogos UX quedan como pendientes de fase posterior.

## Demo Donatarias

No existe `docs/demo/donatarias-donor-demo` en este árbol al momento de la actualización. No se creó un paquete demo nuevo para evitar inventar assets, pantallas o screenshots.

## Validación ejecutada

- `git diff --check -- docs/user-guide docs/05-post-mvp/ui-global-redesign-user-guide-update-note.md`
- `git diff -- src/backend`: sin cambios.
- `git diff -- src/frontend`: sin cambios.
- `docs/user-guide/user-guide.html` y `docs/user-guide/user-guide-final.html`: contenido sincronizado.
- Validación Playwright de `docs/user-guide/user-guide-final.html` en `390px`, `768px` y `1366px`: sin overflow horizontal global.
- Validación Playwright de CSS print: navegación interna oculta y contenido principal en modo impresión.

# UI Markets Density Phase 1 Implementation Note

## Que se implemento

- `/markets` se reorganizo como pantalla de consulta primero y captura contextual despues.
- Se agrego resumen ejecutivo del mercado seleccionado con estatus, alcaldia, secretario general, locatarios, cedulas con alerta e incidencias abiertas/cerradas calculadas en frontend con datos ya cargados.
- Se agregaron tabs locales: `Resumen`, `Locatarios`, `Incidencias / mejoras` y `Documentos / cédulas`.
- Se compactaron filtros de mercado por estatus y alertas activas, listado de mercados, alertas activas y filas de locatarios/incidencias.
- La zona de cédulas quedo separada en `Documentos / cédulas`, mostrando cédula actual, vigencia, estado, descarga y el panel documental contextual existente.

## Formularios movidos a modal

- `Registrar mercado`.
- `Registrar locatario`.
- `Registrar incidencia / mejora`.

Los tres modales usan `role="dialog"`, `aria-modal="true"`, titulo claro, cierre/cancelacion y cierre por overlay/Escape siguiendo el patron visual de Donatarias, Financials y Federation.

## Formularios inline

No quedaron formularios principales inline en `/markets`.

El reemplazo/carga contextual de cédula para locatarios existentes queda dentro del `RelatedDocumentsPanelComponent`, porque ya es el flujo transversal vigente de remediacion documental y conviene conservarlo donde muestra requisito, estado y documentos relacionados.

## Servicios y payloads

- Se conservaron `MarketsService`, `SharedCatalogsService`, DTOs, `FormGroup`, validaciones y submit handlers existentes.
- `POST /api/markets`, `POST /api/markets/{marketId}/tenants`, `POST /api/markets/{marketId}/issues` y descargas de cédula siguen usando los mismos payloads/endpoints.
- No se modifico backend, rutas, permisos, schema, migraciones ni contratos API.
- La UI solo abre las capturas de locatario/incidencia cuando hay mercado seleccionado, el usuario tiene `MARKETS_WRITE` y el mercado no esta en estado terminal segun `statusIsClosed`.

## Labels y copy

- Se retiro el kicker `STAGE-03` visible en la cabecera.
- Se reforzo lenguaje operativo: `Mercado`, `Locatario`, `Cédula`, `Incidencia / mejora`, `Alcaldía`, `Secretario general`, `Estatus` y `Alerta`.
- Se corrigieron labels visibles de `Alcaldía`, `Descripción` y `Satisfacción final` en formularios.

## Validacion responsive

- `npm run build`: exitoso.
- `npm test -- --watch=false`: exitoso, `8` archivos y `29` tests.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: exitoso, `0 Warning(s)`, `0 Error(s)`.
- Playwright mockeado con login `ADMIN`: valido `/markets`, filtros/listado, abrir/cerrar modal de mercado, crear mercado, abrir/cerrar modal de locatario, crear locatario con cédula, descargar cédula, abrir/cerrar modal de incidencia, crear incidencia, tabs y responsive `390px`, `768px`, `1366px` sin overflow horizontal global.
- `git diff -- src/backend`: sin cambios.
- `git diff --check`: sin observaciones.

## Presupuesto CSS

El build reporta warning de budget CSS para `markets-page.component.ts`: presupuesto `4.00 kB`, total `5.29 kB`, exceso `1.28 kB`.

Tambien se mantienen warnings existentes en Federation, Documents, Donatarias y Financials. No bloquean compilacion, pero Markets queda como candidato a extraccion futura de estilos compartidos si se decide reducir presupuesto CSS.

## Riesgos pendientes

- Usuarios habituados al flujo inline pueden necesitar ubicar las capturas por botones contextuales y tabs.
- Los KPIs de resumen son calculos frontend sobre el detalle/listas cargadas, no agregados historicos ni BI.
- La remediacion documental de cédulas existentes depende del panel transversal actual; no se abrio modelo documental nuevo.

## Fuera de alcance

- Backend, contratos API, endpoints, permisos, rutas, schema, migraciones, CI/CD y produccion.
- Wizard complejo, workflow, edicion avanzada de mercados/locatarios/incidencias, modelo documental nuevo, BI/reporting y cambios de autorizacion.

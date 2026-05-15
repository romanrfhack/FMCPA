# Financials UX Etapa A - Nota de implementacion

Fecha: 2026-05-15

## Alcance implementado

Se reorganizo `/financials` como mejora visual acotada de frontend para priorizar contexto operativo, oficio vigente, cadena de renovacion, creditos, comisiones y acciones contextuales sin modificar backend, contratos API, permisos, rutas, schema ni logica funcional.

## Que se implemento

- Encabezado operativo con acciones principales: `Buscar vigente`, `Registrar oficio` y `Actualizar`.
- Navegacion interna por tabs:
  - `Resumen / contexto`
  - `Oficios / autorizaciones`
  - `Creditos`
  - `Comisiones`
  - `Renovaciones / cadena`
- Ficha operativa financiera destacada visualmente como primer bloque del sidebar, con resumen de vigente/antecedente, accion sugerida, cadena, creditos y comisiones.
- Filtros y listado de oficios compactados en el tab `Oficios / autorizaciones`.
- Listados con scroll interno para evitar que tarjetas largas empujen la pantalla completa.
- Badges y labels principales alineados a lenguaje operativo: oficio vigente, historico, cerrado / terminal, credito, comision, ficha operativa y accion sugerida.
- Botones contextuales para abrir captura puntual sin mantener todos los formularios visibles al mismo tiempo.

## Formularios movidos a modal o panel

- `Registrar oficio / autorizacion`: movido a modal responsive, reutilizando `permitForm`, `submitPermit()`, `resolveCurrentPermit()` y el servicio actual.
- `Renovar oficio`: movido a modal responsive, reutilizando `renewalForm`, `submitRenewal()`, draft contextual y `POST /renew` existente.
- `Capturar credito`: movido a modal responsive, reutilizando `creditForm`, contactos, submit y bloqueo vigente/no terminal existente.
- `Registrar comision por credito`: movido a modal responsive, reutilizando `commissionForm`, tipos de comision, credito seleccionado y submit existente.

No quedaron formularios largos inline. Los paneles inline restantes son de lectura, filtros compactos, listados o mensajes de modo consulta.

## Como se conservaron flujos existentes

- No se cambiaron DTOs, servicios Angular, endpoints, payloads ni validaciones de negocio.
- Las acciones contextuales siguen llamando a los mismos metodos de preparacion y submit.
- La busqueda de vigente sigue usando la ficha/resolucion actual y navega al oficio correspondiente.
- La alta contextual solo prellena y abre el modal de oficio; la creacion real sigue en `POST /api/financials`.
- La renovacion contextual sigue generando draft de lectura y confirma con el submit existente.
- La captura de credito y comision conserva la validacion final del backend para oficio vigente/no terminal.
- La cadena de renovacion sigue consultandose y mostrandose desde los datos actuales del permiso seleccionado.

## Validacion responsive

Validacion ejecutada sobre la pantalla con foco visual/responsive:

- `npm run build`
- Playwright/Chromium headless sobre `/financials` con sesion ADMIN mockeada en `390px`, `768px` y `1366px`.
- Apertura/cierre de tabs y modales de oficio, credito, comision y renovacion.
- Confirmacion de ausencia de overflow horizontal global en los tres viewports.

Validacion de suite/solucion ejecutada:

- `npm test -- --watch=false`
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`
- `git diff -- src/backend`
- `git diff --check`

## Fuera de alcance

- Backend, endpoints, contratos API, permisos, guards, rutas, schema y migraciones.
- Catalogo maestro de financieras/dependencias/stands.
- Workflow de aprobacion, versionado contractual avanzado o ajustes historicos controlados.
- Reportes BI, consolidado transversal nuevo o cambios a `/commissions`.
- Rediseño completo de otros modulos.

## Riesgo residual

La pantalla ahora oculta formularios largos tras modales. Usuarios habituados a ver todo inline pueden necesitar abrir el tab o accion contextual correspondiente. Se mitiga con acciones visibles en encabezado, ficha operativa, listado de creditos/comisiones y detalle del oficio seleccionado.

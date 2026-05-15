# Donatarias Transparencia - validacion final demo post-modales

Fecha: 2026-05-13

## Objetivo

Cerrar la validacion end-to-end de Donatarias Transparencia despues de mover `Registrar donacion`, `Registrar aplicacion` y `Cargar evidencia` a modales contextuales, sin implementar funcionalidades nuevas.

## Cambios de codigo

No hubo cambios de codigo de producto durante esta validacion. Solo se actualizo documentacion de cierre porque la validacion quedo cerrada.

## Datos demo usados

- Donante: `Asociación Donante Demo Final`
- Referencia validada: `DON-DEMO-FINAL-1778713719836`
- Total recibido: `100000`
- Aplicaciones:
  - `Demo Final Beneficiario 1 con evidencia` por `35000`, con evidencia PDF.
  - `Demo Final Beneficiario 2 con evidencia` por `30000`, con evidencia PDF.
  - `Demo Final Beneficiario 3 inicialmente sin evidencia` por `20000`, primero sin evidencia y despues remediada desde faltantes.
- Total aplicado final: `85000`
- Saldo pendiente final: `15000`
- Evidencia: archivo PDF valido de prueba `evidencia-final-demo.pdf`.

## Validacion funcional

- Login local con usuario bootstrap `admin`.
- `/donatarias` carga correctamente.
- `Registrar donación` esta visible arriba, abre modal, cancela/cierra y guarda una donacion real.
- `Registrar aplicación` aparece solo en contexto de donacion abierta, abre modal, cancela/cierra y guarda aplicaciones reales.
- `Cargar evidencia` aparece solo con aplicacion seleccionada y donacion abierta, abre modal, cancela/cierra y carga evidencia real.
- La accion desde faltante documental selecciona la aplicacion pendiente y abre el modal correcto.
- Los formularios de donacion, aplicacion y evidencia no aparecen inline fuera de modal.
- KPIs, distribucion, semaforo documental y reporte se actualizan despues de crear aplicaciones y cargar evidencias.

## Reporte, print y navegacion

- El reporte muestra recibido, aplicado, saldo, porcentaje, aplicaciones, evidencias, faltantes y readiness.
- El caso final queda `PARTIAL` por saldo pendiente, sin faltantes documentales despues de remediar la tercera aplicacion.
- La nota de alcance legal/fiscal/contable se mantiene visible.
- La vista imprimible oculta navegacion, tabs, formularios, botones y controles tecnicos; conserva KPIs, aplicaciones, evidencias, faltantes y notas.
- No se presenta como PDF oficial ni como reporte legal/fiscal/contable.
- `/donatarias?donationId=...` selecciona la donacion.
- `/donatarias?donationId=...&applicationId=...` selecciona la aplicacion y abre contexto de evidencias.
- El panel de cierre formal muestra advertencias de saldo pendiente y no se confirmo cierre para la demo.

## Validacion tecnica ejecutada

- `npm run build`: exitoso, con warning no bloqueante existente de budget CSS en Donatarias.
- `npm test -- --watch=false`: exitoso, `22` tests.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: exitoso.
- Playwright real: exitoso, `1/1`, con alta real, upload real, print, query params y viewports `390`, `768`, `1366`.
- `git diff --check`: sin errores.
- `git diff -- src/backend`: sin cambios.

## Riesgos de presentacion

- El caso demo queda listo para presentacion controlada, pero con saldo pendiente de `15000`; por eso el readiness correcto es `PARTIAL`, no `READY`.
- La evidencia registrada acredita presencia documental minima en el sistema; no sustituye revision legal, fiscal o contable.
- La vista imprimible no es PDF oficial, no tiene folio, firma ni snapshot persistido.

## Fuera de alcance

- Nuevas funcionalidades.
- Backend, endpoints, contratos API, schema, migraciones o permisos.
- Cierre formal de la donacion demo.
- PDF oficial, CSV, workflow, aprobacion documental, checklist legal/fiscal/contable o produccion/CI-CD.

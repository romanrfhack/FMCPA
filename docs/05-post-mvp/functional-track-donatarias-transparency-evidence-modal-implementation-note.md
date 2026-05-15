# Donatarias Transparencia - Cargar evidencia en modal

Fecha: 2026-05-13

## Objetivo

Reducir scroll y mantener los tabs de aplicaciones/evidencias enfocados en transparencia, evidencia registrada y faltantes documentales moviendo `Cargar evidencia` desde formulario inline a una ventana modal contextual.

## Implementado

- Boton `Cargar evidencia` en el contexto de la aplicacion seleccionada.
- Accion `Cargar evidencia` en aplicaciones con evidencia pendiente para seleccionar la aplicacion y abrir el modal directamente.
- El boton se muestra solo cuando hay donacion seleccionada, aplicacion seleccionada, donacion no cerrada y permiso de escritura de Donatarias.
- Modal responsive con `role="dialog"` y `aria-modal="true"`.
- Cierre por boton `Cerrar`, boton `Cancelar`, Escape o click en overlay.
- Formulario de evidencia movido al modal:
  - tipo de evidencia;
  - archivo;
  - descripcion.
- Formulario inline removido del tab `Evidencias` y del detalle heredado de donaciones.
- Contexto visible dentro del modal: beneficiario, monto aplicado, fecha de aplicacion y estado documental actual.
- Advertencia visible: la evidencia registrada acredita presencia documental minima y no sustituye revision legal, fiscal o contable.

## Comportamiento conservado

- Se reutiliza `evidenceForm`.
- Se conserva `selectedEvidenceFile` y `selectedEvidenceFileName`.
- Se conservan validaciones frontend actuales.
- Se conserva `DonationsService.createApplicationEvidence`.
- No cambia payload multipart, endpoint ni contrato API.
- No cambian validaciones de upload.
- Tras carga exitosa se conserva la aplicacion seleccionada.
- La recarga de detalle actualiza documentary-status, transparency-report, KPIs, semaforo documental, faltantes y reporte.
- El modal se cierra tras la carga y la confirmacion queda visible como mensaje global.

## Acciones desde faltantes

- Las aplicaciones sin evidencia muestran accion directa `Cargar evidencia` cuando la donacion esta abierta y el usuario puede escribir.
- Al ejecutar la accion, la aplicacion queda seleccionada antes de abrir el modal.
- La accion evita que el usuario tenga que buscar manualmente la aplicacion desde otro panel.

## Validacion responsive

Validacion ejecutada:

- `npm run build`: exitoso, conserva advertencia blanda existente de presupuesto CSS de Donatarias.
- `npm test -- --watch=false`: `22/22` pruebas frontend exitosas.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: exitoso, `0` warnings y `0` errores.
- Playwright mockeado en desktop `1366px`, tablet `768px` y movil `390px`: accion desde faltante abre modal, cancelar cierra, reabrir permite cargar evidencia valida, la aplicacion deja de aparecer como pendiente y no hay overflow horizontal.
- Playwright confirma que el reporte ya no muestra faltantes basicos tras la carga y que una donacion cerrada no habilita carga de evidencia.
- `git diff -- src/backend`: sin cambios.
- `git diff --check`: sin observaciones.

## Riesgos pendientes

- El componente Donatarias conserva una advertencia blanda de presupuesto CSS; la subetapa reutilizo estilos existentes y no agrego reglas CSS nuevas.
- La evidencia cargada representa presencia documental minima, no suficiencia legal, fiscal o contable.
- El flujo no agrega revision/aprobacion documental ni checklist avanzado.

## Fuera de alcance

- Backend.
- Contratos API.
- Endpoints nuevos.
- Permisos.
- Schema o migraciones.
- CI/CD o produccion.
- Wizard de carga.
- Rediseño completo de Donatarias.
- Revision legal, fiscal o contable.
- Aprobacion documental.
- Cambiar reglas de upload.

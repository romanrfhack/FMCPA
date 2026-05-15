# Federation UX Etapa B - modales de captura

## Alcance

Se actualizo `/federation` solo en frontend para mover las capturas principales fuera de la pagina principal y dejar la pantalla enfocada en consulta, seguimiento, donaciones, aplicaciones, comisiones y evidencias.

No se modificaron backend, contratos API, endpoints, permisos, rutas, schema, migraciones, CI/CD ni produccion.

## Formularios movidos

- `Registrar gestion`: ahora abre en modal desde el tab `Gestiones`, visible solo con permiso de escritura de Federacion.
- `Agregar participante`: ahora abre en modal contextual cuando existe una gestion seleccionada, el usuario puede escribir y la gestion no esta terminal.
- `Registrar donacion`: ahora abre en modal desde el tab `Donaciones`, visible solo con permiso de escritura.
- `Registrar aplicacion`: ahora abre en modal contextual cuando existe una donacion seleccionada, el usuario puede escribir y la donacion no esta terminal. El modal muestra donante, total recibido, total aplicado y saldo pendiente.
- `Registrar comision`: ahora abre en modal contextual cuando existe aplicacion seleccionada, el usuario puede escribir y ni la donacion ni la aplicacion estan terminales. El modal muestra la aplicacion y monto asociado.
- `Cargar evidencia`: ahora abre en modal contextual cuando existe aplicacion seleccionada, el usuario puede escribir y ni la donacion ni la aplicacion estan terminales. El modal muestra beneficiario/destino, monto aplicado, estatus documental simple y la nota de alcance documental.

## Formularios inline

No quedaron formularios principales inline en Etapa B.

Los cierres formales se conservaron en su mecanismo actual porque la etapa pidio no moverlos ni redisenarlos salvo cambio minimo. Siguen usando la accion existente de cierre formal en gestiones y donaciones.

## Conservacion funcional

- Se reutilizaron los mismos `FormGroup`, validadores, DTOs, servicios y metodos submit existentes.
- No se cambiaron payloads ni endpoints.
- Tras guardar, cada modal se cierra, se resetea el formulario correspondiente y se recargan listas, detalle, resumen y alertas con las mismas llamadas que ya usaba el flujo inline.
- Las reglas de disponibilidad usan permisos actuales y `statusIsClosed` ya presente en los DTOs de gestion, donacion y aplicacion.
- La descarga de evidencias permanece sin cambios.

## Responsive y accesibilidad

- Los modales usan `role="dialog"`, `aria-modal="true"`, `aria-labelledby`, boton `Cerrar`, boton `Cancelar`, cierre por overlay y cierre con Escape siguiendo el patron ya usado en Donatarias y Financieras.
- El panel de modal usa `max-height: 90vh` con scroll interno para formularios largos.
- Las acciones por tab se envuelven en bloque responsive y pasan a ancho completo en movil.
- El panel compartido de documentos relacionados recibio un ajuste CSS minimo de `min-width: 0` y `overflow-wrap: anywhere` para evitar overflow en `/federation/evidencias` cuando se muestran codigos largos de reglas documentales.
- Se validaron los anchos objetivo `390px`, `768px` y `1366px` mediante build y validacion UI headless descrita en la sesion.

## Budget CSS

`npm run build` completo pasa, pero Angular reporta warning de budget CSS ya visible en componentes densos:

- `donatarias-page.component.ts`: 8.00 kB.
- `federation-page.component.ts`: 5.04 kB.
- `financials-page.component.ts`: 5.24 kB.

La adicion nueva se mantuvo compacta: estilos compartidos para `.modal`, `.modal-panel`, `.section-actions` y `.single-column`.

## Riesgos pendientes

- Usuarios habituados al flujo inline pueden necesitar ubicar las capturas desde los botones contextuales por tab.
- Los modales muestran saldos y contexto operativo, pero el backend sigue siendo el control final de montos, permisos y estados terminales.
- La evidencia acredita presencia documental minima; no sustituye revision legal, fiscal o contable.

## Fuera de alcance

- Backend, endpoints, contratos API, permisos, rutas, schema y migraciones.
- Wizard complejo o workflow nuevo.
- Redisenar cierres formales.
- Edicion avanzada, aprobaciones, reporte oficial, validacion legal/fiscal/contable o cambios documentales transversales.

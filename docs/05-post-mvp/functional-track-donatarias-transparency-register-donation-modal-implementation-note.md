# Donatarias Transparencia - Registrar donacion en modal

Fecha: 2026-05-13

## Objetivo

Reducir scroll y mantener `/donatarias` enfocada en consulta, transparencia y operacion contextual moviendo el alta maestra `Registrar donacion` desde formulario inline a una ventana modal.

## Implementado

- Boton `Registrar donación` en la cabecera principal de `/donatarias`, visible para usuarios con permiso de escritura de Donatarias.
- Modal responsive con `role="dialog"` y `aria-modal="true"`.
- Cierre por boton `Cerrar`, boton `Cancelar`, Escape o click en overlay.
- Formulario maestro movido al modal:
  - donante;
  - fecha;
  - tipo de donacion;
  - total recibido / valor recibido;
  - referencia;
  - estatus inicial;
  - observaciones.
- Formulario inline removido de la pagina principal.

## Comportamiento conservado

- Se reutiliza `donationForm`.
- Se conservan validaciones frontend actuales.
- Se conserva `DonationsService.createDonation`.
- No cambia payload, endpoint ni contrato API.
- Tras alta exitosa se conserva la recarga de donaciones, recarga de alertas y seleccion de la donacion creada.
- El modal se cierra tras el alta y la confirmacion queda visible como mensaje global.

## Validacion responsive

Validacion ejecutada:

- `npm run build`: exitoso, conserva advertencia blanda existente de presupuesto CSS de Donatarias.
- `npm test -- --watch=false`: `22/22` pruebas frontend exitosas.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: exitoso, `0` warnings y `0` errores.
- Playwright mockeado en desktop `1366px`, tablet `768px` y movil `390px`: boton visible, formulario inline ausente, modal abre/cancela/reabre, alta valida actualiza la lista y no hay overflow horizontal.
- En tablet y movil se valido cierre por Escape; el modal conserva salida por `Cerrar`, `Cancelar` y overlay.
- `git diff -- src/backend`: sin cambios.
- `git diff --check`: sin observaciones.

## Riesgos pendientes

- El componente Donatarias conserva una advertencia blanda de presupuesto CSS; la subetapa mantuvo estilos nuevos al minimo.
- `Registrar aplicacion` y `Cargar evidencia` siguen inline por alcance, por lo que aun pueden requerir una etapa UX posterior si se quiere compactar mas los tabs operativos.

## Fuera de alcance

- Backend.
- Contratos API.
- Endpoints nuevos.
- Permisos.
- Schema o migraciones.
- CI/CD o produccion.
- Wizard de alta.
- Rediseño completo de Donatarias.
- Mover `Registrar aplicacion`.
- Mover `Cargar evidencia`.

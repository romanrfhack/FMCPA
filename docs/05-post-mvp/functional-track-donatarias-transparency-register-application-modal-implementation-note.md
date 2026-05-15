# Donatarias Transparencia - Registrar aplicacion en modal

Fecha: 2026-05-13

## Objetivo

Reducir scroll y mantener el tab `Aplicaciones / distribucion` enfocado en transparencia, distribucion del recurso, saldo restante y estado documental moviendo el alta `Registrar aplicacion` desde formulario inline a una ventana modal.

## Implementado

- Boton `Registrar aplicación` en el contexto de la donacion seleccionada.
- El boton se muestra solo cuando hay donacion seleccionada, la donacion no esta cerrada y el usuario tiene permiso de escritura de Donatarias.
- Modal responsive con `role="dialog"` y `aria-modal="true"`.
- Cierre por boton `Cerrar`, boton `Cancelar`, Escape o click en overlay.
- Formulario de aplicacion movido al modal:
  - beneficiario;
  - fecha de aplicacion;
  - contacto responsable;
  - responsable o creador;
  - monto aplicado;
  - estatus de aplicacion;
  - detalle de comprobacion;
  - nota de cierre de la aplicacion.
- Formulario inline removido del tab `Aplicaciones / distribucion` y del detalle heredado de donaciones.
- Contexto financiero visible dentro del modal: total recibido, total aplicado actual, saldo pendiente antes de capturar y nota de no exceder saldo.

## Comportamiento conservado

- Se reutiliza `applicationForm`.
- Se conservan validaciones frontend actuales.
- Se conserva `DonationsService.createDonationApplication`.
- No cambia payload, endpoint ni contrato API.
- La validacion de negocio para no exceder el monto de la donacion sigue quedando en el flujo existente.
- Tras alta exitosa se conserva la recarga de donaciones, detalle de donacion, alertas y seleccion de la aplicacion creada.
- La recarga de detalle actualiza KPIs, distribucion, semaforos y reporte calculado.
- El modal se cierra tras el alta y la confirmacion queda visible como mensaje global.

## Validacion responsive

Validacion ejecutada:

- `npm run build`: exitoso, conserva advertencia blanda existente de presupuesto CSS de Donatarias.
- `npm test -- --watch=false`: `22/22` pruebas frontend exitosas.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: exitoso, `0` warnings y `0` errores.
- Playwright mockeado en desktop `1366px`, tablet `768px` y movil `390px`: boton visible en donacion abierta, formulario inline ausente, modal abre/cancela/reabre, alta valida actualiza aplicado/saldo y no hay overflow horizontal.
- Playwright confirma que una donacion cerrada no muestra el boton `Registrar aplicación`.
- En tablet y movil se valido cierre por Escape; el modal conserva salida por `Cerrar`, `Cancelar` y overlay.
- `git diff -- src/backend`: sin cambios.
- `git diff --check`: sin observaciones.

## Riesgos pendientes

- El componente Donatarias conserva una advertencia blanda de presupuesto CSS; la subetapa reutilizo estilos existentes y no agrego reglas CSS nuevas.
- En esta subetapa `Cargar evidencia` quedo fuera por alcance; se atendio despues en una subetapa propia con modal contextual.
- La validacion final de monto excedido sigue dependiendo del flujo existente del servicio/backend; el modal muestra contexto de saldo para reducir captura incorrecta.

## Fuera de alcance

- Backend.
- Contratos API.
- Endpoints nuevos.
- Permisos.
- Schema o migraciones.
- CI/CD o produccion.
- Wizard de alta.
- Rediseño completo de Donatarias.
- Mover `Cargar evidencia`.
- Cambiar reglas de negocio de aplicaciones o evidencias.

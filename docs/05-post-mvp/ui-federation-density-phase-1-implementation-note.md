# Federation UX Etapa A - nota de implementacion

## Alcance

Se reorganizo `/federation` solo en frontend para reducir densidad visual y separar los dominios principales sin cambiar rutas, permisos, servicios, DTOs, endpoints, schema, migraciones ni backend.

## Implementado

- Tabs internos en la misma ruta: `Resumen`, `Gestiones`, `Donaciones`, `Aplicaciones / comisiones` y `Evidencias`.
- Resumen ejecutivo calculado con datos ya disponibles en frontend: gestiones visibles, gestiones en proceso, seguimiento pendiente, concluidas/cerradas, donaciones visibles, total recibido, total aplicado, saldo pendiente, porcentaje aplicado, donaciones no/partialmente aplicadas, aplicaciones, comisiones, evidencias y alertas.
- Filtros de gestiones y donaciones compactados visualmente, con menor padding y listados con scroll interno.
- Gestiones separadas como dominio propio, con listado, alertas, detalle, participantes y cierre formal en su tab.
- Donaciones separadas como dominio financiero, con lenguaje `Total recibido`, `Total aplicado`, `Saldo pendiente` y `Porcentaje aplicado`.
- Aplicaciones y comisiones agrupadas en un tab dedicado para dejar claro que la comision pertenece a la aplicacion seleccionada.
- Evidencias agrupadas en un tab dedicado por aplicacion, conservando descarga y el panel documental transversal.
- Ajustes de copy visible hacia lenguaje operativo en labels principales de Federacion.

## Separacion de dominios

`Gestiones` conserva filtros, alta, listado, alertas, detalle, participantes y cierre formal de gestiones.

`Donaciones` conserva filtros, alta, listado, alertas y resumen financiero de la donacion seleccionada.

`Aplicaciones / comisiones` usa la donacion seleccionada y muestra aplicaciones, alta de aplicacion, comision asociada a la aplicacion seleccionada y listado de comisiones.

`Evidencias` usa la aplicacion seleccionada y muestra carga, evidencias descargables y documentos relacionados del catalogo documental.

## Formularios que siguen inline

Por decision de bajo riesgo para Etapa A, siguen inline:

- Registrar gestion.
- Agregar participante.
- Registrar donacion de Federacion.
- Registrar aplicacion.
- Registrar comision.
- Cargar evidencia.

## Recomendacion Etapa B

- Mover `Registrar donacion de Federacion` a modal.
- Mover `Registrar aplicacion`, `Registrar comision` y `Cargar evidencia` a modales o panel contextual.
- Agregar acciones contextuales desde cards/listados para reducir aun mas scroll.
- Evaluar query params para abrir tab/seleccion especifica sin crear rutas nuevas.

## Validacion responsive

Etapa A se diseno para:

- 390px: tabs en una columna, botones full-width en bloques compactos y sin ancho fijo global.
- 768px: grids colapsados y filtros/listados usables.
- 1366px: layout maestro-detalle con listas internas compactas.

La validacion automatica/runtime debe confirmar ausencia de overflow horizontal global en los tres anchos.

## Fuera de alcance

- Backend, endpoints, contratos API, permisos, schema y migraciones.
- Rutas nuevas o division del modulo en pantallas separadas.
- Cambios en reglas de negocio, validacion de montos, descarga de evidencia o cierre formal.
- Reportes nuevos, exportaciones, workflow documental o aprobacion de evidencias.

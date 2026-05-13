# Donatarias - Propuesta UX para transparencia del recurso donado

Fecha de propuesta: 2026-05-12

## Objetivo

Reorganizar Donatarias para que la primera lectura responda, sin buscar entre formularios:

- Cuanto recurso se recibio.
- Cuanto se aplico.
- Cuanto queda pendiente.
- En que se aplico.
- Que evidencia existe.
- Que falta para poder presentarlo al donante.
- Si la donacion esta abierta, lista para cierre o cerrada.

La propuesta es no breaking: primero reordena informacion y acciones existentes. Las funciones nuevas se marcan como brecha y se dejan para fases posteriores.

## Principios de UX

1. Mostrar transparencia antes que captura.
2. Separar lectura de operacion.
3. Distinguir estatus financiero, documental y operativo.
4. Hacer visible el saldo pendiente y la evidencia faltante.
5. Evitar duplicar fuentes: evidencias propias y catalogo documental deben presentarse como una sola historia.
6. No prometer cumplimiento legal cuando el sistema solo valida presencia minima de evidencia.
7. Mantener permisos actuales: lectura, escritura y cierre formal admin.

## Estructura propuesta

### Encabezado persistente

El encabezado debe reemplazar el lenguaje tecnico de etapa por lenguaje de transparencia.

Informacion visible:

- Nombre del donante de la donacion seleccionada.
- Referencia.
- Fecha de donacion.
- Tipo de donacion.
- Total recibido.
- Estatus financiero.
- Estatus documental.
- Estatus operativo.

Acciones:

- `Actualizar`.
- `Registrar donacion`.
- `Registrar aplicacion` si hay donacion seleccionada y no esta cerrada.
- `Cargar evidencia` si hay aplicacion seleccionada y no esta cerrada.
- `Cerrar formalmente` solo admin.
- `Ver reporte` cuando exista la tab de reporte.

Datos actuales reutilizables:

- `DonationDetail`.
- `DonationSummary`.
- `statusCode`, `statusName`, `statusIsClosed`.
- `appliedAmountTotal`, `remainingAmount`, `appliedPercentage`.
- `applicationCount`, `evidenceCount`.

Brechas:

- Estado documental agregado por donacion.
- Reporte listo para compartir.
- Fecha de corte formal del reporte.

## Tabs propuestas

### Tab 1: Resumen

Objetivo:

Dar una lectura ejecutiva inmediata del recurso recibido, aplicado, pendiente y comprobado.

Informacion visible:

- Total recibido.
- Total aplicado.
- Saldo pendiente.
- Porcentaje aplicado.
- Numero de aplicaciones.
- Evidencias totales.
- Aplicaciones con evidencia completa.
- Aplicaciones con evidencia pendiente.
- Ultima evidencia cargada.
- Alertas activas de la donacion seleccionada.
- Notas de la donacion.
- Semaforos: financiero, documental y operativo.

Acciones disponibles:

- Registrar aplicacion.
- Cargar evidencia para aplicacion pendiente.
- Ir a aplicaciones.
- Ir a evidencias.
- Cerrar formalmente, solo admin.

Datos actuales reutilizables:

- `baseAmount`.
- `appliedAmountTotal`.
- `remainingAmount`.
- `appliedPercentage`.
- `applications`.
- `evidenceCount`.
- `alertState`.
- `notes`.
- `statusIsClosed`.
- `DocumentRequirement` por aplicacion, si se consulta desde catalogo documental.

Brechas que requieren implementacion:

- Agregado documental por donacion sin multiples llamadas.
- Ultima evidencia agregada de manera eficiente para listas grandes.
- Checklist formal de cierre.

### Tab 2: Donaciones

Objetivo:

Operar y consultar el inventario de donaciones maestras sin mezclarlo con el detalle de aplicaciones.

Informacion visible:

- Tabla/lista de donaciones.
- Donante.
- Fecha.
- Tipo.
- Referencia.
- Total recibido.
- Total aplicado.
- Saldo pendiente.
- Porcentaje aplicado.
- Estatus financiero.
- Estatus operativo.
- Alerta.

Acciones disponibles:

- Filtrar por estatus.
- Filtrar por alertas activas.
- Seleccionar donacion.
- Registrar donacion.
- Actualizar.

Datos actuales reutilizables:

- `DonationSummary[]`.
- `DonationAlert[]`.
- Catalogo de estatus `DONATARIAS/DONATION`.

Brechas que requieren implementacion:

- Busqueda por donante/referencia en backend, si se requiere para volumen.
- Rango de fechas.
- Exportacion especifica de listado de donaciones.
- Donante como catalogo estructurado.

### Tab 3: Aplicaciones / distribucion

Objetivo:

Mostrar como se distribuyo el recurso y que parte representa cada aplicacion.

Informacion visible:

- Beneficiario.
- Fecha de aplicacion.
- Responsable.
- Monto aplicado.
- Porcentaje de la donacion.
- Estatus de aplicacion.
- Detalle de comprobacion.
- Datos de cierre.
- Numero de evidencias.
- Estado documental de la aplicacion.

Acciones disponibles:

- Registrar aplicacion.
- Seleccionar aplicacion.
- Cargar evidencia para la aplicacion.
- Ver documentos de la aplicacion.

Datos actuales reutilizables:

- `DonationDetail.applications`.
- `DonationApplication.appliedAmount`.
- `DonationApplication.verificationDetails`.
- `DonationApplication.closingDetails`.
- `DonationApplication.evidenceCount`.
- `DocumentRequirement` por aplicacion.

Brechas que requieren implementacion:

- Proposito/categoria estructurada de la aplicacion.
- Resultado o impacto de la aplicacion.
- Aprobacion/revision de comprobacion.
- Edicion/correccion controlada de una aplicacion.

### Tab 4: Evidencias

Objetivo:

Concentrar la comprobacion documental por aplicacion y evitar que el usuario tenga que buscar archivos en diferentes bloques.

Informacion visible:

- Aplicacion asociada.
- Tipo de evidencia.
- Archivo.
- Descripcion.
- Tamano.
- Fecha de carga.
- Clase documental.
- Estado de integridad.
- Estado operativo documental.
- Timeline documental.
- Conteo requerido vs actual.

Acciones disponibles:

- Cargar evidencia.
- Descargar evidencia.
- Remediar faltante documental.
- Actualizar documentos relacionados.

Datos actuales reutilizables:

- `DonationApplication.evidences`.
- `DonationApplicationEvidence`.
- `DocumentCatalogItem`.
- `DocumentRequirement`.
- `DocumentTimelineResponse`.
- `RelatedDocumentsPanelComponent`.

Brechas que requieren implementacion:

- Vista agregada de todas las evidencias de una donacion sin seleccionar aplicacion una por una.
- Revision/aprobacion documental.
- Reglas documentales mas finas por tipo de evidencia.
- Validacion de suficiencia legal o correspondencia con monto.

### Tab 5: Reporte de transparencia

Objetivo:

Preparar una vista limpia para presentar a un donante o asociacion interesada.

Informacion visible:

- Datos de la donacion.
- Corte del reporte.
- Total recibido.
- Total aplicado.
- Saldo pendiente.
- Porcentaje aplicado.
- Tabla de aplicaciones.
- Evidencias disponibles por aplicacion.
- Aplicaciones sin evidencia.
- Semaforos financiero, documental y operativo.
- Nota de alcance: el sistema valida presencia minima de evidencia, no suficiencia legal.

Acciones disponibles:

- Vista imprimible.
- Exportar, cuando se implemente.
- Copiar resumen o descargar reporte, cuando se implemente.

Datos actuales reutilizables:

- `DonationDetail`.
- `DonationApplication`.
- `DonationApplicationEvidence`.
- `DocumentCatalogItem`.
- `DocumentRequirement`.
- `AuditEvent` si se incorpora timeline funcional en una fase posterior.

Brechas que requieren implementacion:

- Endpoint o agregador de reporte.
- Exportacion PDF/CSV especifica.
- Plantilla imprimible.
- Fecha de corte formal.
- Firma/responsable del reporte, si el negocio lo requiere.

## Indicadores propuestos

| KPI | Definicion | Puede salir con datos actuales | Comentario |
| --- | --- | --- | --- |
| Total recibido | `baseAmount` de la donacion seleccionada o suma de donaciones filtradas. | Si | Para vista global puede calcularse en frontend con la pagina cargada. |
| Total aplicado | `appliedAmountTotal`. | Si | Ya viene calculado en summary/detail. |
| Saldo pendiente | `remainingAmount`. | Si | Ya viene calculado. |
| Porcentaje aplicado | `appliedPercentage`. | Si | Ya viene calculado. |
| Numero de aplicaciones | `applicationCount` o `applications.length`. | Si | Disponible. |
| Evidencias totales | `evidenceCount` o suma por aplicaciones. | Si | Disponible, pero no necesariamente equivale a evidencia activa completa. |
| Aplicaciones con evidencia completa | Aplicaciones con requisito documental `COMPLETE`. | Parcial | Requiere usar endpoints documentales por aplicacion o nuevo agregado. |
| Aplicaciones con evidencia pendiente | Aplicaciones con requisito documental `INCOMPLETE`. | Parcial | Existe por aplicacion; falta agregado por donacion. |
| Donaciones cerradas | `statusIsClosed = true`. | Si | Disponible en summary. |
| Donaciones abiertas | `statusIsClosed = false`. | Si | Disponible en summary. |
| Donaciones no aplicadas | `appliedAmountTotal <= 0` o `alertState = NOT_APPLIED`. | Si | Disponible. |
| Donaciones parcialmente aplicadas | `alertState = PARTIALLY_APPLIED`. | Si | Disponible. |
| Donaciones aplicadas al 100% | `remainingAmount = 0` o `appliedPercentage >= 100`. | Si | Disponible. |

## Semaforizacion propuesta

### Estatus financiero / aplicacion

Este semaforo responde: que paso con el dinero o recurso recibido.

| Estado | Regla propuesta | Datos actuales |
| --- | --- | --- |
| Sin aplicar | `appliedAmountTotal <= 0` y no cerrada. | Si |
| Parcialmente aplicada | `appliedAmountTotal > 0` y `appliedAmountTotal < baseAmount`. | Si |
| Aplicada | `appliedAmountTotal >= baseAmount` o estatus `APPLIED`. | Si |

Notas:

- `CLOSED` no debe sustituir este semaforo. Una donacion cerrada puede haber sido cerrada por decision operativa aunque no este al 100%.
- Si `baseAmount` es valor en especie, la UI debe decir "valor registrado" o "monto/valor recibido" para evitar lectura contable estricta.

### Estatus documental / evidencia

Este semaforo responde: existe evidencia suficiente segun reglas minimas del sistema.

| Estado | Regla propuesta | Datos actuales |
| --- | --- | --- |
| Evidencia pendiente | Al menos una aplicacion no tiene evidencia activa minima. | Parcial |
| Comprobacion completa | Todas las aplicaciones tienen al menos una evidencia activa asociada. | Parcial |
| Sin aplicaciones que comprobar | Donacion sin aplicaciones registradas. | Si |

Notas:

- Con `evidenceCount` se puede crear una version simple, pero no distingue documentos archivados o no activos.
- Para mayor precision debe usarse `DocumentRequirement` o `DocumentCompleteness` por aplicacion.
- La etiqueta debe evitar "validacion legal completa"; la cobertura actual es presencia minima de evidencia.

### Estatus operativo / cierre

Este semaforo responde: el expediente operativo sigue abierto o ya se cerro formalmente.

| Estado | Regla propuesta | Datos actuales |
| --- | --- | --- |
| Abierta | `statusIsClosed = false`. | Si |
| Cerrada | `statusIsClosed = true`. | Si |
| Cierre disponible | Usuario admin, donacion no terminal. | Si |

Notas:

- El cierre debe mostrarse separado del estado financiero/documental.
- Antes del cierre se recomienda mostrar advertencias si hay saldo pendiente o evidencia incompleta.

## Reorganizacion visual sugerida

### Layout base

- Barra superior con filtros compactos y accion `Registrar donacion`.
- Lista de donaciones a la izquierda solo en desktop; en mobile usar selector/lista colapsable.
- Panel principal por tabs.
- Formularios en modal o panel lateral contextual.
- Alertas como bloque dentro de `Resumen`, no como lista permanente separada.

### Cambios de copy recomendados

| Actual | Propuesto |
| --- | --- |
| Monto base | Total recibido / valor recibido |
| Monto aplicado | Total aplicado |
| Remanente | Saldo pendiente |
| Porcentaje | Porcentaje aplicado |
| Alta de donacion | Registrar donacion |
| Alta de aplicacion | Registrar aplicacion |
| Alta de evidencia | Cargar evidencia |
| Comprobacion / detalle | Detalle de comprobacion |
| Datos de cierre | Nota de cierre de la aplicacion |
| Aplicacion parcial | Parcialmente aplicada |

## Quick wins UX sin backend

1. Reordenar la pantalla en tabs.
2. Mover formularios a acciones contextuales o modales.
3. Poner KPIs arriba del detalle.
4. Renombrar labels para hablar de transparencia.
5. Mostrar tres semaforos separados: financiero, documental simple y operativo.
6. Agregar columna "saldo pendiente" en lista de donaciones.
7. Agregar columna "% del total" en aplicaciones.
8. Mostrar "aplicaciones sin evidencia" usando `evidenceCount === 0` como version inicial.
9. Consumir query params `donationId` y `applicationId` para seleccionar contexto desde Documentos.
10. Reemplazar `prompt` de cierre formal por modal.

## Recomendacion de implementacion UX

La primera fase debe limitarse a datos ya entregados por `DonationSummary` y `DonationDetail`. La segunda fase puede integrar mas fuerte `DocumentCatalogService` para semaforo documental agregado. El reporte para donante debe llegar despues de estabilizar la lectura de resumen, porque si se implementa antes solo exportaria la dispersion actual.

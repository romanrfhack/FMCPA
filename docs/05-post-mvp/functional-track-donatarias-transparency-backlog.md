# Donatarias - Backlog por fases para transparencia

Fecha de backlog: 2026-05-12

## Estrategia general

El camino recomendado es incremental. Primero se reorganiza lo existente sin tocar schema; despues se agregan agregados, semaforos documentales y reporte. La exportacion o vista imprimible debe llegar al final, cuando el contenido del reporte ya este validado por negocio.

## Fases sugeridas

### Fase 1: Reorganizacion UI sin schema nuevo

Estado 2026-05-12: Entregada en frontend. Se implementaron tabs, encabezado de transparencia, KPIs, semaforos separados, aplicaciones con porcentaje del total, evidencias agrupadas, vista preliminar de reporte, seleccion por query params y panel de cierre formal con advertencias. No se cambio backend, contratos API, schema, permisos ni migraciones.

Objetivo:

Hacer que la pantalla explique primero la transparencia de la donacion seleccionada y deje la captura como acciones contextuales.

Alcance:

- Tabs: Resumen, Donaciones, Aplicaciones, Evidencias y Reporte.
- KPIs del detalle usando DTOs actuales.
- Renombrar labels a lenguaje de donante.
- Formularios en modales o paneles contextuales.
- Alertas integradas al resumen.
- Soporte de query params `donationId` y `applicationId`.
- Modal para cierre formal en lugar de `prompt`.

Impacto:

- Alto para claridad.
- Bajo para riesgo tecnico.

Riesgo:

- Bajo si se mantiene la misma llamada a servicios y permisos.
- Medio si se cambia demasiado el layout sin pruebas visuales.

Requiere backend:

- No.

Requiere migracion:

- No.

Criterios de aceptacion:

- No cambia ningun contrato API.
- No cambia ningun modelo persistido.
- Las acciones actuales siguen disponibles con los mismos permisos.
- Una donacion seleccionada muestra total recibido, aplicado, saldo y porcentaje sin desplazamiento largo.
- El usuario distingue estatus financiero, documental simple y operativo.
- El enlace `/donatarias?donationId=...&applicationId=...` selecciona el contexto cuando los ids existen.

### Fase 2: Resumen/KPIs y distribucion mas clara

Estado 2026-05-12: Entregada y validada en frontend. Se agregaron KPIs de lista cargada/filtrada, saldo pendiente accionable, orden visual de aplicaciones, saldo restante por aplicacion calculado en cliente, indicador de peso relativo, criterio operativo preliminar "Lista para presentar" y reporte preliminar con distribucion financiera y faltantes basicos por aplicacion. No se cambio backend, contratos API, schema, permisos ni migraciones.

Objetivo:

Convertir el detalle de donacion en un tablero operativo de distribucion.

Alcance:

- KPIs por donacion y, si aplica, agregados de la lista filtrada.
- Tabla de aplicaciones con monto, porcentaje del total, responsable, evidencia y estatus.
- Indicador de saldo pendiente con llamada a accion.
- Ordenamiento visual de aplicaciones por fecha o monto.
- Mensajes de advertencia cuando una aplicacion dejaria saldo pendiente o cuando la donacion esta completa financieramente.

Impacto:

- Alto para transparencia financiera.
- Medio para preparacion de reporte.

Riesgo:

- Bajo con calculos frontend.
- Medio si se agrega endpoint agregado y filtros nuevos.

Requiere backend:

- No para KPIs de donacion seleccionada.
- Opcional para resumen global server-side: `GET /api/donations/summary` o equivalente.

Requiere migracion:

- No.

Criterios de aceptacion:

- Cada aplicacion muestra que porcentaje representa del total recibido.
- El saldo pendiente es visible y consistente con backend.
- Las donaciones aplicadas al 100% se distinguen de las cerradas.
- No se permite registrar aplicacion que exceda el monto, igual que hoy.

### Fase 3: Evidencia/comprobacion y semaforo documental

Estado 2026-05-12: Entregada y validada en backend/frontend. Se agrego `GET /api/donations/{donationId}/documentary-status` como agregado calculado en lectura con `DocumentRuleRegistry` y `StoredDocument` activo/no reemplazado; la UI muestra aplicaciones completas, pendientes, faltantes documentales y estado agregado en Resumen, Aplicaciones, Evidencias, Reporte preliminar y cierre formal. No se cambio schema, migraciones, permisos ni significado de estatus.

Objetivo:

Mostrar si cada aplicacion esta documentada y si la donacion esta lista documentalmente para presentarse.

Alcance:

- Semaforo documental por aplicacion.
- Conteo de aplicaciones con evidencia completa y pendiente.
- Lista de faltantes documentales dentro de la donacion.
- Integracion visible con `DocumentRequirement` y timeline documental.
- Nota de alcance: presencia minima de evidencia, no suficiencia legal.

Impacto:

- Alto para confianza del donante.
- Alto para operacion interna.

Riesgo:

- Medio si se consulta `DocumentRequirement` por cada aplicacion en frontend.
- Bajo si se crea un endpoint agregado calculado en lectura.

Requiere backend:

- No para una version inicial por aplicacion seleccionada.
- Recomendado para agregado por donacion: endpoint que devuelva estado documental de todas las aplicaciones.

Requiere migracion:

- No si se reutiliza `StoredDocument` y reglas actuales.
- Si el negocio pide checklist, aprobacion o revision humana, si puede requerir modelo nuevo.

Criterios de aceptacion:

- Una aplicacion sin evidencia activa queda marcada como `Evidencia pendiente`.
- Una aplicacion con al menos una evidencia activa queda marcada como `Comprobacion completa` en alcance minimo.
- Una donacion muestra cuantos faltantes documentales tiene.
- El usuario puede descargar evidencia desde la misma zona donde ve el estado documental.

### Fase 4: Reporte de transparencia para donante

Estado 2026-05-12: Entregada a nivel aplicacion con `GET /api/donations/{donationId}/transparency-report` protegido por `DONATIONS_READ` y tab `Reporte de transparencia` consumiendo ese endpoint. El reporte consolida resumen financiero, estado operativo, estado documental de Fase 3, aplicaciones, evidencias descargables, faltantes, readiness operativo preliminar y notas de alcance; no crea migracion, schema, permisos nuevos, PDF/CSV, folio, firma ni aprobacion documental.

Objetivo:

Consolidar la historia de la donacion en una vista presentable.

Alcance:

- Vista de reporte por donacion.
- Encabezado con donante, referencia, fecha, tipo y corte.
- KPIs financieros.
- Tabla de aplicaciones.
- Evidencias por aplicacion.
- Semaforos.
- Nota de alcance documental.
- Timeline funcional minimo, si se incorpora bitacora.

Impacto:

- Muy alto para presentacion a asociacion/donante.

Riesgo:

- Medio: requiere acuerdo de contenido con negocio.
- Medio: si se agregan datos de bitacora o documentos en una sola consulta.

Requiere backend:

- Recomendado: endpoint de reporte calculado en lectura, por ejemplo `GET /api/donations/{donationId}/transparency-report`.

Requiere migracion:

- No si el reporte usa datos actuales.
- Si se requieren aprobaciones, firma, folio de reporte o versionamiento de reporte, si requeriria modelo nuevo.

Criterios de aceptacion:

- El reporte responde claramente: recibido, aplicado, pendiente, evidencias y faltantes.
- El reporte no muestra rutas internas de archivos.
- El reporte distingue "comprobacion minima en sistema" de cumplimiento legal.
- Un usuario con solo lectura puede consultar el reporte.

### Fase 5: Exportacion o vista imprimible

Estado 2026-05-13: Fase 5A entregada como vista imprimible frontend no oficial. Se agrego accion `Imprimir reporte` dentro del tab `Reporte de transparencia`, usando los datos actuales de `GET /api/donations/{donationId}/transparency-report`; la impresion oculta navegacion principal, tabs, formularios, botones de captura/descarga y controles tecnicos, y conserva encabezado, KPIs, estados, readiness, aplicaciones, evidencias, faltantes y notas de alcance. No se agrego endpoint, PDF backend, CSV, folio, firma, snapshot/versionamiento, aprobacion documental, permisos, schema ni migracion.

Objetivo:

Permitir compartir el reporte sin dar acceso directo al sistema.

Alcance:

- Vista imprimible.
- Exportacion CSV ligera o PDF, segun prioridad de negocio.
- Encabezados seguros de descarga.
- Filtros y permisos consistentes.

Impacto:

- Alto para presentaciones externas.

Riesgo:

- Bajo para vista imprimible frontend.
- Medio para PDF backend.
- Medio para CSV si se debe incluir estructura jerarquica de aplicaciones/evidencias.

Requiere backend:

- No para vista imprimible.
- Si para archivo generado server-side o CSV/PDF oficial.

Requiere migracion:

- No.

Criterios de aceptacion:

- La vista imprimible oculta controles de captura.
- La exportacion no incluye rutas fisicas internas ni datos tecnicos sensibles.
- Los usuarios sin `DONATIONS_READ` no pueden acceder.
- El archivo respeta la misma informacion visible en el reporte.

### Mejora UX posterior: alta de donacion en modal

Estado 2026-05-13: Entregada y validada en frontend. Se movio el formulario maestro `Registrar donacion` desde el flujo inline de la pagina principal a un modal responsive abierto desde la cabecera de `/donatarias`. Se conserva `donationForm`, validaciones, `DonationsService.createDonation`, payload, endpoint, recarga de lista y seleccion de la donacion creada. En esa subetapa no se movieron `Registrar aplicacion` ni `Cargar evidencia`; `Registrar aplicacion` se atendio despues en una subetapa propia.

Objetivo:

Reducir scroll y mantener `/donatarias` enfocada en consulta, transparencia y operacion contextual.

Alcance:

- Boton `Registrar donación` visible en la cabecera para usuarios con `DONATIONS_WRITE`.
- Modal con donante, fecha, tipo, total recibido, referencia, estatus inicial y notas.
- Cierre por boton `Cerrar`, `Cancelar`, Escape o click en overlay.
- Scroll interno del modal en viewport pequeno.

Requiere backend:

- No.

Requiere migracion:

- No.

Criterios de aceptacion:

- El formulario de alta ya no aparece inline.
- El modal abre, cancela, cierra y guarda usando el comportamiento existente.
- `/donatarias` conserva filtros, lista, tabs, aplicaciones, evidencias y reporte sin cambios de contrato.

### Mejora UX posterior: alta de aplicacion en modal

Estado 2026-05-13: Entregada y validada en frontend. Se movio el formulario `Registrar aplicacion` desde el flujo inline del tab `Aplicaciones / distribucion` y del detalle heredado a un modal responsive abierto desde el contexto de la donacion seleccionada. Se conserva `applicationForm`, validaciones, `DonationsService.createDonationApplication`, payload, endpoint, recarga de lista/detalle/alertas, seleccion de la aplicacion creada y actualizacion de KPIs/semaforos/reporte. No se movio `Cargar evidencia`.

Objetivo:

Reducir scroll y mantener el tab de aplicaciones enfocado en distribucion del recurso, saldo restante, evidencia pendiente y estado documental.

Alcance:

- Boton `Registrar aplicación` visible solo con donacion seleccionada, abierta/no terminal y permiso `DONATIONS_WRITE`.
- Modal con beneficiario, fecha de aplicacion, contacto responsable, responsable, monto aplicado, estatus, detalle de comprobacion y nota de cierre.
- Contexto financiero dentro del modal: total recibido, total aplicado actual, saldo pendiente antes de capturar y nota de no exceder saldo.
- Cierre por boton `Cerrar`, `Cancelar`, Escape o click en overlay.
- Scroll interno del modal en viewport pequeno.

Requiere backend:

- No.

Requiere migracion:

- No.

Criterios de aceptacion:

- El formulario de aplicacion ya no aparece inline.
- El modal abre, cancela, cierra y guarda usando el comportamiento existente.
- Tras guardar se actualizan lista, detalle, KPIs, semaforos y reporte calculado.
- Las donaciones cerradas no muestran el boton de registro de aplicacion.
- `/donatarias` conserva filtros, lista, tabs, evidencias y reporte sin cambios de contrato.

## Backlog propuesto

| ID | Fase | Prioridad | Item | Tipo | Backend | Migracion | Criterio resumido |
| --- | --- | --- | --- | --- | --- | --- | --- |
| DON-TR-001 | 1 | P0 | Reorganizar pantalla en tabs | UX/UI | No | No | Tabs visibles y acciones actuales preservadas. |
| DON-TR-002 | 1 | P0 | Renombrar labels a lenguaje de transparencia | UX/UI | No | No | Total recibido, aplicado, saldo pendiente y evidencia son claros. |
| DON-TR-003 | 1 | P0 | KPIs arriba del detalle | UX/UI | No | No | KPIs visibles sin desplazamiento largo. |
| DON-TR-004 | 1 | P1 | Formularios en modal/panel contextual | UX/UI | No | No | Entregado para `Registrar donacion` y `Registrar aplicacion`; evidencia queda fuera de esta subetapa. |
| DON-TR-005 | 1 | P1 | Soportar query params de seleccion | Frontend | No | No | `donationId` y `applicationId` abren el contexto solicitado. |
| DON-TR-006 | 1 | P1 | Reemplazar `prompt` de cierre formal | UX/UI | No | No | Cierre usa modal con motivo y advertencias. |
| DON-TR-007 | 2 | P0 | Tabla de aplicaciones con porcentaje del total | Frontend | No | No | Cada aplicacion muestra monto y porcentaje de la donacion. |
| DON-TR-008 | 2 | P0 | Indicador de saldo pendiente accionable | UX/UI | No | No | Saldo pendiente queda visible y contextualizado. |
| DON-TR-009 | 2 | P1 | KPIs globales de lista filtrada | Frontend | No inicial | No | La lista muestra totales de donaciones cargadas. |
| DON-TR-010 | 2 | P2 | Endpoint de resumen global server-side | API | Si | No | Resumen respeta filtros y permisos sin traer todo al frontend. |
| DON-TR-021 | 2 | P1 | Criterio operativo preliminar "Lista para presentar" | Frontend | No | No | La donacion muestra Si/Parcial/No sin presentarlo como aprobacion legal. |
| DON-TR-022 | 2 | P1 | Saldo restante por aplicacion segun orden visual | Frontend | No | No | La tabla muestra saldo acumulado y aclara que depende del orden visual seleccionado. |
| DON-TR-011 | 3 | P0 | Semaforo documental simple por aplicacion | Frontend | No | No | Entregado con estado basado en agregado documental cuando existe y fallback minimo. |
| DON-TR-012 | 3 | P0 | Agregado de evidencias pendientes por donacion | API/Frontend | Si | No | Entregado con `GET /api/donations/{donationId}/documentary-status`. |
| DON-TR-013 | 3 | P1 | Unificar lista propia de evidencias y panel documental | UX/UI | No | No | Entregado como lectura agregada y copy que distingue evidencia operativa y catalogo documental relacionado. |
| DON-TR-014 | 3 | P2 | Checklist documental avanzado | Modelo | Si | Si probable | Reglas por tipo de evidencia o aprobacion quedan persistidas. |
| DON-TR-015 | 4 | P0 | Vista de reporte de transparencia | Frontend | Opcional | No | Entregado como tab real basado en `transparency-report`, con KPIs, aplicaciones, evidencias, faltantes y notas. |
| DON-TR-016 | 4 | P1 | Endpoint `transparency-report` | API | Si | No | Entregado; devuelve reporte calculado sin rutas internas. |
| DON-TR-017 | 4 | P1 | Timeline funcional de donacion | API/Frontend | Si | No | Alta, aplicaciones, evidencias y cierre se ven en secuencia. |
| DON-TR-018 | 5 | P0 | Vista imprimible | Frontend | No | No | Entregado en Fase 5A; impresion oculta navegacion, formularios y controles, sin PDF/CSV oficial. |
| DON-TR-019 | 5 | P1 | Exportacion CSV ligera | API/Frontend | Si | No | Archivo respeta permisos y no expone storage. |
| DON-TR-020 | 5 | P2 | Exportacion PDF oficial | API/Frontend | Si | No | PDF generado con plantilla aprobada. |

## Recomendacion tecnica

## Estado de Fase 1

| Item | Resultado |
| --- | --- |
| `DON-TR-001` | Entregado. |
| `DON-TR-002` | Entregado. |
| `DON-TR-003` | Entregado. |
| `DON-TR-004` | Entregado como secciones/paneles contextuales dentro de tabs; no se abrieron modales generales para todas las altas. |
| `DON-TR-005` | Entregado para carga inicial de `/donatarias?donationId=...&applicationId=...`. |
| `DON-TR-006` | Entregado con panel de cierre formal y advertencias operativas. |

## Estado de Fase 2

| Item | Resultado |
| --- | --- |
| `DON-TR-007` | Entregado y reforzado con monto aplicado, porcentaje del total recibido, saldo restante, estatus, evidencias y faltantes basicos. |
| `DON-TR-008` | Entregado con callout de saldo pendiente/aplicado y advertencia cuando la donacion esta cerrada operativamente con saldo pendiente. |
| `DON-TR-009` | Entregado como KPIs de lista cargada/filtrada calculados en frontend; no se presentan como resumen global server-side. |
| `DON-TR-010` | Pendiente para fase futura si negocio requiere totales globales auditables por endpoint. |
| `DON-TR-021` | Entregado como criterio operativo preliminar `Lista para presentar: Si/Parcial/No`. |
| `DON-TR-022` | Entregado con saldo restante por aplicacion y nota de alcance sobre orden visual. |

## Estado de Fase 3

| Item | Resultado |
| --- | --- |
| `DON-TR-011` | Entregado con semaforo por aplicacion basado en el agregado documental cuando esta disponible; la UI ya no usa solo `evidenceCount` para el estado agregado. |
| `DON-TR-012` | Entregado con endpoint calculado `GET /api/donations/{donationId}/documentary-status`, protegido por `DONATIONS_READ`, sin schema ni migracion. |
| `DON-TR-013` | Entregado con evidencia agrupada por aplicacion, panel documental relacionado y copy de alcance para evitar dos fuentes contradictorias. |
| `DON-TR-014` | Pendiente; checklist legal/documental avanzado, aprobacion humana y criterios por tipo de evidencia siguen fuera de alcance. |

## Estado de Fase 4

| Item | Resultado |
| --- | --- |
| `DON-TR-015` | Entregado con el tab `Reporte de transparencia` consumiendo el endpoint nuevo; muestra encabezado, corte, donante, KPIs financieros, estado documental, readiness, aplicaciones, evidencias y notas de alcance. |
| `DON-TR-016` | Entregado con `GET /api/donations/{donationId}/transparency-report`, protegido por `DONATIONS_READ`, calculado en lectura y sin exponer `StoredRelativePath` ni rutas fisicas. |
| `DON-TR-017` | Pendiente; timeline funcional unificado queda fuera de Fase 4 para no abrir auditoria/reporting adicional. |

## Estado de Fase 5A

| Item | Resultado |
| --- | --- |
| `DON-TR-018` | Entregado como vista imprimible frontend con `window.print()`, boton deshabilitado cuando no hay donacion seleccionada, CSS print acotado y nota de alcance visible en pantalla e impresion. |
| `DON-TR-019` | Pendiente; CSV ligero queda fuera de Fase 5A. |
| `DON-TR-020` | Pendiente; PDF oficial/backend queda fuera de Fase 5A. |

## Validacion demo integral 2026-05-13

- Datos usados: `Asociación Donante Demo`, referencia `DON-DEMO-TRANSP-2026-001`, total recibido 100000, tres aplicaciones por 35000, 25000 y 15000, dos evidencias PDF registradas y una aplicacion sin evidencia.
- Resultado funcional: `/donatarias` carga, tabs visibles, KPIs correctos, distribucion financiera visible, evidencias agrupadas y descargables, semaforo documental pendiente, reporte de transparencia con readiness `PARTIAL`, faltantes y notas de alcance.
- Resultado print: el boton `Imprimir reporte` invoca `window.print()`, el CSS print oculta navegacion, tabs, formularios y botones, conserva encabezado, KPIs, estados, aplicaciones, evidencias, faltantes y notas.
- Query params: `/donatarias?donationId=...` carga la donacion; `/donatarias?donationId=...&applicationId=...` abre el contexto de evidencias de la aplicacion seleccionada y permite ir al reporte.
- Cierre formal: el panel advierte saldo pendiente 25000, una aplicacion con evidencia minima pendiente y que la evidencia no sustituye revision legal o contable.
- Validacion tecnica: `npm run build`, `npm test -- --watch=false`, `dotnet build src/backend/FMCPA.Backend.sln --no-restore`, Playwright real `1/1` y `git diff --check` pasaron; no hubo cambios backend ni migracion.
- Estado: lista para demo controlada, explicando que el caso demo esta parcialmente listo por saldo y evidencia pendiente.

### Puede hacerse solo con frontend y DTOs actuales

- Tabs y reorganizacion visual.
- KPIs de la donacion seleccionada.
- KPIs basicos de la lista cargada.
- Semaforo financiero.
- Semaforo operativo.
- Semaforo documental simple por aplicacion con fallback usando `evidenceCount` solo si no existe agregado documental.
- Tabla de aplicaciones con porcentaje del total.
- Vista imprimible inicial.
- Seleccion por query params.
- Modal de cierre formal reutilizando `closeDonation`.
- Cambios de copy.

### Requiere endpoint nuevo o agregado API

- Resumen global filtrado si se requiere calculo server-side.
- Reporte de transparencia calculado en una sola respuesta.
- Timeline funcional unificada de donacion.
- Exportacion oficial CSV/PDF.
- Busqueda server-side por donante, referencia o fechas.

### Requiere modelo nuevo

- Catalogo formal de donantes.
- Convenios o comprobantes de recepcion de donacion.
- Propositos/categorias estructuradas de aplicacion.
- Checklist de revision documental.
- Aprobacion humana de evidencia.
- Versionamiento o folio de reporte emitido.
- Reapertura/correccion formal de donaciones o aplicaciones.

### Requiere migracion

- Cualquier nuevo campo persistido de donante, convenio, moneda, cuenta, categoria, revision, aprobacion, folio o snapshot de reporte.
- Checklist documental avanzado.
- Flujo de correccion/auditoria con entidades dedicadas.

### Debe evitarse para no romper lo existente

- Cambiar significado de `CLOSED` para que implique aplicacion financiera completa.
- Mezclar estatus financiero, documental y operativo en un solo campo.
- Reinterpretar `evidenceCount` como cumplimiento legal.
- Exponer `StoredRelativePath` o rutas internas en reportes.
- Cambiar contratos actuales de `DonationSummary` o `DonationDetail` sin versionar.
- Bloquear cierre formal solo porque hay saldo pendiente sin validar si negocio quiere conservar cierre por decision operativa.
- Duplicar logica documental fuera de `DocumentRuleRegistry` si la regla ya existe.
- Crear exportaciones que ignoren permisos actuales.

## Quick wins recomendados

1. Reordenar a tabs con `Resumen` como primera vista.
2. Cambiar copy de monto base a total recibido.
3. Agregar saldo pendiente y porcentaje aplicado como KPIs principales.
4. Agregar porcentaje por aplicacion.
5. Marcar aplicaciones sin evidencia minima activa usando el agregado documental de `StoredDocument`.
6. Integrar alertas en el resumen de la donacion seleccionada.
7. Consumir query params para mejorar navegacion desde catalogo documental.
8. Reemplazar `prompt` por modal de cierre.
9. Agregar seccion "Listo para presentar" con advertencias basicas.
10. Crear vista imprimible despues de validar el contenido del reporte.

## Riesgos y dudas abiertas

- Si la asociacion donante espera trazabilidad contable formal, el modelo actual es insuficiente porque solo registra monto/valor base y aplicaciones.
- Si se requiere comprobacion legal, la regla actual de una evidencia activa por aplicacion es demasiado basica.
- Si el volumen de donaciones crece, los agregados calculados en frontend pueden quedarse cortos.
- Si se agregan reportes externos, hay que acordar que datos son publicables y que datos son internos.
- Si se cambia el cierre formal, debe conservarse la decision ya documentada: se permite cierre operativo aunque no este totalmente aplicada.
- Evidencia heredada fuera de `StoredDocument` puede no contar para el semaforo agregado hasta que exista regularizacion documental aprobada.

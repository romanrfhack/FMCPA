# Documents UX Etapa A - Nota de implementacion

Fecha: 2026-05-15

## Alcance implementado

Se aplico una mejora visual y responsive acotada a `/documents` y `/documents/work-queue`, tomando como referencia el login institucional, el header autenticado, la navegacion agrupada y Donatarias compactada.

El cambio fue solo frontend:

- Compactacion de filtros en grillas responsive.
- Reduccion de padding, alto visual y acciones permanentes.
- Traduccion de labels tecnicos principales a lenguaje operativo.
- Reorganizacion del detalle documental para priorizar archivo, origen, clase, estado operativo, integridad, retencion y acciones.
- Agrupacion de metadata tecnica en `Detalles tecnicos`.
- Ajuste de work queue para mostrar pendiente, prioridad, motivo y accion en lenguaje de usuario.
- Conservacion de filtros, exportaciones CSV, descarga, detalle y acciones administrativas existentes.

## Labels tecnicos ajustados

| Antes | Ahora |
| --- | --- |
| `TRACK 3 DOCUMENTOS` | `Control documental` |
| `Catalogo documental` | `Documentos` |
| `documentOperationalStatusCode` / codigos `ACTIVE_OK`, `INTEGRITY_ISSUE`, etc. | `Estado operativo`: `Disponible`, `Revisar integridad`, `En resguardo`, `Requiere revision`, `Retencion vencida`, `Archivado`, `Reemplazado` |
| `retentionStatusCode` / `ACTIVE_RETENTION`, `REVIEW_DUE`, `EXPIRED_RETENTION` | `Estado de retencion`: `Dentro de periodo`, `Por revisar`, `Vencida` |
| `integrityState` / `VALID`, `MISSING_FILE`, `SIZE_MISMATCH`, `INVALID_PATH` | `Integridad`: `Correcta`, `Archivo no localizado`, `Tamano distinto`, `Ruta no valida` |
| `moduleCode` | `Modulo` |
| `entityType` | `Tipo de origen` |
| `entityId` / `GUID` | `ID de origen` / `Identificador` |
| `workItemType` | `Tipo de pendiente` |
| `severityCode` / `HIGH`, `MEDIUM`, `LOW` | `Prioridad`: `Alta`, `Media`, `Baja` |
| `reasonCode` | `Motivo` operativo: `Falta evidencia`, `Revisar archivo`, `Revision pendiente` |
| `routeHint` / `Abrir contexto` | `Ir al origen` |
| `Hold admin` | `En resguardo` |
| `Override retencion` | `Retencion ajustada` |

## Overflow y responsive

La correccion principal fue reemplazar filas rigidas por grillas `auto-fit/minmax`, permitir wrapping de botones/badges y limitar el ancho de contenido con `min-width: 0`, `box-sizing: border-box` y `overflow-wrap: anywhere` en textos largos.

En `/documents/work-queue`, la exportacion CSV se separo del formulario principal en un area secundaria compacta. En desktop queda alineada a la derecha; en movil baja con ancho seguro sin romper la pantalla.

En `/documents`, los filtros conservan todos los campos actuales pero ocupan menos alto, se apilan de forma natural en 390px y aprovechan el ancho disponible en tablet/desktop. Las filas documentales compactan badges y acciones para evitar que botones o metadatos largos generen overflow global.

## Validacion ejecutada

- `npm run build`
- `npm test -- --watch=false`
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`
- Playwright/Chromium headless con sesion ADMIN mockeada y datos documentales controlados:
  - `/documents` en 390px, 768px y 1366px.
  - `/documents/work-queue` en 390px, 768px y 1366px.
  - Filtros visibles.
  - Resumen visible.
  - Listado visible.
  - Detalle visible.
  - Exportacion visible.
  - Sin overflow horizontal global.

## Fuera de alcance

- No se modifico backend.
- No se modificaron endpoints ni contratos API.
- No se modificaron permisos, rutas, guards ni modelos de datos.
- No se crearon migraciones.
- No se cambio CI/CD ni produccion.
- No se redisenaron `/documents/review` ni la capa documental completa.
- No se convirtieron acciones administrativas a menus o modales; se dejaron disponibles y solo se compactaron visualmente.

## Riesgo residual

El relabeling reduce la exposicion de codigos tecnicos en la vista principal. Para diagnostico administrativo, los valores tecnicos y hashes siguen disponibles en la seccion secundaria `Detalles tecnicos` del detalle documental. Si operacion requiere auditoria avanzada, debe abrirse una etapa posterior sin mezclarla con esta mejora visual.

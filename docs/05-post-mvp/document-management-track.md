# Document Management Track

## Objetivo
- Pasar del almacenamiento documental local por modulo a una estrategia transversal mas consistente y gobernable.

## Alcance propuesto
- Catalogo documental transversal minimo sobre `StoredDocument`
- Ciclo de vida documental minimo `ACTIVE`/`ARCHIVED`
- Clasificacion documental minima transversal
- Edicion administrativa minima de metadata documental
- Retencion documental minima como metadata operativa
- Overrides administrativos minimos de retencion
- Bandeja minima de revision de retencion documental
- Navegacion contextual documental minima
- Completitud documental minima por entidad clave
- Remediacion documental contextual directa
- Bandeja operativa documental unificada
- Resumen ejecutivo documental transversal
- Politica documental transversal
- Reglas de almacenamiento y metadatos comunes
- Respaldo y retencion
- Limpieza segura de archivos
- Lineamientos de continuidad para evidencias y adjuntos

## Fuera de alcance
- Borrado fisico documental en esta primera subetapa
- Versionado documental en esta primera subetapa
- Retencion avanzada en esta primera subetapa
- Borrado automatico por retencion en esta primera subetapa
- Cumplimiento documental/normativo avanzado en esta primera subetapa
- Legal hold complejo en esta primera subetapa
- Overrides de retencion como legal hold formal o cumplimiento avanzado
- Backup real en esta primera subetapa
- Storage externo en esta primera subetapa
- OCR o clasificacion automatica en esta primera subetapa
- Taxonomia documental compleja en esta primera subetapa
- Edicion masiva documental en esta primera subetapa
- Integraciones externas complejas de almacenamiento
- Rediseño funcional de los modulos ya implementados
- Reporteria documental avanzada
- BI, analitica pesada y exportaciones complejas en esta primera subetapa

## Punto de partida real
- Mercados, Donatarias y Federacion almacenan archivos de forma local por modulo.
- `StoredDocument` ya registra metadata transversal de cedulas de Mercados, evidencias de Donatarias y evidencias de Federacion.
- El Track 2 ya endurecio uploads, descargas, headers, integridad operativa y permisos por modulo.

## Subetapa actual: catalogo y ciclo de vida transversal minimo
- Se implementa `GET /api/documents` para consultar documentos con filtros por modulo, entidad, area, fecha, integridad y limite/paginacion simple.
- Se implementa `GET /api/documents/{documentId}` para detalle minimo sin exponer rutas fisicas internas.
- Se implementa `GET /api/documents/{documentId}/download` como resolver unificado de descarga por `documentId`.
- La API filtra resultados por permisos efectivos de lectura de modulo: `MARKETS_READ`, `DONATIONS_READ`, `FEDERATION_READ`.
- La UI Angular agrega `/documents` para listar, filtrar, ver detalle y descargar documentos permitidos.
- La primera subetapa de catalogo no requirio migracion; el ciclo de vida minimo agrega `StatusCode`, `ArchivedUtc` y `ArchiveReason` en `StoredDocument` mediante la migracion `Track3DocumentLifecycle`.
- Se implementan `POST /api/documents/{documentId}/archive` y `POST /api/documents/{documentId}/restore` como operaciones logicas reversibles, restringidas a `ADMIN`.
- El catalogo filtra por `statusCode` y `includeArchived`; por defecto lista solo `ACTIVE`.
- Los documentos archivados siguen siendo descargables para usuarios con permiso de lectura del modulo mientras su integridad sea valida.
- `DOCUMENT_ARCHIVED` y `DOCUMENT_RESTORED` quedan registrados en `AuditEvent`.
- Se agrega clasificacion minima con `DocumentClassCode`, `BusinessPurpose`, `IsPrimaryDocument` y `ClassificationNotes`.
- Las clases documentales vigentes son `CERTIFICATE`, `SIGNED_DOCUMENT`, `SUPPORTING_DOCUMENT`, `PHOTO_EVIDENCE`, `VIDEO_EVIDENCE` y `OTHER`.
- `GET /api/documents` filtra por `documentClassCode` y la UI `/documents` muestra clase e indicador de documento principal.
- Los uploads nuevos clasifican automaticamente por area/content-type y la migracion `Track3DocumentClassification` hace backfill minimo para registros existentes.
- `PATCH /api/documents/{documentId}/metadata` permite a `ADMIN` editar clase, proposito, notas e indicador principal.
- La regla minima de principal deja un solo documento `ACTIVE` principal por `DocumentAreaCode + EntityType + EntityId`.
- La edicion no permite modificar ruta, tamano, hash, content-type, modulo, entidad, integridad ni estado documental.
- La UI `/documents` agrega un formulario simple de metadata para `ADMIN`.
- Se agrega retencion minima con `RetentionPolicyCode` y `RetentionUntilUtc` persistidos en `StoredDocument`.
- `RetentionStatusCode` se calcula al consultar como `ACTIVE_RETENTION`, `REVIEW_DUE` o `EXPIRED_RETENTION` para evitar estado obsoleto sin jobs.
- Las politicas iniciales son `CERTIFICATE_REVIEW`, `SIGNED_LONG_TERM`, `EVIDENCE_MEDIUM_TERM` y `GENERIC_REVIEW`, derivadas por clase documental.
- La retencion baseline permanece en `RetentionPolicyCode` y `RetentionUntilUtc`.
- `ADMIN` puede establecer o limpiar un override explicito con `RetentionOverridePolicyCode`, `RetentionOverrideUntilUtc` y `RetentionOverrideReason`.
- La retencion efectiva se calcula como override cuando existe; si no hay override, effective = baseline.
- `GET /api/documents`, review queue, work queue y summary usan la retencion efectiva para filtros, estados y conteos.
- `PATCH /api/documents/{documentId}/retention-override` y `DELETE /api/documents/{documentId}/retention-override` quedan restringidos a `ADMIN`.
- Set/clear de override registra `DOCUMENT_RETENTION_OVERRIDE_SET` / `DOCUMENT_RETENTION_OVERRIDE_CLEARED` y reinicia revision a `REVIEW_PENDING`.
- La UI `/documents` muestra baseline vs effective y permite a `ADMIN` operar el override con motivo breve.
- La validacion runtime de overrides se cerro en base aislada `FMCPA_RetentionOverrideRuntime_20260506`, confirmando migracion real, `ADMIN` set/clear, `403` para `OPERATOR`/`READONLY`, baseline/effective, review queue, work queue, summary y timeline/auditoria.
- Se agrega hold administrativo minimo con `IsAdministrativeHold`, `HoldReason`, `HoldPlacedUtc`, `HoldReleasedUtc` y `HoldPlacedBy`.
- `POST /api/documents/{documentId}/hold` y `DELETE /api/documents/{documentId}/hold` quedan restringidos a `ADMIN`.
- El hold administrativo no archiva, no borra, no mueve, no bloquea descarga autorizada y no representa legal hold formal.
- La politica elegida excluye documentos con hold activo de review queue y de items `RETENTION_REVIEW` en work queue; summary expone `administrativeHoldCount` y mantiene esos documentos fuera de conteos accionables de retencion.
- Set/clear de hold registra `DOCUMENT_HOLD_SET` / `DOCUMENT_HOLD_CLEARED` y la UI `/documents` permite a `ADMIN` aplicar/limpiar hold con motivo breve.
- `GET /api/documents` filtra por `retentionPolicyCode` y `retentionStatusCode`; el detalle/listado muestran politica, fecha objetivo y estado de retencion.
- `EXPIRED_RETENTION` significa listo para revision operativa; no borra, no mueve y no bloquea descarga en esta etapa.
- La migracion `Track3DocumentRetentionBaseline` hace backfill por `DocumentClassCode` y `CreatedUtc`.
- La UI `/documents` agrega filtros y badges simples de retencion.
- Se agrega revision minima de retencion con `RetentionReviewStatusCode`, `LastRetentionReviewUtc`, `NextRetentionReviewUtc` y `RetentionReviewNotes`.
- Los estados de revision son `REVIEW_PENDING`, `REVIEW_COMPLETED` y `REVIEW_DEFERRED`.
- `GET /api/documents/review-queue` lista documentos `REVIEW_DUE` o `EXPIRED_RETENTION` para `ADMIN`, con filtros por modulo, estado de revision y limite.
- `PATCH /api/documents/{documentId}/retention-review` permite a `ADMIN` marcar revision completada o diferirla hasta una fecha futura con nota breve.
- La revision no cambia clase documental, ciclo de vida `ACTIVE`/`ARCHIVED`, politica base de retencion, descarga ni storage.
- `DOCUMENT_RETENTION_REVIEWED` y `DOCUMENT_RETENTION_REVIEW_DEFERRED` quedan registrados en `AuditEvent`.
- La UI `/documents/review` agrega una bandeja simple ADMIN-only para operar la cola.
- Se agrega contexto origen en `GET /api/documents` y `GET /api/documents/{documentId}` mediante `OriginContext`.
- `GET /api/documents/by-entity` lista documentos relacionados a una entidad de negocio usando `moduleCode`, `entityType` y `entityId`.
- Las relaciones contextuales cubiertas son `MarketTenant`/cedulas, `DonationApplication`/evidencias y `FederationDonationApplication`/evidencias.
- La UI `/documents` muestra el origen del documento y un enlace contextual cuando existe `routeHint`.
- Las pantallas de Mercados, Donatarias y Federacion incorporan una seccion minima de documentos relacionados reutilizando el catalogo transversal.
- Se agregan reglas minimas de completitud documental calculadas en lectura:
  - `MarketTenant` requiere un documento activo `CERTIFICATE` asociado como cédula/certificado.
  - `DonationApplication` requiere al menos una evidencia documental activa asociada.
  - `FederationDonationApplication` requiere al menos una evidencia documental activa asociada.
- Las reglas anteriores quedan centralizadas en `DocumentRuleRegistry`, con `ruleCode`, modulo, entidad, entidad documental cubierta, area documental, clases requeridas, cardinalidad minima y mensajes de faltante.
- `GET /api/documents/rules` expone en solo lectura las reglas activas visibles para los modulos que el usuario puede leer.
- `GET /api/documents/requirements/by-entity` expone el requisito aplicable por entidad, estado `COMPLETE`/`INCOMPLETE`, conteo actual/minimo y remediacion operativa minima.
- `GET /api/documents/completeness/by-entity` evalua una entidad puntual y devuelve `COMPLETE` o `INCOMPLETE`.
- `GET /api/documents/pending` lista entidades incompletas con filtro por modulo, permisos efectivos de lectura y contexto de regla incumplida.
- La UI `/documents` muestra una seccion simple de pendientes documentales y los paneles relacionados muestran requisito documental, estado, clases requeridas, conteo actual/minimo y remediacion minima en Mercados, Donatarias y Federacion.
- La remediacion directa queda disponible en el panel contextual cuando el usuario tiene permiso de escritura del modulo.
- `MarketTenant` puede cargar/reemplazar cédula desde el contexto del locatario mediante `POST /api/markets/tenants/{tenantId}/cedula`.
- `DonationApplication` y `FederationDonationApplication` reutilizan los endpoints existentes de evidencia desde el mismo panel contextual.
- La cola de pendientes mantiene navegacion clara hacia el origen para resolver faltantes sin convertirse en workflow o bandeja de tareas.
- La trazabilidad minima de reemplazo queda en `StoredDocument`: el documento previo queda `ARCHIVED`/superseded y el vigente queda `ACTIVE` con `ReplacedDocumentId`.
- El catalogo y detalle exponen `isSuperseded`, `supersededByDocumentId`, `replacedDocumentId` y `replacementGroupKey`.
- `GET /api/documents/{documentId}/timeline` y `GET /api/documents/timeline/by-entity` exponen historia documental minima usando carga inicial sintetica, eventos documentales de `AuditEvent` y relaciones de reemplazo.
- `GET /api/documents/work-queue` consolida pendientes de completitud documental, issues de integridad e items de revision de retencion en una bandeja operativa calculada.
- La severidad de la bandeja es simple: `HIGH` para faltantes requeridos e integridad, `MEDIUM` para `EXPIRED_RETENTION` y `LOW` para `REVIEW_DUE`.
- La UI `/documents/work-queue` muestra filtros por modulo, tipo y severidad, badges operativos y enlaces a contexto/remediacion existente.
- `GET /api/documents/summary` expone un resumen ejecutivo documental calculado en lectura.
- El resumen incluye `totalDocuments`, `activeDocuments`, `archivedDocuments`, `administrativeHoldCount`, `integrityIssuesCount`, `incompleteEntitiesCount`, `reviewDueCount` y `expiredRetentionCount`.
- Tambien incluye desglose por modulo, por clase documental, por estado operativo derivado y por categorias de work queue para priorizacion rapida.
- El resumen respeta permisos de lectura por modulo: el usuario solo ve numeros de modulos que puede leer y los documentos con `ModuleCode` no mapeado quedan fuera.
- La UI `/documents` agrega una seccion de resumen con KPIs principales, desglose por modulo y acceso directo a `/documents/work-queue`.
- Esta subetapa no agrega migracion ni persiste metricas; reutiliza `StoredDocument` y la composicion de la work queue.
- Cada documento expone un estado operativo derivado no persistido: `INTEGRITY_ISSUE`, `ON_HOLD`, `SUPERSEDED`, `ARCHIVED`, `RETENTION_EXPIRED`, `REVIEW_DUE` o `ACTIVE_OK`.
- La precedencia documentada es integridad, hold, superseded, archivado, retencion vencida, revision proxima y activo OK.
- `GET /api/documents` permite filtrar por `documentOperationalStatusCode`; catalogo, detalle, summary y work queue exponen tambien una severidad simple.
- `GET /api/documents/export`, `GET /api/documents/work-queue/export` y `GET /api/documents/review-queue/export` generan CSV ligero reutilizando filtros y permisos de sus consultas base.
- Las exportaciones responden como attachment con `Content-Type: text/csv`, `Content-Disposition` y `Cache-Control: no-store`; no incluyen `StoredRelativePath` ni rutas fisicas internas.
- La exportacion respeta el limite paginado existente (`take`, maximo `200`) y no abre reportes masivos, XLSX complejo ni centro de BI.
- La UI `/documents`, `/documents/work-queue` y `/documents/review` agrega un boton simple `Exportar CSV` que usa los filtros activos de cada pantalla.

## Entregables esperados
- Catalogo documental transversal minimo
- Ciclo de vida documental minimo sin borrado fisico
- Clasificacion documental minima
- Edicion administrativa minima de metadata
- Retencion documental minima consultable
- Overrides administrativos minimos de retencion
- Hold administrativo minimo
- Bandeja minima de revision de retencion
- Navegacion contextual documental minima
- Completitud documental minima por entidad clave
- Registry canónico de reglas documentales minimas
- Requisitos documentales en contexto de entidad
- Remediacion directa de faltantes documentales en contexto
- Trazabilidad minima de reemplazo documental
- Timeline documental minimo
- Bandeja documental unificada
- Resumen ejecutivo documental
- Estado operativo documental derivado
- Exportacion documental ligera de catalogo, work queue y review queue
- Criterios comunes de storage documental
- Politica de respaldo y retencion
- Reglas base para acceso, limpieza y recuperacion
- Decision de continuidad para el storage local actual

## Items candidatos
- PMB-007
- PMB-008
- PMB-010
- PMB-029

## Criterios de salida sugeridos
- Existe una superficie transversal usable para consultar documentos existentes.
- Existe archivado/restauracion logica con filtros de estado y auditoria minima.
- Existe clasificacion documental minima con filtro por clase y defaults compatibles.
- Existe edicion ADMIN-only de metadata minima y regla de documento principal.
- Existe retencion documental minima con filtro por politica/estado y sin borrado automatico.
- Existe override administrativo minimo de retencion con baseline vs effective visible, auditoria, validacion runtime real y sin legal hold formal.
- Existe hold administrativo minimo reversible, visible en catalogo/detalle/summary, que excluye documentos de senales accionables de retencion sin bloquear descarga ni abrir legal hold formal.
- Existe estado operativo documental derivado, con precedencia explicita y visible en catalogo/detalle/summary/work queue, sin persistir estado calculado.
- Existe bandeja de revision de retencion para documentos `REVIEW_DUE` y `EXPIRED_RETENTION`, sin borrar ni mover archivos.
- Existe contexto origen visible en el catalogo documental y consulta por entidad de negocio.
- Las pantallas de Mercados, Donatarias y Federacion muestran documentos relacionados sin rehacer sus modulos.
- Existen reglas minimas de completitud para `MarketTenant`, `DonationApplication` y `FederationDonationApplication`.
- Las reglas minimas se declaran en una fuente canónica reusable y no como mensajes/constantes dispersas en la evaluacion.
- Existen requisitos documentales visibles en contexto de entidad para Mercados, Donatarias y Federacion.
- Existen acciones contextuales para remediar faltantes en `MarketTenant`, `DonationApplication` y `FederationDonationApplication`, respetando permisos de escritura por modulo.
- Existe relacion explicita entre documento reemplazado y documento vigente sin abrir versionado completo.
- Existe linea de tiempo documental minima por documento y por entidad, basada en `StoredDocument`, `AuditEvent` y relaciones de reemplazo.
- Existe bandeja documental unificada que consolida completitud, integridad y revision de retencion sin abrir workflow ni asignaciones.
- Existe resumen ejecutivo documental con KPIs principales, desglose por modulo y coherencia con catalogo/work queue.
- Existe exportacion CSV ligera de catalogo documental, work queue y review queue, coherente con filtros/permisos y sin exponer rutas fisicas internas.
- Existe listado transversal de pendientes documentales filtrado por permisos de lectura de modulo.
- La consulta respeta permisos por modulo y no expone rutas fisicas internas.
- Existe una estrategia documental comun documentada y aprobable.
- Las reservas sobre respaldo, retencion y limpieza dejan de estar dispersas.
- El equipo puede decidir si mantiene storage local endurecido o migra a una solucion transversal posterior.

## Dependencias
- Requiere coordinacion con Security Track para acceso futuro a archivos.

## Estado
- Iniciado con catalogo documental transversal minimo, ciclo de vida logico `ACTIVE`/`ARCHIVED`, clasificacion documental minima, edicion administrativa minima de metadata, retencion documental minima como metadata operativa, overrides administrativos minimos de retencion validados runtime, hold administrativo minimo, estado operativo documental derivado, bandeja ADMIN-only de revision de retencion, navegacion contextual minima, completitud documental minima por entidad clave, registry canónico de reglas, requisitos documentales en contexto, remediacion contextual directa, trazabilidad minima de reemplazo documental, timeline documental minimo, bandeja operativa documental unificada, resumen ejecutivo documental y exportacion documental ligera CSV, pendiente de aprobacion formal.

## Referencias
- [Document Management Track Transversal Catalog Implementation Note](./document-management-track-transversal-catalog-implementation-note.md)
- [Document Management Track Document Lifecycle Implementation Note](./document-management-track-document-lifecycle-implementation-note.md)
- [Document Management Track Document Classification Implementation Note](./document-management-track-document-classification-implementation-note.md)
- [Document Management Track Document Metadata Editing Implementation Note](./document-management-track-document-metadata-editing-implementation-note.md)
- [Document Management Track Retention Baseline Implementation Note](./document-management-track-retention-baseline-implementation-note.md)
- [Document Management Track Retention Overrides Implementation Note](./document-management-track-retention-overrides-implementation-note.md)
- [Document Management Track Document Hold Implementation Note](./document-management-track-document-hold-implementation-note.md)
- [Document Management Track Document Operational Status Implementation Note](./document-management-track-document-operational-status-implementation-note.md)
- [Document Management Track Retention Review Queue Implementation Note](./document-management-track-retention-review-queue-implementation-note.md)
- [Document Management Track Document Context Navigation Implementation Note](./document-management-track-document-context-navigation-implementation-note.md)
- [Document Management Track Document Completeness Implementation Note](./document-management-track-document-completeness-implementation-note.md)
- [Document Management Track Document Rules Registry Implementation Note](./document-management-track-document-rules-registry-implementation-note.md)
- [Document Management Track Document Requirements In Context Implementation Note](./document-management-track-document-requirements-in-context-implementation-note.md)
- [Document Management Track Contextual Remediation Implementation Note](./document-management-track-contextual-remediation-implementation-note.md)
- [Document Management Track Document Replacement Traceability Implementation Note](./document-management-track-document-replacement-traceability-implementation-note.md)
- [Document Management Track Document Timeline Implementation Note](./document-management-track-document-timeline-implementation-note.md)
- [Document Management Track Document Work Queue Implementation Note](./document-management-track-document-work-queue-implementation-note.md)
- [Document Management Track Document Summary Implementation Note](./document-management-track-document-summary-implementation-note.md)
- [Document Management Track Light Export Implementation Note](./document-management-track-light-export-implementation-note.md)

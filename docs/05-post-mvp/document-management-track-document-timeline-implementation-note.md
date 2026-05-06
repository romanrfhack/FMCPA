# Track 3 - Document Timeline Implementation Note

## Objetivo
Agregar una linea de tiempo documental minima por documento y por entidad, reutilizando `StoredDocument`, `AuditEvent`, navegacion contextual y trazabilidad de reemplazo, sin abrir versionado completo ni plataforma documental avanzada.

## Que se implemento
- `GET /api/documents/{documentId}/timeline` para consultar la historia de un documento puntual.
- `GET /api/documents/timeline/by-entity?moduleCode=...&entityType=...&entityId=...` para consultar la historia documental de la entidad origen.
- Contratos `DocumentTimelineResponse` y `DocumentTimelineEventResponse`.
- UI minima en `/documents` para mostrar la historia del documento seleccionado.
- Timeline contextual en el panel reusable de documentos relacionados usado por Mercados, Donatarias y Federacion.
- Cobertura de regresion para carga inicial, reemplazo de cédula y documento superseded visible en timeline.

## Eventos cubiertos
- `DOCUMENT_UPLOADED`: evento sintetico calculado desde `StoredDocument.CreatedUtc`.
- `DOCUMENT_ARCHIVED` y `DOCUMENT_RESTORED`: eventos desde `AuditEvent`.
- `DOCUMENT_METADATA_UPDATED` y `DOCUMENT_PRIMARY_CHANGED`: eventos desde `AuditEvent`.
- `DOCUMENT_RETENTION_REVIEWED` y `DOCUMENT_RETENTION_REVIEW_DEFERRED`: eventos desde `AuditEvent`.
- `DOCUMENT_REPLACED`: evento desde `AuditEvent`, relacionado con `ReplacedDocumentId`.
- `DOCUMENT_SUPERSEDED`: evento sintetico calculado desde `SupersededByDocumentId` para que el documento anterior muestre claramente que fue sustituido.

## Timeline por documento
La API valida que el documento exista y que el usuario tenga permiso de lectura del modulo (`MARKETS_READ`, `DONATIONS_READ` o `FEDERATION_READ`). La respuesta se ordena cronologicamente e incluye solo informacion operativa: tipo, fecha, titulo, detalle, documento asociado y relacion de reemplazo cuando aplica. No se expone `MetadataJson` crudo ni rutas fisicas.

## Timeline por entidad
La API reutiliza la consulta de documentos por entidad ya aprobada:
- `MarketTenant` y cédulas de Mercados.
- `DonationApplication` y evidencias asociadas.
- `FederationDonationApplication` y evidencias asociadas.

El timeline de entidad mezcla los eventos de los documentos relacionados para entender la evolucion documental sin crear un workflow ni una consola avanzada.

## Replacement traceability
Cuando una cédula de `MarketTenant` reemplaza a otra:
- El documento vigente muestra `DOCUMENT_REPLACED` y referencia el documento previo.
- El documento previo muestra `DOCUMENT_SUPERSEDED` y referencia el documento vigente.
- La completitud sigue contando solo documentos `ACTIVE` no superseded.

## Como validar localmente
1. Ejecutar `dotnet restore src/backend/FMCPA.Backend.sln`.
2. Ejecutar `dotnet build src/backend/FMCPA.Backend.sln --no-restore`.
3. Ejecutar `dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter "DocumentCatalogTests"`.
4. Ejecutar `npm run build` en `src/frontend`.
5. Con backend local, cargar una cédula inicial de `MarketTenant` y luego reemplazarla con `POST /api/markets/tenants/{tenantId}/cedula`.
6. Consultar `GET /api/documents/{documentId}/timeline` para el documento vigente y para el reemplazado.
7. Consultar `GET /api/documents/timeline/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId={tenantId}`.
8. Confirmar que el vigente tenga `DOCUMENT_REPLACED`, el anterior tenga `DOCUMENT_SUPERSEDED` y que ambos mantengan relacion cruzada.

## Fuera de alcance
- Versionado completo.
- Numeracion formal de versiones.
- Arbol historico documental.
- Auditoria forense.
- Backup real.
- Storage externo.
- OCR, clasificacion automatica o compliance documental avanzado.

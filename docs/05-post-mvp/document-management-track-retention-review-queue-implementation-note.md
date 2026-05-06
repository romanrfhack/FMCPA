# Document Management Track Retention Review Queue Implementation Note

## Objetivo
Implementar una bandeja minima de revision de retencion documental sobre `StoredDocument` para que `ADMIN` pueda operar documentos con retencion proxima a revision o vencida, sin borrar, mover, retener legalmente ni convertir el sistema en una plataforma documental completa.

## Que se implemento
- Campos nuevos en `StoredDocument`:
  - `RetentionReviewStatusCode`
  - `LastRetentionReviewUtc`
  - `NextRetentionReviewUtc`
  - `RetentionReviewNotes`
- Constantes de estado en `DocumentRetentionReviewStatusCodes`.
- Migracion `Track3DocumentRetentionReviewQueue`.
- Endpoint `GET /api/documents/review-queue`.
- Endpoint `PATCH /api/documents/{documentId}/retention-review`.
- Campos de revision expuestos en listado y detalle de `/api/documents`.
- Vista Angular `/documents/review`, protegida para `ADMIN`.
- Filtros simples por modulo, estado de revision y limite.
- Acciones de `marcar revisado` y `diferir revision`.
- Auditoria minima con `DOCUMENT_RETENTION_REVIEWED` y `DOCUMENT_RETENTION_REVIEW_DEFERRED`.
- Guardrail de superficie actualizado para los endpoints nuevos.

## Decisiones tomadas
- La bandeja queda restringida a `ADMIN` mediante `USERS_ADMIN`.
- Los estados operativos son:
  - `REVIEW_PENDING`
  - `REVIEW_COMPLETED`
  - `REVIEW_DEFERRED`
- Por defecto la queue incluye documentos no completados con `RetentionUntilUtc` dentro de la ventana `REVIEW_DUE` o ya vencidos.
- Un documento `REVIEW_DEFERRED` vuelve a aparecer en la queue default cuando `NextRetentionReviewUtc` ya vencio.
- Filtrar explicitamente por `REVIEW_DEFERRED` permite ver documentos diferidos aunque su fecha futura aun no haya vencido.
- Marcar revisado o diferir revision no cambia:
  - clase documental
  - ciclo de vida `ACTIVE`/`ARCHIVED`
  - politica base de retencion
  - `RetentionUntilUtc`
  - rutas fisicas
  - descarga autorizada
  - integridad documental

## Estados y acciones
- `REVIEW_PENDING`: estado default para documentos existentes y nuevos.
- `REVIEW_COMPLETED`: indica que `ADMIN` reviso operativamente el documento.
- `REVIEW_DEFERRED`: indica que la revision se pospuso hasta una fecha futura simple.

Acciones permitidas por API:
- `REVIEW_COMPLETED` con nota opcional.
- `REVIEW_DEFERRED` con `nextRetentionReviewUtc` futuro y nota opcional.

La API rechaza:
- documentos con `ACTIVE_RETENTION`, porque todavia no estan en revision operativa;
- estados no soportados;
- `REVIEW_DEFERRED` sin fecha futura;
- notas mayores a 500 caracteres.

## Como validarlo localmente
Comandos base:

```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
npm run build
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --filter FullyQualifiedName~DocumentCatalogTests
```

Con backend local y un documento de prueba en `REVIEW_DUE` o `EXPIRED_RETENTION`:

```bash
curl -s "http://127.0.0.1:5080/api/documents/review-queue?take=20" \
  -H "Authorization: Bearer ${ADMIN_TOKEN}"

curl -i -X PATCH "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}/retention-review" \
  -H "Authorization: Bearer ${ADMIN_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"retentionReviewStatusCode":"REVIEW_COMPLETED","nextRetentionReviewUtc":null,"retentionReviewNotes":"Revision operativa local"}'

curl -i -X PATCH "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}/retention-review" \
  -H "Authorization: Bearer ${ADMIN_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"retentionReviewStatusCode":"REVIEW_DEFERRED","nextRetentionReviewUtc":"2030-05-05T00:00:00+00:00","retentionReviewNotes":"Diferido localmente"}'
```

Validaciones esperadas:
- `ADMIN` recibe `200` en queue y acciones.
- `OPERATOR` y `READONLY` reciben `403`.
- El catalogo refleja `RetentionReviewStatusCode`, `LastRetentionReviewUtc`, `NextRetentionReviewUtc` y `RetentionReviewNotes`.
- La descarga del documento sigue funcionando segun la politica actual de descarga e integridad.
- `AuditEvent` contiene `DOCUMENT_RETENTION_REVIEWED` o `DOCUMENT_RETENTION_REVIEW_DEFERRED`.

## Que quedo fuera
- Borrado fisico o automatico.
- Legal hold complejo.
- Workflow de aprobaciones.
- Scheduler, alertas o notificaciones.
- Recalculo materializado de estados.
- Backup real.
- Storage externo.
- OCR o clasificacion automatica.
- Cumplimiento documental/normativo avanzado.

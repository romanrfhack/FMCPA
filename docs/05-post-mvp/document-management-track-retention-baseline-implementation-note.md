# Track 3 - Retention Baseline Implementation Note

## Que se implemento
- Metadata minima de retencion sobre `StoredDocument`:
  - `RetentionPolicyCode`
  - `RetentionUntilUtc`
- Estado de retencion calculado en la API:
  - `ACTIVE_RETENTION`
  - `REVIEW_DUE`
  - `EXPIRED_RETENTION`
- Filtros transversales:
  - `retentionPolicyCode`
  - `retentionStatusCode`
- Respuestas de listado y detalle enriquecidas con politica, fecha objetivo y estado de retencion.
- UI `/documents` con filtros, badges y detalle de retencion.
- Migracion `Track3DocumentRetentionBaseline` con backfill por clase documental y `CreatedUtc`.

## Politicas definidas
- `CERTIFICATE_REVIEW`: documentos `CERTIFICATE`, fecha objetivo `CreatedUtc + 1 año`.
- `SIGNED_LONG_TERM`: documentos `SIGNED_DOCUMENT`, fecha objetivo `CreatedUtc + 7 años`.
- `EVIDENCE_MEDIUM_TERM`: documentos `SUPPORTING_DOCUMENT`, `PHOTO_EVIDENCE` y `VIDEO_EVIDENCE`, fecha objetivo `CreatedUtc + 3 años`.
- `GENERIC_REVIEW`: documentos `OTHER` o no mapeados, fecha objetivo `CreatedUtc + 1 año`.

Estas reglas son operativas y provisionales. No representan cumplimiento legal ni una matriz normativa aprobada.

## Estrategia de backfill
- La migracion agrega columnas nullable, actualiza registros existentes y despues las vuelve requeridas.
- `RetentionPolicyCode` se asigna desde `DocumentClassCode`.
- `RetentionUntilUtc` se calcula desde `CreatedUtc`.
- Los documentos existentes quedan consultables inmediatamente por politica y estado de retencion.

## Estado de retencion
- `RetentionStatusCode` no se persiste.
- La API lo calcula en lectura para evitar estado obsoleto sin scheduler:
  - `EXPIRED_RETENTION`: `RetentionUntilUtc` es anterior a la fecha actual.
  - `REVIEW_DUE`: `RetentionUntilUtc` vence dentro de los proximos 30 dias.
  - `ACTIVE_RETENTION`: fecha objetivo posterior a la ventana de revision.

## Regla operativa
- `EXPIRED_RETENTION` no borra archivos.
- `EXPIRED_RETENTION` no mueve archivos.
- `EXPIRED_RETENTION` no bloquea descargas autorizadas.
- El estado solo indica que el documento esta listo para revision operativa.

## Como validarlo localmente
```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter FullyQualifiedName~DocumentCatalogTests --logger "console;verbosity=minimal"
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --logger "console;verbosity=minimal"
npm run build
npm test -- --watch=false
```

Con backend local levantado:
```bash
curl -s "http://127.0.0.1:5080/api/documents?retentionPolicyCode=EVIDENCE_MEDIUM_TERM&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents?retentionStatusCode=REVIEW_DUE&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents?retentionStatusCode=EXPIRED_RETENTION&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -i "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}/download" -H "Authorization: Bearer ${TOKEN}"
```

La descarga de documentos expirados debe seguir funcionando si el usuario tiene permiso de lectura del modulo y la integridad es `VALID`.

## Validacion ejecutada en la implementacion
- `dotnet restore src/backend/FMCPA.Backend.sln`: exitoso.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: exitoso.
- `npm run build`: exitoso.
- `dotnet test ... --filter FullyQualifiedName~DocumentCatalogTests`: `10/10` exitosos.
- `dotnet test ...`: `120/120` exitosos.
- `npm test -- --watch=false`: `19/19` exitosos.
- Migracion `Track3DocumentRetentionBaseline` aplicada sobre `FMCPA_DocRetention_20260505`.
- Backfill real validado sobre `FMCPA_RetentionBackfill2_20260505`: documento `SUPPORTING_DOCUMENT` creado antes de la migracion de retencion quedo con `EVIDENCE_MEDIUM_TERM` y `RetentionUntilUtc = 2027-05-05T00:00:00+00:00`.

## Que quedo fuera
- Borrado automatico.
- Movimiento fisico de archivos.
- Retencion legal avanzada.
- Legal hold.
- Notificaciones o workflows de revision.
- Backup real.
- Storage externo.
- OCR o clasificacion automatica.
- Plataforma documental completa.
- Cambios de produccion o CI/CD.

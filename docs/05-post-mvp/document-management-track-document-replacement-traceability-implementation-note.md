# Document Management Track - Document Replacement Traceability Implementation Note

## Objetivo
Conservar trazabilidad minima cuando un documento vigente sustituye a otro, especialmente en cédulas de `MarketTenant`, sin abrir versionado completo ni una plataforma documental avanzada.

## Implementado
- `StoredDocument` agrega `ReplacedDocumentId`, `SupersededByDocumentId` y `ReplacementGroupKey`.
- La migracion `Track3DocumentReplacementTraceability` agrega esos campos, hace backfill del grupo de reemplazo y reemplaza el indice unico previo por indices de consulta.
- `POST /api/markets/tenants/{tenantId}/cedula` ahora crea un nuevo `StoredDocument` vigente cuando existe una cédula previa.
- El documento previo queda `ARCHIVED`, no principal y con `SupersededByDocumentId` apuntando al documento vigente.
- El documento nuevo queda `ACTIVE`, principal y con `ReplacedDocumentId` apuntando al documento anterior.
- El catalogo y detalle documental exponen `isSuperseded`, `supersededByDocumentId`, `replacedDocumentId` y `replacementGroupKey`.
- `/documents` muestra indicacion visual de reemplazado o vigente que reemplaza a otro.
- Se registra auditoria minima `DOCUMENT_REPLACED`.

## Politica elegida para documento sustituido
El documento sustituido se archiva logicamente como `ARCHIVED` y queda marcado como superseded. Sigue descargable para usuarios con permiso de lectura del modulo mientras su integridad sea `VALID`.

No se borra, no se mueve fisicamente y no se convierte en version formal.

## Como se determina el vigente
- Vigente operativo: `StatusCode = ACTIVE` y `SupersededByDocumentId = null`.
- Reemplazado: `SupersededByDocumentId` con valor.
- Documento que reemplaza a otro: `ReplacedDocumentId` con valor.
- La completitud documental cuenta solo documentos activos no sustituidos.

## Validacion local
Comandos base:
```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
npm run build --prefix src/frontend
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --filter "Contextual_market_tenant_certificate_remediation_completes_requirement_and_requires_write_permission"
```

Validacion runtime sugerida:
1. Levantar backend local en `Development` con SQL Server local y migraciones aplicadas.
2. Crear un `MarketTenant` o usar uno existente.
3. Subir cédula inicial con `POST /api/markets/tenants/{tenantId}/cedula`.
4. Subir una segunda cédula por el mismo endpoint.
5. Consultar `GET /api/documents/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId={tenantId}&includeArchived=true`.
6. Confirmar un documento `ACTIVE` con `replacedDocumentId` y un documento `ARCHIVED` con `supersededByDocumentId`.
7. Consultar detalle de ambos documentos y confirmar que no se exponen rutas fisicas.
8. Consultar completitud por entidad y confirmar `COMPLETE` usando el documento vigente.
9. Verificar evento `DOCUMENT_REPLACED` en bitacora/auditoria.

## Fuera de alcance
- Versionado completo.
- Arbol de versiones o comparacion entre versiones.
- Edicion masiva documental.
- Borrado fisico o purga automatica.
- Backup real, storage externo, OCR, legal hold y compliance documental avanzado.

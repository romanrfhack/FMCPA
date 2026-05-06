# Document Management Track - Document Metadata Editing Implementation Note

## Que se implemento
- Se agrego `PATCH /api/documents/{documentId}/metadata` para edicion administrativa minima.
- La edicion permite actualizar:
  - `documentClassCode`
  - `businessPurpose`
  - `isPrimaryDocument`
  - `classificationNotes`
- La API valida clase documental permitida y longitud maxima de `500` caracteres para proposito/notas.
- La API registra `DOCUMENT_METADATA_UPDATED` en `AuditEvent`.
- Si cambia la marca de principal o se desmarca un principal previo, registra `DOCUMENT_PRIMARY_CHANGED`.
- La vista Angular `/documents` agrega formulario simple para `ADMIN`.

## Decisiones tomadas
- No se crea migracion: `StoredDocument` ya tenia los campos necesarios desde `Track3DocumentClassification`.
- La edicion no permite modificar ruta fisica, nombre fisico, tamano, hash, content-type, modulo, entidad, estado ni integridad.
- La superficie queda protegida por `USERS_ADMIN`.
- `OPERATOR` y `READONLY` reciben `403`.

## Regla de documento principal
- Solo puede existir un documento `ACTIVE` marcado como principal por:
  - `DocumentAreaCode`
  - `EntityType`
  - `EntityId`
- Si un `ADMIN` marca un documento como principal, la API desmarca otros documentos activos principales del mismo conjunto.
- Un documento `ARCHIVED` no puede marcarse como principal.
- Esta regla es metadata operativa del catalogo; no crea versionado ni jerarquia documental.

## Como validarlo localmente
```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter FullyQualifiedName~DocumentCatalogTests --logger "console;verbosity=minimal"
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --logger "console;verbosity=minimal"
npm run build
npm test -- --watch=false
```

Validacion funcional esperada:
- `ADMIN` ejecuta `PATCH /api/documents/{documentId}/metadata` con clase/proposito/notas/principal validos y recibe `200`.
- `GET /api/documents/{documentId}` refleja la metadata editada.
- Si se marca un documento como principal, otros principales activos del mismo conjunto quedan en `false`.
- `OPERATOR` o `READONLY` reciben `403` al usar el endpoint.
- La vista `/documents` permite editar metadata solo a `ADMIN`.

Resultados locales de cierre de la subetapa:
- `DocumentCatalogTests`: `9/9` exitosos.
- `FMCPA.Api.AuthorizationRegressionTests`: `119/119` exitosos.
- Frontend Angular tests: `19/19` exitosos.

## Que quedo fuera
- Versionado documental.
- Retencion avanzada.
- Backup real.
- Storage externo.
- OCR o clasificacion automatica por contenido.
- Edicion masiva.
- Edicion de rutas, hash, tamano, content-type, integridad, entidad o estado.
- Plataforma documental completa.

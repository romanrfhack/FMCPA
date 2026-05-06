# Document Management Track - Document Lifecycle Implementation Note

## Que se implemento
- Se agrego ciclo de vida minimo a `StoredDocument` con estado `ACTIVE`/`ARCHIVED`.
- Se agregaron columnas `StatusCode`, `ArchivedUtc` y `ArchiveReason` mediante la migracion `Track3DocumentLifecycle`.
- Se agregaron endpoints:
  - `POST /api/documents/{documentId}/archive`
  - `POST /api/documents/{documentId}/restore`
- `GET /api/documents` y `GET /api/documents/{documentId}` ahora exponen estado documental y metadata de archivado.
- El catalogo transversal soporta `statusCode` e `includeArchived`; por defecto lista solo documentos `ACTIVE`.
- La vista Angular `/documents` agrega filtro de estado, indicador `ACTIVE`/`ARCHIVED` y acciones ADMIN para archivar/restaurar.
- Se registran eventos `DOCUMENT_ARCHIVED` y `DOCUMENT_RESTORED` en `AuditEvent`.

## Decisiones tomadas
- `ARCHIVED` es un estado logico reversible; no borra ni mueve archivos fisicos.
- Archivado/restauracion quedan restringidos a `ADMIN` mediante la policy `USERS_ADMIN`.
- El motivo de archivado es opcional, breve y operativo; no es una taxonomia formal.
- No se abren permisos por documento individual ni una matriz documental nueva.
- Se reutilizan permisos de lectura por modulo para listado, detalle y descarga.

## Politica de descarga de archivados
- Los documentos archivados siguen siendo descargables para usuarios con permiso de lectura del modulo y con integridad `VALID`.
- Esta politica trata `ARCHIVED` como ocultamiento operativo del catalogo por defecto, no como cuarentena, retencion legal ni bloqueo de evidencia.
- Si mas adelante se requiere impedir descargas de archivados, debe abrirse una decision especifica de politica documental.

## Filtros soportados
- `statusCode=ACTIVE`
- `statusCode=ARCHIVED`
- `includeArchived=true`
- Los filtros previos siguen vigentes: `moduleCode`, `entityType`, `entityId`, `documentAreaCode`, `integrityState`, `fromUtc`, `toUtc`, `skip` y `take`.
- Si no se envia `statusCode` ni `includeArchived=true`, el listado devuelve solo `ACTIVE`.

## Como se respetan permisos
- Listado, detalle y descarga filtran por permisos de lectura del modulo documental: `MARKETS_READ`, `DONATIONS_READ`, `FEDERATION_READ`.
- Archivado/restauracion requieren `USERS_ADMIN`.
- `OPERATOR` y `READONLY` reciben `403` al intentar archivar o restaurar.

## Como validarlo localmente
La migracion de este paso fue generada con:
```bash
dotnet tool run dotnet-ef migrations add Track3DocumentLifecycle --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --context PlatformDbContext --output-dir Persistence/Migrations --no-build
```

Para validar una copia local con la migracion ya versionada:
```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
FMCPA_SQL_PORT=14334 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-doc-life FMCPA_DB_NAME=FMCPA_DocLifecycle_20260505 ./scripts/local/apply-migrations.sh
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter FullyQualifiedName~DocumentCatalogTests --logger "console;verbosity=minimal"
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --logger "console;verbosity=minimal"
npm run build
npm test -- --watch=false
```

Validacion funcional esperada:
- Crear o reutilizar un documento en `StoredDocument`.
- Confirmar que aparece por defecto en `GET /api/documents` como `ACTIVE`.
- Ejecutar `POST /api/documents/{documentId}/archive` como `ADMIN`.
- Confirmar que el documento ya no aparece en el listado por defecto.
- Confirmar que aparece con `statusCode=ARCHIVED` o `includeArchived=true`.
- Confirmar que `GET /api/documents/{documentId}/download` sigue respondiendo segun permisos de lectura e integridad.
- Ejecutar `POST /api/documents/{documentId}/restore` como `ADMIN`.
- Confirmar que vuelve a `ACTIVE`.
- Confirmar `403` para `OPERATOR` o `READONLY` en archive/restore.
- Revisar eventos `DOCUMENT_ARCHIVED` y `DOCUMENT_RESTORED` en `AuditEvent`.

Resultados locales de cierre de la subetapa:
- `DocumentCatalogTests`: `5/5` exitosos.
- `FMCPA.Api.AuthorizationRegressionTests`: `111/111` exitosos.
- Frontend Angular tests: `19/19` exitosos.

## Que quedo fuera
- Borrado fisico.
- Versionado documental.
- Retencion avanzada.
- Backup real.
- Storage externo.
- OCR o clasificacion automatica.
- Antivirus/DLP.
- Permisos por documento individual.
- Workflow documental complejo o consola documental completa.

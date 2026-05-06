# Document Management Track - Document Classification Implementation Note

## Que se implemento
- Se agrego clasificacion documental minima en `StoredDocument`.
- Se agregaron campos:
  - `DocumentClassCode`
  - `BusinessPurpose`
  - `IsPrimaryDocument`
  - `ClassificationNotes`
- Se agrego la migracion `Track3DocumentClassification`.
- `GET /api/documents` soporta filtro `documentClassCode`.
- El listado y detalle documental exponen clase, proposito, indicador de documento principal y notas de clasificacion.
- La vista Angular `/documents` agrega filtro de clase, badge de clase e indicador de documento principal.
- Los uploads nuevos asignan clasificacion de forma deterministica desde el area documental y el content-type.

## Clases definidas
- `CERTIFICATE`
- `SIGNED_DOCUMENT`
- `SUPPORTING_DOCUMENT`
- `PHOTO_EVIDENCE`
- `VIDEO_EVIDENCE`
- `OTHER`

## Estrategia de asignacion
- Cédulas de Mercados:
  - `DocumentClassCode = CERTIFICATE`
  - `BusinessPurpose = Acreditar la cedula digitalizada del locatario.`
  - `IsPrimaryDocument = true`
- Evidencias PDF de Donatarias:
  - `DocumentClassCode = SUPPORTING_DOCUMENT`
  - `BusinessPurpose = Soportar la aplicacion documental de una donacion.`
  - `IsPrimaryDocument = false`
- Evidencias JPEG/PNG de Donatarias:
  - `DocumentClassCode = PHOTO_EVIDENCE`
  - mismo proposito de Donatarias
- Evidencias PDF de Federacion:
  - `DocumentClassCode = SUPPORTING_DOCUMENT`
  - `BusinessPurpose = Soportar la aplicacion documental de Federacion.`
  - `IsPrimaryDocument = false`
- Evidencias JPEG/PNG de Federacion:
  - `DocumentClassCode = PHOTO_EVIDENCE`
  - mismo proposito de Federacion
- Documentos de areas no mapeadas:
  - `DocumentClassCode = OTHER`

## Estrategia de backfill/defaults
- La migracion agrega `DocumentClassCode` como requerido con default `OTHER`.
- La migracion actualiza documentos existentes de areas conocidas:
  - `MARKETS_TENANT_CERTIFICATES` -> `CERTIFICATE`, principal.
  - `DONATIONS_APPLICATION_EVIDENCES` -> `PHOTO_EVIDENCE` si `ContentType` es imagen; si no, `SUPPORTING_DOCUMENT`.
  - `FEDERATION_APPLICATION_EVIDENCES` -> misma regla que Donatarias.
- `BusinessPurpose` se rellena solo para areas conocidas.
- `ClassificationNotes` queda reservado para notas breves futuras; no se abre edicion desde UI en esta etapa.

## Como validar localmente
La migracion de este paso fue generada con:
```bash
dotnet tool run dotnet-ef migrations add Track3DocumentClassification --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --context PlatformDbContext --output-dir Persistence/Migrations --no-build
```

Validacion local:
```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
FMCPA_SQL_PORT=14335 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-doc-class FMCPA_DB_NAME=FMCPA_DocClassification_20260505 ./scripts/local/apply-migrations.sh
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter FullyQualifiedName~DocumentCatalogTests --logger "console;verbosity=minimal"
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --logger "console;verbosity=minimal"
npm run build
npm test -- --watch=false
```

Validacion funcional esperada:
- `GET /api/documents?documentClassCode=CERTIFICATE` devuelve documentos clasificados como certificados.
- Documentos creados sin clasificacion explicita quedan en `OTHER`.
- Un upload nuevo de cédula de Mercado crea `StoredDocument` con `CERTIFICATE`, `IsPrimaryDocument=true` y proposito de negocio.
- El catalogo combina `documentClassCode` con filtros de modulo, estado e integridad.

Resultados locales de cierre de la subetapa:
- `DocumentCatalogTests`: `7/7` exitosos.
- `FMCPA.Api.AuthorizationRegressionTests`: `113/113` exitosos.
- Frontend Angular tests: `19/19` exitosos.

## Que quedo fuera
- Versionado documental.
- Retencion avanzada.
- Backup real.
- Storage externo.
- OCR.
- Clasificacion automatica por contenido.
- Taxonomia compleja o tabla administrable de clases.
- Edicion manual de clasificacion por usuario.
- Plataforma documental completa.

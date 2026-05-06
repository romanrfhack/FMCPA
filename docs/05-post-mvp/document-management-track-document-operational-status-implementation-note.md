# Document Management Track - Document Operational Status Implementation Note

## Que se implemento
- Estado operativo documental derivado en lectura sobre `StoredDocument` y la inspeccion de integridad existente.
- Contratos de catalogo/detalle con:
  - `documentOperationalStatusCode`
  - `documentOperationalSeverityCode`
- Filtro de catalogo `documentOperationalStatusCode`.
- Summary con desglose `operationalStatuses`.
- Work queue con estado/severidad operativa contextual para items asociados a documento.
- UI `/documents` con filtro, badge, detalle y desglose por estado operativo.
- UI `/documents/work-queue` con badge contextual de estado operativo cuando el item tiene documento.

## Estados definidos
- `INTEGRITY_ISSUE`
- `ON_HOLD`
- `SUPERSEDED`
- `ARCHIVED`
- `RETENTION_EXPIRED`
- `REVIEW_DUE`
- `ACTIVE_OK`

## Precedencia elegida
La senal se resuelve en este orden:

1. `INTEGRITY_ISSUE`
2. `ON_HOLD`
3. `SUPERSEDED`
4. `ARCHIVED`
5. `RETENTION_EXPIRED`
6. `REVIEW_DUE`
7. `ACTIVE_OK`

Esto significa que un documento con archivo faltante e incluso hold activo se muestra como `INTEGRITY_ISSUE`; un documento en hold vencido se muestra como `ON_HOLD`; un documento reemplazado archivado se muestra como `SUPERSEDED` antes que `ARCHIVED`.

## Severidad simple
- `HIGH`: `INTEGRITY_ISSUE`
- `MEDIUM`: `RETENTION_EXPIRED`
- `LOW`: `ON_HOLD`, `REVIEW_DUE`, `ARCHIVED`, `SUPERSEDED`
- `NONE`: `ACTIVE_OK`

## Como validar localmente

```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
npm run build
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter Document_catalog_exposes_operational_status_and_applies_documented_precedence
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter DocumentCatalogTests
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build
```

Resultados de esta sesion:
- `dotnet restore`: proyectos actualizados.
- `dotnet build`: exitoso, `0` warnings, `0` errors.
- `npm run build`: exitoso, sin warnings.
- Prueba focal de precedencia: `1/1` exitosa.
- `DocumentCatalogTests`: `20/20` exitosos.
- Suite `FMCPA.Api.AuthorizationRegressionTests`: `190/190` exitosos.

Con backend local y token valido:

```bash
curl -s "http://127.0.0.1:5080/api/documents?documentOperationalStatusCode=ON_HOLD&includeArchived=true&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/summary" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/work-queue?take=20" -H "Authorization: Bearer ${TOKEN}"
```

## Quedo fuera
- Persistir el estado derivado.
- Migracion nueva.
- Scoring complejo.
- Analitica pesada o BI.
- Versionado completo.
- Compliance avanzado.
- Legal hold formal.
- OCR, clasificacion automatica, backup real, storage externo o plataforma documental completa.

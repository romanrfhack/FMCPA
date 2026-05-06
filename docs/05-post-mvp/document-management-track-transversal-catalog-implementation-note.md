# Document Management Track - Transversal Catalog Implementation Note

## Objetivo
Abrir `Track 3` con una superficie minima de consulta documental transversal sobre `StoredDocument`, sin abrir todavia versionado, retencion avanzada, backup real, storage externo, OCR ni clasificacion automatica.

## Que se implemento
- API `GET /api/documents` para listado limitado/paginado de documentos.
- API `GET /api/documents/{documentId}` para detalle minimo.
- API `GET /api/documents/{documentId}/download` para descarga unificada por `documentId`.
- Contratos `DocumentCatalogListResponse`, `DocumentCatalogItemResponse` y `DocumentCatalogDetailResponse`.
- Filtros por `moduleCode`, `entityType`, `entityId`, `documentAreaCode`, `integrityState`, `fromUtc`, `toUtc`, `skip` y `take`.
- Filtrado por permisos efectivos de lectura de modulo.
- Vista Angular `/documents` con filtros simples, listado, detalle y descarga.
- Pruebas backend `DocumentCatalogTests`.
- Guardrail de superficie actualizado para cubrir los nuevos endpoints.

## Decisiones tomadas
- `StoredDocument` sigue siendo la fuente transversal de metadata documental; no se crea migracion.
- El listado transversal solo muestra modulos documentales con mapeo explicito y permiso de lectura aprobado.
- La descarga unificada valida integridad `VALID` antes de servir el archivo.
- La API no expone `StoredRelativePath` ni rutas fisicas internas en listado o detalle.
- La pantalla `/documents` es operativa y simple; no intenta ser una consola documental completa.

## Permisos por modulo
| ModuleCode | Permiso requerido |
| --- | --- |
| `MARKETS` | `MARKETS_READ` |
| `DONATARIAS` | `DONATIONS_READ` |
| `FEDERATION` | `FEDERATION_READ` |

`ADMIN` ve todo dentro del alcance actual porque su rol deriva esos permisos. Si aparece un nuevo `ModuleCode`, debe agregarse un mapeo explicito antes de quedar visible en el catalogo transversal.

## Integridad y descarga
- El listado y detalle usan `IDocumentBinaryStore.InspectAsync` para exponer `IntegrityState` y `ActualSizeBytes`.
- La descarga rechaza documentos con integridad distinta de `VALID` con `409`.
- La respuesta de descarga reutiliza los headers seguros existentes: `nosniff`, `no-store` y `Content-Disposition` de attachment.

## Como validarlo localmente
```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --filter FullyQualifiedName~DocumentCatalogTests --logger "console;verbosity=minimal"
npm run build
npm test -- --watch=false
```

Con backend local levantado:
```bash
TOKEN="<token-devuelto-por-login>"
curl -s "http://127.0.0.1:5080/api/documents?take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents?moduleCode=MARKETS&integrityState=VALID&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/<document-id>" -H "Authorization: Bearer ${TOKEN}"
curl -i "http://127.0.0.1:5080/api/documents/<document-id>/download" -H "Authorization: Bearer ${TOKEN}"
```

En frontend local:
- Entrar a `http://127.0.0.1:4200/documents`.
- Filtrar por modulo o integridad.
- Seleccionar un documento.
- Descargarlo desde el detalle o la fila.

## Validacion ejecutada en la sesion
- `dotnet restore src/backend/FMCPA.Backend.sln`: exitoso.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: exitoso.
- `npm run build`: exitoso.
- `dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter FullyQualifiedName~DocumentCatalogTests --logger "console;verbosity=minimal"`: `3/3` exitosos.
- `dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --logger "console;verbosity=minimal"`: `101/101` exitosos.
- `npm test -- --watch=false`: `19/19` exitosos.
- `git diff --check`: exitoso.

## Fuera de alcance
- Versionado documental.
- Retencion avanzada.
- Backup real.
- Storage externo.
- OCR.
- Clasificacion automatica.
- Permisos por documento individual.
- Consola documental avanzada, exportaciones o busqueda full-text.
- Cambios de produccion o CI/CD.

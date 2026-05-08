# Track 3 - Exportacion ligera documental

## Que se implemento
- `GET /api/documents/export` para exportar el catalogo documental transversal en CSV.
- `GET /api/documents/work-queue/export` para exportar la bandeja documental unificada en CSV.
- `GET /api/documents/review-queue/export` para exportar la bandeja ADMIN-only de revision de retencion en CSV.
- Botones `Exportar CSV` en `/documents`, `/documents/work-queue` y `/documents/review`.
- Cobertura de regresion para contenido CSV, headers de descarga, ausencia de rutas fisicas internas y permisos de review queue.
- Guardrail e inventario de autorizacion actualizados para las tres rutas nuevas.
- No se agrego migracion ni persistencia nueva; todo se resuelve por consulta/formateo en lectura.

## Politica de exportacion
- Formato unico de esta subetapa: CSV.
- Las exportaciones reutilizan los filtros de sus consultas base.
- Las exportaciones respetan el mismo control de permisos:
  - catalogo y work queue: usuario autenticado con filtrado por permisos de lectura de modulo;
  - review queue: `ADMIN` por `USERS_ADMIN`, igual que la cola base.
- Las respuestas usan `Content-Type: text/csv`, `Content-Disposition` como attachment y `Cache-Control: no-store`.
- No se exportan rutas fisicas internas, `StoredRelativePath` ni metadata de storage no necesaria.
- El alcance sigue paginado con `take` normalizado hasta `200`; no es exportacion masiva ni backup.

## Columnas principales
- Catalogo: `documentId`, modulo, area, entidad, origen, archivo original, clase, lifecycle, estado operativo, integridad, retencion efectiva, hold, superseded y fecha de creacion.
- Work queue: llave de item, tipo, severidad, modulo, entidad, origen, documento relacionado, resumen, motivo, estado operativo, accion sugerida, ruta y remediacion.
- Review queue: documento, modulo, entidad, clase, retencion/revision, fechas de retencion/revision, hold, estado operativo, integridad y fecha de creacion.

## Como validar localmente

```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
npm run build
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter DocumentCatalogTests
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter "AuthorizationRegressionTests|AuthorizationSurfaceGuardrailTests"
npm test -- --watch=false
```

Validacion manual con backend local y token valido:

```bash
curl -i "http://127.0.0.1:5080/api/documents/export?moduleCode=MARKETS&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -i "http://127.0.0.1:5080/api/documents/work-queue/export?take=20" -H "Authorization: Bearer ${TOKEN}"
curl -i "http://127.0.0.1:5080/api/documents/review-queue/export?take=20" -H "Authorization: Bearer ${TOKEN}"
```

Verificar en cada respuesta:
- `200 OK` cuando el rol/permisos aplican.
- `text/csv`.
- `Content-Disposition` con nombre `.csv`.
- `Cache-Control: no-store`.
- filas coherentes con filtros activos.
- ausencia de `StoredRelativePath` o rutas fisicas internas.

Para review queue, repetir con `OPERATOR` o `READONLY` y esperar `403`.

## Quedo fuera
- XLSX y formatos complejos.
- Centro de reportes.
- BI o analitica pesada.
- Exportaciones masivas o scheduler.
- Backup real, storage externo o persistencia de archivos exportados.
- Nuevos permisos.
- Legal hold, compliance avanzado, OCR, versionado y plataforma documental completa.

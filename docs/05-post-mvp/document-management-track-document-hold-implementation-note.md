# Document Management Track - Document Hold Implementation Note

## Que se implemento
- Hold administrativo minimo sobre `StoredDocument` con `IsAdministrativeHold`, `HoldReason`, `HoldPlacedUtc`, `HoldReleasedUtc` y `HoldPlacedBy`.
- Migracion `Track3DocumentAdministrativeHold`.
- Endpoints ADMIN-only:
  - `POST /api/documents/{documentId}/hold`
  - `DELETE /api/documents/{documentId}/hold`
- Catalogo, detalle y summary exponen estado/metadatos de hold.
- `/documents` permite a `ADMIN` aplicar y limpiar hold con motivo breve.
- Auditoria minima con `DOCUMENT_HOLD_SET` y `DOCUMENT_HOLD_CLEARED`, visible tambien en timeline documental.

## Politica elegida
- El hold es administrativo y reversible.
- No es legal hold formal.
- No implica archivado.
- No implica borrado.
- No mueve archivos.
- No cambia politica baseline/effective de retencion.
- No bloquea descarga autorizada mientras la integridad del documento siga siendo valida.

## Impacto en colas y summary
- Documentos con hold activo no aparecen en `GET /api/documents/review-queue`.
- Documentos con hold activo no generan items `RETENTION_REVIEW` en `GET /api/documents/work-queue`.
- Summary cuenta `administrativeHoldCount` y mantiene `reviewDueCount`/`expiredRetentionCount` coherentes con la work queue, es decir, sin contar holds activos como pendientes accionables de retencion.
- Integridad y completitud no se pausan por hold administrativo; la exclusion aplica solo a tratamiento operativo de retencion.

## Validacion local
Comandos ejecutados en esta sesion:

```bash
dotnet ef migrations add Track3DocumentAdministrativeHold --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --context PlatformDbContext --output-dir Persistence/Migrations
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
npm run build
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter DocumentCatalogTests
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build
docker ps
dotnet ef database update --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --context PlatformDbContext --no-build
dotnet run --no-build --project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --urls http://127.0.0.1:5080
curl -s http://127.0.0.1:5080/health
git diff --check
```

Resultados:
- Migracion generada correctamente.
- Backend compila sin warnings ni errores.
- Frontend compila sin warnings ni errores.
- `DocumentCatalogTests`: `19/19` exitosos.
- `FMCPA.Api.AuthorizationRegressionTests`: `189/189` exitosos, incluyendo guardrails de superficie y endpoints `POST/DELETE /hold` ADMIN-only.
- Backend local arranco en `http://127.0.0.1:5080` y `/health` respondio `Healthy`.
- `dotnet ef database update` no pudo aplicarse porque no hay SQL Server local accesible en `localhost,1433`.
- Los scripts locales con Docker no pudieron usarse porque `docker` no existe en esta distro WSL.
- `git diff --check` no reporto problemas de whitespace.

La prueba `Admin_can_set_and_clear_administrative_hold_and_retention_queues_pause_actionability` valida:
- `ADMIN` aplica hold.
- `READONLY` recibe `403` al aplicar hold.
- `OPERATOR` recibe `403` al limpiar hold.
- Un documento en hold sigue descargable.
- La revision directa de retencion queda bloqueada mientras el hold esta activo.
- Review queue y work queue de retencion excluyen el documento en hold.
- Summary incrementa/decrementa `administrativeHoldCount` y no cuenta el hold como pendiente de retencion accionable.
- `ADMIN` limpia hold.
- Auditoria contiene `DOCUMENT_HOLD_SET` y `DOCUMENT_HOLD_CLEARED`.

## Quedo fuera
- Legal hold formal.
- Cumplimiento documental avanzado.
- Aprobaciones multinivel.
- Borrado fisico o automatico.
- Backup real.
- Storage externo.
- OCR.
- Versionado documental completo.
- Plataforma documental completa.
- Validacion runtime SQL real en esta sesion por bloqueo de entorno local.

# Track 3 - Overrides administrativos minimos de retencion

## Objetivo
- Permitir ajustes excepcionales de retencion efectiva por parte de `ADMIN`, manteniendo visible la retencion baseline y sin abrir legal hold, compliance avanzado ni plataforma documental completa.

## Que se implemento
- Campos nuevos en `StoredDocument`:
  - `RetentionOverridePolicyCode`
  - `RetentionOverrideUntilUtc`
  - `RetentionOverrideReason`
- Migracion `Track3RetentionOverrides`.
- Endpoints ADMIN-only:
  - `PATCH /api/documents/{documentId}/retention-override`
  - `DELETE /api/documents/{documentId}/retention-override`
- Catalogo/detalle con baseline vs effective y estado de override.
- Auditoria minima:
  - `DOCUMENT_RETENTION_OVERRIDE_SET`
  - `DOCUMENT_RETENTION_OVERRIDE_CLEARED`
- UI `/documents` para que `ADMIN` vea baseline/effective, establezca override y lo limpie.

## Baseline vs effective
- Baseline:
  - `RetentionPolicyCode`
  - `RetentionUntilUtc`
- Override:
  - `RetentionOverridePolicyCode`
  - `RetentionOverrideUntilUtc`
  - `RetentionOverrideReason`
- Effective:
  - si no hay override, effective = baseline
  - si hay override, effective usa la politica/fecha override disponible

Las respuestas mantienen `retentionPolicyCode`, `retentionUntilUtc` y `retentionStatusCode` como valores efectivos para compatibilidad operativa, y agregan campos explicitos `retentionBaseline*`, `retentionEffective*` y `hasRetentionOverride`.

## Uso del override
- `ADMIN` puede establecer politica override, fecha override o ambas.
- El motivo breve es obligatorio al establecer override.
- Limpiar override restaura la retencion efectiva a la baseline.
- Establecer o limpiar override reinicia la revision a `REVIEW_PENDING`, porque una revision previa puede quedar obsoleta al cambiar la retencion efectiva.
- Filtros de retencion, review queue, work queue y summary usan la retencion efectiva.

## Validacion local
```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
dotnet ef migrations add Track3RetentionOverrides --project src/backend/src/FMCPA.Infrastructure --startup-project src/backend/src/FMCPA.Api --context PlatformDbContext --output-dir Persistence/Migrations --no-build
dotnet ef database update --project src/backend/src/FMCPA.Infrastructure --startup-project src/backend/src/FMCPA.Api --context PlatformDbContext --no-build
npm run build
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter DocumentCatalogTests
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build
npm test -- --watch=false
dotnet run --project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --no-build --launch-profile http
curl -i -s http://127.0.0.1:5080/health
```

Resultado de la validacion de esta etapa:
- `dotnet restore src/backend/FMCPA.Backend.sln`: exitoso.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: exitoso, `0` warnings, `0` errores.
- `npm run build`: exitoso.
- `npm test -- --watch=false`: exitoso, `21/21`.
- `dotnet test ... --filter DocumentCatalogTests`: exitoso, `18/18`.
- `dotnet test ...`: exitoso, `180/180`.
- `dotnet run ... --launch-profile http` + `/health`: API levanto en `5080` y `/health` respondio `200`.
- `dotnet ef database update ... --no-build`: inicialmente bloqueado porque SQL Server local no estaba accesible; la validacion runtime posterior lo cerro sobre una base aislada.
- `git diff --check`: exitoso.

Resultado de validacion runtime real:
- Stack aislado:
  - SQL Server container `fmcpa-sql-retention-overrides`
  - SQL port `14360`
  - API port `5110`
  - DB `FMCPA_RetentionOverrideRuntime_20260506`
  - Storage `/tmp/fmcpa-retention-overrides-storage`
  - Bootstrap local `admin`/`operator`/`readonly`
- `20260506002221_Track3RetentionOverrides` confirmado en `dbo.__EFMigrationsHistory`.
- Login runtime:
  - `admin`: `200`, rol `ADMIN`
  - `operator`: `200`, rol `OPERATOR`
  - `readonly`: `200`, rol `READONLY`
- Documento semilla:
  - baseline `EVIDENCE_MEDIUM_TERM` hasta `2029-01-01T00:00:00+00:00`
  - effective inicial igual a baseline
  - estado inicial `ACTIVE_RETENTION`
- `OPERATOR` y `READONLY` recibieron `403` en `PATCH /api/documents/{documentId}/retention-override`.
- `ADMIN` establecio override `GENERIC_REVIEW` hasta `2026-05-01T00:00:00+00:00` con respuesta `200`.
- Despues del set:
  - `hasRetentionOverride = true`
  - effective policy `GENERIC_REVIEW`
  - estado `EXPIRED_RETENTION`
  - review queue incluyo el documento
  - work queue incluyo item `RETENTION_REVIEW` con reason `EXPIRED_RETENTION`
  - summary incremento `expiredRetentionCount` de `0` a `1`
- `OPERATOR` y `READONLY` recibieron `403` en `DELETE /api/documents/{documentId}/retention-override`.
- `ADMIN` limpio el override con respuesta `200`.
- Despues del clear:
  - effective volvio a baseline
  - estado `ACTIVE_RETENTION`
  - review queue y work queue dejaron de incluir el documento
  - summary regreso `expiredRetentionCount` a `0`
- `GET /api/documents/{documentId}/timeline` mostro `DOCUMENT_RETENTION_OVERRIDE_SET` y `DOCUMENT_RETENTION_OVERRIDE_CLEARED`.
- No hubo cambios de codigo durante el cierre runtime.

Validacion manual opcional:
```bash
curl -i -X PATCH "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}/retention-override" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H 'Content-Type: application/json' \
  -d '{"retentionOverridePolicyCode":"GENERIC_REVIEW","retentionOverrideUntilUtc":"2026-05-01T00:00:00+00:00","retentionOverrideReason":"Validacion local"}'

curl -s "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/review-queue?take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/work-queue?workItemType=RETENTION_REVIEW&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/summary" -H "Authorization: Bearer ${TOKEN}"

curl -i -X DELETE "http://127.0.0.1:5080/api/documents/${DOCUMENT_ID}/retention-override" \
  -H "Authorization: Bearer ${TOKEN}"
```

## Que quedo fuera
- Legal hold formal.
- Aprobaciones multinivel.
- Workflow de cumplimiento.
- Borrado automatico.
- Backup real.
- Storage externo.
- OCR o clasificacion automatica.
- Analitica/compliance avanzado.

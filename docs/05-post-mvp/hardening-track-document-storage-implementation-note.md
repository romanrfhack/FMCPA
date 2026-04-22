# Hardening Track Document Storage Implementation Note

## Estado
- Fecha: 2026-04-20
- Track: `Track 1: Hardening y consistencia operativa`
- Subpaso: `Hardening transversal minimo del storage documental`
- Resultado actual: Implementacion completada, pendiente de aprobacion formal del track

## Que se implemento
- Se agrega la entidad transversal ligera `StoredDocument` para registrar metadatos homogeneos de archivos en Mercados, Donatarias y Federacion.
- Se agrega la migracion `20260421052801_Track1DocumentStorageHardening`.
- La migracion hace backfill minimo de documentos ya existentes desde:
  - `MarketTenant`
  - `DonationApplicationEvidence`
  - `FederationDonationApplicationEvidence`
- Se introduce un store local comun para:
  - guardar archivos nuevos con checksum SHA-256
  - validar integridad minima antes de descargar
  - eliminar archivos fisicos si falla la persistencia del metadata
- Los uploads nuevos de los tres flujos documentales ya registran `StoredDocument` hacia adelante.
- Los downloads ahora validan integridad minima y responden error claro cuando el archivo fisico o la ruta registrada no son consistentes.
- Se expone `GET /api/documents/integrity` para revisar:
  - resumen de integridad
  - inconsistencias detectadas
  - registros documentales con estado operativo
- La pantalla de Bitacora incorpora una vista minima de integridad documental para uso operativo del track.

## Flujos cubiertos
- Cédula digitalizada de locatario en Mercados
- Evidencias de aplicaciones en Donatarias
- Evidencias de aplicaciones en Federacion

## Decisiones tomadas
- Se endurece el storage con `StoredDocument`, pero sin abrir todavia una plataforma documental completa.
- El storage sigue siendo local por modulo; la mejora se centra en consistencia operativa y deteccion de inconsistencias.
- El backfill historico de `StoredDocument` es minimo y sin checksum retroactivo; el checksum solo queda garantizado para uploads nuevos desde este subpaso.
- Los downloads siguen aceptando fallback a la metadata del modulo si por alguna razon no existiera fila en `StoredDocument`, para no romper compatibilidad con el MVP.
- Si un upload escribe el archivo pero falla la persistencia en base de datos, se intenta cleanup del archivo para reducir riesgo de archivos sueltos.

## Convivencia con el storage actual por modulo
- No se reemplaza todavia el esquema local por modulo bajo `App_Data/*`.
- Cada flujo conserva su endpoint y su ruta de storage existente.
- `StoredDocument` actua como registro transversal ligero por encima del storage actual.
- La integridad documental se evalua comparando:
  - metadata del modulo
  - metadata de `StoredDocument`
  - existencia fisica del archivo local
- El resultado puede distinguir al menos:
  - `VALID`
  - `MISSING_FILE`
  - `SIZE_MISMATCH`
  - `INVALID_PATH`
  - `MISSING_DOCUMENT_RECORD`
  - `ORPHANED_DOCUMENT_RECORD`
  - `METADATA_MISMATCH`

## Validacion local ejecutada

### Restore y build
```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln
npm run build
```

### Migracion y base local
```bash
dotnet ef migrations add Track1DocumentStorageHardening --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --context PlatformDbContext --output-dir Persistence/Migrations --no-build --verbose
docker start bigsmile-sql
set +H; ConnectionStrings__PlatformDatabase="Server=127.0.0.1,14333;Database=FMCPA_Development;User Id=sa;Password=BigSmile_dev_Passw0rd!;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" dotnet ef database update --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --context PlatformDbContext --no-build
```

### API y validacion documental
```bash
set +H; ASPNETCORE_ENVIRONMENT=Development ConnectionStrings__PlatformDatabase="Server=127.0.0.1,14333;Database=FMCPA_Development;User Id=sa;Password=BigSmile_dev_Passw0rd!;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" dotnet run --no-build --project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --urls http://127.0.0.1:5087
printf '%s\n' 'Track1 document storage validation certificate' > /tmp/track1-market-certificate.txt
curl -s http://127.0.0.1:5087/health
curl -s -X POST http://127.0.0.1:5087/api/markets -H 'Content-Type: application/json' -d '{"name":"Mercado Track1 Documentos","borough":"Cuauhtemoc","statusCatalogEntryId":1001,"secretaryGeneralName":"Coordinacion Track1","notes":"Validacion documental Track 1 post-MVP."}'
curl -s -X POST http://127.0.0.1:5087/api/markets/{marketId}/tenants -F 'tenantName=Locatario Track1 Documentos' -F 'certificateNumber=CED-T1-001' -F 'certificateValidityTo=2026-12-31' -F 'businessLine=Abarrotes' -F 'mobilePhone=5550001111' -F 'whatsAppPhone=5550001111' -F 'email=track1.locatario@example.com' -F 'notes=Validacion de hardening documental post-MVP.' -F 'certificateFile=@/tmp/track1-market-certificate.txt;type=text/plain'
curl -s http://127.0.0.1:5087/api/markets/tenants/{tenantId}/cedula
curl -s 'http://127.0.0.1:5087/api/documents/integrity?moduleCode=MARKETS&entityType=MARKET_TENANT&take=20'
```

### Simulacion controlada de inconsistencia
```bash
set +H; docker exec bigsmile-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "BigSmile_dev_Passw0rd!" -d FMCPA_Development -Q "UPDATE StoredDocuments SET StoredRelativePath = N'missing/track1-market-certificate.txt' WHERE DocumentAreaCode = N'MARKETS_TENANT_CERTIFICATES' AND EntityType = N'MARKET_TENANT' AND EntityId = '{tenantId}';"
curl -s 'http://127.0.0.1:5087/api/documents/integrity?moduleCode=MARKETS&entityType=MARKET_TENANT&take=20'
set +H; docker exec bigsmile-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "BigSmile_dev_Passw0rd!" -d FMCPA_Development -Q "UPDATE StoredDocuments SET StoredRelativePath = N'{rutaOriginal}' WHERE DocumentAreaCode = N'MARKETS_TENANT_CERTIFICATES' AND EntityType = N'MARKET_TENANT' AND EntityId = '{tenantId}';"
curl -s 'http://127.0.0.1:5087/api/documents/integrity?moduleCode=MARKETS&entityType=MARKET_TENANT&take=20'
```

## Resultado de validacion
- Backend compila correctamente.
- Frontend compila correctamente.
- La migracion se genera y se aplica correctamente sobre `FMCPA_Development`.
- El upload nuevo de Mercados registra metadata homogenea, checksum y fila en `StoredDocument`.
- La descarga actual sigue funcionando cuando el archivo y la metadata son consistentes.
- El endpoint `/api/documents/integrity` detecta una inconsistencia simulada real con:
  - `METADATA_MISMATCH`
  - `MISSING_FILE`
- El registro de validacion se restaura y la integridad vuelve a estado `VALID`.

## Que quedo fuera
- Estrategia documental transversal completa
- Retencion y respaldo reales
- Storage externo o blob storage
- Antivirus, scanning o controles avanzados de seguridad documental
- Administracion documental completa en frontend
- Remediacion automatica de inconsistencias

## Riesgos y pendientes inmediatos
- Los documentos heredados del MVP quedan registrados en `StoredDocument`, pero sin checksum historico.
- La deteccion actual es operativa y bajo demanda; no existe monitoreo continuo.
- El storage sigue siendo local por modulo y puede perder archivos si el filesystem se altera fuera del flujo de la aplicacion.
